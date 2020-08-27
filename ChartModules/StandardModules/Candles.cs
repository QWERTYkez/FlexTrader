/* 
    Copyright ©  2020  Andrej Melekhin <QWERTYkez@outlook.com>.

    This file is part of FlexTrader
    FlexTrader is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    FlexTrader is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with FlexTrader. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ChartModules.StandardModules
{
    public class CandlesModule : ChartModule
    {
        public List<ICandle> AllCandles = new List<ICandle>();
        public TimeSpan? DeltaTime { get; private set; }
        public DateTime? StartTime { get; private set; }

        public Vector CurrentTranslate;
        public Vector CurrentScale;

        private readonly IDrawingCanvas CandlesLayer;
        private readonly PriceLineModule PriceLineModule;
        private readonly TimeLineModule TimeLineModule;
        private readonly TranslateTransform Translate;
        private readonly ScaleTransform ScaleX;
        private readonly ScaleTransform ScaleY;
        private readonly IDrawingCanvas TimeLine;
        private readonly Grid PriceLine;
        public CandlesModule(IChart chart, IDrawingCanvas CandlesLayer, PriceLineModule PriceLineModule,
            TimeLineModule TimeLineModule, TranslateTransform Translate, ScaleTransform ScaleX, 
            ScaleTransform ScaleY, IDrawingCanvas TimeLine, Grid PriceLine, Vector CurrentScale) : base(chart)
        {
            this.CandlesLayer = CandlesLayer;
            this.PriceLineModule = PriceLineModule;
            this.TimeLineModule = TimeLineModule;
            this.Translate = Translate;
            this.ScaleX = ScaleX;
            this.ScaleY = ScaleY;
            this.TimeLine = TimeLine;
            this.PriceLine = PriceLine;
            this.CurrentScale = CurrentScale;

            TimeLine.PreviewMouseDown += TimeLine_MouseDown;
            PriceLine.PreviewMouseDown += PriceLine_MouseDown;

            var SetUpBrush = new Action<object>(b => { UpBrush = b as Brush; Redraw(); });
            var SetUpPenBrush = new Action<object>(b => { Dispatcher.Invoke(() => { this.UpPen.Brush = b as Brush; }); Redraw(); });
            var SetDownBrush = new Action<object>(b => { DownBrush = b as Brush; Redraw(); });
            var SetDownPenBrush = new Action<object>(b => { Dispatcher.Invoke(() => { this.DownPen.Brush = b as Brush; }); Redraw(); });
            var SetThicknesses = new Action<object>(b =>
            {
                Dispatcher.Invoke(() =>
                {
                    this.DownPen.Thickness = (b as double?).Value;
                    this.UpPen.Thickness = (b as double?).Value;
                });
                Redraw();
            });

            Chart.Moving = MovingChart;
            Chart.MWindow.ToggleMagnet += b =>
            {
                Task.Run(() =>
                {
                    MagnetStatus = b;
                    if (b) UpdateMagnetData();
                    else ResetMagnetData();
                });
            };

            SetsName = "Настройки свечей";

            Setting.SetsLevel(Sets, "Бычья свеча", new Setting[]
            {
                new Setting("Цвет тела", () => this.UpBrush, SetUpBrush, Brushes.Lime),
                new Setting("Цвет фитиля", () => this.UpPen.Brush, SetUpPenBrush, Brushes.Lime)
            });

            Setting.SetsLevel(Sets, "Медвежья свеча", new Setting[]
            {
                new Setting("Цвет тела", () => this.DownBrush, SetDownBrush, Brushes.Red),
                new Setting("Цвет фитиля", () => this.DownPen.Brush, SetDownPenBrush, Brushes.Red)
            });

            Sets.Add(new Setting(SetType.DoubleSlider, "Толщина фитиля", () => this.DownPen.Thickness, SetThicknesses, 2d, 6d, 4d));
        }

        private double Delta;
        private double Min;

        private readonly object parallelkey = new object();
        public void AddCandles(List<ICandle> NewCandles)
        {
            if (!DeltaTime.HasValue)
                DeltaTime = (NewCandles[1].TimeStamp - NewCandles[0].TimeStamp);
            else
            {
                var ts = NewCandles[1].TimeStamp - NewCandles[0].TimeStamp;
                if (DeltaTime.Value != ts) DeltaTime = ts;
            }
            AllCandles.AddRange(NewCandles);
            AllCandles = AllCandles.OrderBy(c => c.TimeStamp).ToList();

            //DrawCandles
            {
                if (StartTime == null)
                    StartTime = NewCandles.Last().TimeStamp;

                var DrawTeplates = new List<(Pen ShadowPen, Point PointA,
                    Point PointB, Brush BodyBrush, Rect Rect)>();
                Parallel.ForEach(NewCandles, c =>
                {
                    var open = c.OpenD / Chart.TickSize;
                    var close = c.CloseD / Chart.TickSize;
                    var low = c.LowD / Chart.TickSize;
                    var high = c.HighD / Chart.TickSize;
                    var x = 7.5 + (StartTime.Value - c.TimeStamp) * 15 / DeltaTime.Value;
                    var x1 = x + 6;
                    var x2 = x - 6;

                    var PointA = new Point(x, low);
                    var PointB = new Point(x, high);
                    var Rect = new Rect(new Point(x1, open), new Point(x2, close));

                    lock (parallelkey)
                    {
                        if (c.UP) DrawTeplates.Add((UpPen, PointA, PointB, UpBrush, Rect));
                        else DrawTeplates.Add((DownPen, PointA, PointB, DownBrush, Rect));
                    }
                });

                Dispatcher.Invoke(() =>
                {
                    var dvisual = new DrawingVisual();
                    CandlesLayer.AddVisual(dvisual);

                    using var dc = dvisual.RenderOpen();
                    foreach (var dt in DrawTeplates)
                    {
                        dc.DrawLine(dt.ShadowPen, dt.PointA, dt.PointB);
                        dc.DrawRectangle(dt.BodyBrush, null, dt.Rect);
                    }
                });

                VerticalReset();
            }
        }

        #region Перерассчет шкал
        private bool VerticalLock = true;
        private async void VerticalReset()
        {
            if (VerticalLock)
            {
                CurrentTranslate.Y = Min + Delta * 0.5 - Chart.ChHeight * 0.5;
                PriceLineModule.LastY = CurrentTranslate.Y;
                CurrentScale.Y = Chart.ChHeight / Delta;
                Dispatcher.Invoke(() =>
                {
                    Translate.Y = CurrentTranslate.Y;
                    ScaleY.ScaleY = CurrentScale.Y;
                });
            }
            await PriceLineModule.Redraw();
        }
        public void HorizontalReset(bool HeightChanged = false)
        {
            if (StartTime.HasValue && DeltaTime.HasValue)
            {
                TimeLineModule.Redraw();
                var TimeA = StartTime.Value - Math.Ceiling(((Chart.ChWidth / CurrentScale.X + CurrentTranslate.X) / 15)) * DeltaTime.Value;
                var TimeB = StartTime.Value - Math.Floor((CurrentTranslate.X / 15)) * DeltaTime.Value;

                var currentCandles = from c in AllCandles.AsParallel()
                                     where c.TimeStamp >= TimeA && c.TimeStamp <= TimeB
                                     select c;

                if (currentCandles.Count() < 1) goto Return;

                var mmm = Convert.ToDouble(currentCandles.Select(c => c.LowD).Min()) / Chart.TickSize;
                var max = Convert.ToDouble(currentCandles.Select(c => c.HighD).Max()) / Chart.TickSize;
                var delta = max - mmm;
                max += delta * 0.05;
                var nMin = mmm - delta * 0.05;
                var nDelta = max - Min;
                if (Min == nMin && Delta == nDelta && !HeightChanged) goto Return;
                Min = nMin;
                Delta = nDelta;

                if (VerticalLock)
                {
                    PriceLineModule.LastMin = Min;
                    PriceLineModule.LastY = CurrentTranslate.Y;
                    PriceLineModule.LastDelta = Delta;
                }

                VerticalReset();
            }
        Return:
            if (MagnetStatus) ResetMagnetData();
        }
        #endregion
        #region скалирование по ценовой шкале
        private double LastScaleY;
        private void PriceLine_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (e.ClickCount == 2) { VerticalLock = true; VerticalReset(); return; }
            LastScaleY = CurrentScale.Y;
            VerticalLock = false;
            Chart.MWindow.MoveCursor(e, vec =>
            {
                if (vec == null) return;
                Task.Run(async () =>
                {
                    var X = vec.Value.Y / 50;
                    if (vec.Value.Y < 0)
                    {
                        var Z = Chart.ChHeight / (CurrentScale.Y * Chart.TickSize) - 5 * Chart.ChHeight;
                        if (Z <= 0) return;
                        CurrentScale.Y = LastScaleY * (1 - X);
                    }
                    else if (vec.Value.Y > 0) CurrentScale.Y = LastScaleY / (1 + X);
                    await Dispatcher.InvokeAsync(() => ScaleY.ScaleY = CurrentScale.Y);

                    _ = PriceLineModule.Redraw();
                    await UpdateMagnetData();
                });
            });
        }
        #endregion
        #region скалирование по временной шкале
        private double LastScaleX;
        private const double MaxCandleWidth = 175;
        private async void TimeLine_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (e.ClickCount == 2)
            {
                CurrentScale.X = 1;
                ScaleX.ScaleX = 1;
                CurrentTranslate.X = 0;
                Translate.X = 0;
                HorizontalReset();

                await UpdateMagnetData();
                return;
            }
            LastScaleX = CurrentScale.X;
            Chart.MWindow.MoveCursor(e, vec =>
            {
                if (vec == null) return;

                Task.Run(async () =>
                {
                    var Y = -vec.Value.X / 50;
                    if (vec.Value.X > 0)
                    {
                        CurrentScale.X = LastScaleX / (1 - Y);
                        await Dispatcher.InvokeAsync(() => ScaleX.ScaleX = CurrentScale.X);
                    }
                    else if (vec.Value.X < 0)
                    {
                        var nScale = LastScaleX * (1 + Y);
                        var TimeA = StartTime.Value - Math.Ceiling(((Chart.ChWidth / nScale + CurrentTranslate.X) / 15)) * DeltaTime.Value;
                        var TimeB = StartTime.Value - Math.Floor((CurrentTranslate.X / 15)) * DeltaTime.Value;
                        var currentCandles = from c in AllCandles.AsParallel()
                                             where c.TimeStamp >= TimeA && c.TimeStamp <= TimeB
                                             select c;


                        if (currentCandles.Count() < 1 || Chart.ChWidth / MaxCandleWidth > (TimeB - TimeA) / DeltaTime.Value) return;
                        CurrentScale.X = nScale;
                        await Dispatcher.InvokeAsync(() => ScaleX.ScaleX = CurrentScale.X);
                    }
                    HorizontalReset();

                    await UpdateMagnetData();
                });
            });
        }
        #endregion
        #region перемещение графика 
        private Vector LastTranslateVector;
        public void MovingChart(MouseButtonEventArgs e)
        {
            LastTranslateVector = CurrentTranslate;
            Chart.MWindow.MoveCursor(e, async vec => 
            {
                if (vec == null) return;


                var X = LastTranslateVector.X + vec.Value.X / CurrentScale.X;
                var TimeA = StartTime.Value - Math.Floor(((Chart.ChWidth / CurrentScale.X + X) / 15)) * DeltaTime.Value;
                var TimeB = StartTime.Value - Math.Ceiling((X / 15)) * DeltaTime.Value;
                var currentCandles = from c in AllCandles.AsParallel()
                                     where c.TimeStamp >= TimeA && c.TimeStamp <= TimeB
                                     select c;

                if (currentCandles.Count() < 0) 
                {
                    var TSS = AllCandles.AsParallel().Select(c => c.TimeStamp);
                    var MaxT = TSS.Max(); var MinT = TSS.Min();
                    if (!(TimeB < MaxT && CurrentTranslate.X < X)) return;
                    if (!(TimeA < MinT && CurrentTranslate.X > X)) return;
                }
                CurrentTranslate.X = X;
                if (VerticalLock)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        Translate.X = CurrentTranslate.X;
                    });
                    HorizontalReset();
                } 
                else
                {
                    CurrentTranslate.Y = LastTranslateVector.Y + vec.Value.Y / CurrentScale.Y;
                    await Dispatcher.InvokeAsync(() =>
                    {
                        Translate.X = CurrentTranslate.X;
                        Translate.Y = CurrentTranslate.Y;
                    });
                    _ = TimeLineModule.Redraw();
                    _ = PriceLineModule.Redraw();
                }
            });
        }
        #endregion
        #region Скалирование колесом 
        public event Action WhellScalled;
        public Task WhellScalling(MouseWheelEventArgs e)
        {
            e.Handled = true;
            return Task.Run(async () =>
            {
                if (e.Delta > 0)
                {
                    var nScale = CurrentScale.X * 1.1;
                    var TimeA = StartTime.Value - Math.Ceiling(((Chart.ChWidth / nScale + CurrentTranslate.X) / 15)) * DeltaTime.Value;
                    var TimeB = StartTime.Value - Math.Floor((CurrentTranslate.X / 15)) * DeltaTime.Value;
                    var currentCandles = from c in AllCandles.AsParallel()
                                         where c.TimeStamp >= TimeA && c.TimeStamp <= TimeB
                                         select c;


                    if (currentCandles.Count() < 1 || Chart.ChWidth / MaxCandleWidth > (TimeB - TimeA) / DeltaTime.Value) return;
                    CurrentScale.X = nScale;
                    await Dispatcher.InvokeAsync(() =>
                    {
                        ScaleX.ScaleX = CurrentScale.X;
                    });
                    HorizontalReset();
                }
                else
                {
                    CurrentScale.X /= 1.1;
                    await Dispatcher.InvokeAsync(() =>
                    {
                        ScaleX.ScaleX = CurrentScale.X;
                    });
                    HorizontalReset();
                }

                await UpdateMagnetData();
                WhellScalled?.Invoke();
            });
        }
        #endregion

        #region Magnet
        public List<MagnetPoint> MagnetPoints { get; private set; } = new List<MagnetPoint>();
        private int ChangesCounter = 0;
        public bool MagnetStatus = false;
        public Task UpdateMagnetData()
        {
            if (!MagnetStatus) return Task.Run(() => { });

            ChangesCounter += 1;
            var x = ChangesCounter;
            Thread.Sleep(100);
            if (x != ChangesCounter) return Task.Run(() => { });

            return Task.Run(() => 
            {
                var tA = Chart.WidthToTime(0);
                var tB = Chart.WidthToTime(Chart.ChWidth);
                var max = Chart.HeightToPrice(0);
                var min = Chart.HeightToPrice(Chart.ChHeight);

                var cc = from c in AllCandles.AsParallel()
                         where c.TimeStamp >= tA && c.TimeStamp <= tB
                         select c;

                MagnetPoints.Clear();

                double x; double y; decimal yp;

                foreach (var c in cc) 
                {
                    x = Chart.TimeToWidth(c.TimeStamp);

                    y = c.HighD; yp = c.High;
                    if (min <= y && y <= max)
                        MagnetPoints.Add(new MagnetPoint(x, Chart.PriceToHeight(y), yp));

                    y = c.LowD; yp = c.Low;
                    if (min <= y && y <= max)
                        MagnetPoints.Add(new MagnetPoint(x, Chart.PriceToHeight(y), yp));

                    y = c.OpenD; yp = c.Open;
                    if (min <= y && y <= max)
                        MagnetPoints.Add(new MagnetPoint(x, Chart.PriceToHeight(y), yp));

                    y = c.CloseD; yp = c.Close;
                    if (min <= y && y <= max)
                        MagnetPoints.Add(new MagnetPoint(x, Chart.PriceToHeight(y), yp));
                }
            });
        }
        public void ResetMagnetData() => MagnetPoints.Clear();
        public struct MagnetPoint
        {
            public MagnetPoint(double X, double Y, decimal Price)
            {
                this.X = X; this.Y = Y;
                this.Price = Price;
            }

            public double X;
            public double Y;
            public decimal Price;

            public bool Equals(MagnetPoint other) => Equals(other, this);
            public override int GetHashCode() => $"{X}||{Y}||{Price}".GetHashCode();
            public static bool operator ==(MagnetPoint c1, MagnetPoint c2) => c1.Equals(c2);
            public static bool operator !=(MagnetPoint c1, MagnetPoint c2) => !c1.Equals(c2);
        }
        #endregion

        public override Task Redraw() 
        { 
            return Task.Run(() => 
            {
                DeltaTime = (AllCandles[1].TimeStamp - AllCandles[0].TimeStamp);

                //DrawCandles
                {
                    StartTime = AllCandles.Last().TimeStamp;

                    var DrawTeplates = new List<(Pen ShadowPen, Point PointA,
                        Point PointB, Brush BodyBrush, Rect Rect)>();
                    Parallel.ForEach(AllCandles, c =>
                    {
                        var open = c.OpenD / Chart.TickSize;
                        var close = c.CloseD / Chart.TickSize;
                        var low = c.LowD / Chart.TickSize;
                        var high = c.HighD / Chart.TickSize;
                        var x = 7.5 + (StartTime.Value - c.TimeStamp) * 15 / DeltaTime.Value;
                        var x1 = x + 6;
                        var x2 = x - 6;

                        var PointA = new Point(x, low);
                        var PointB = new Point(x, high);
                        var Rect = new Rect(new Point(x1, open), new Point(x2, close));

                        lock (parallelkey)
                        {
                            if (c.UP) DrawTeplates.Add((UpPen, PointA, PointB, UpBrush, Rect));
                            else DrawTeplates.Add((DownPen, PointA, PointB, DownBrush, Rect));
                        }
                    });

                    Dispatcher.Invoke(() =>
                    {
                        CandlesLayer.ClearVisuals();

                        var dvisual = new DrawingVisual();
                        CandlesLayer.AddVisual(dvisual);

                        using var dc = dvisual.RenderOpen();
                        foreach (var dt in DrawTeplates)
                        {
                            dc.DrawLine(dt.ShadowPen, dt.PointA, dt.PointB);
                            dc.DrawRectangle(dt.BodyBrush, null, dt.Rect);
                        }
                    });

                    VerticalReset();
                }
            }); 
        }
        private protected override void Destroy() 
        {
            TimeLine.PreviewMouseDown -= TimeLine_MouseDown;
            PriceLine.PreviewMouseDown -= PriceLine_MouseDown;
        }

        private Brush UpBrush = Brushes.Lime;
        private Brush DownBrush = Brushes.Red;
        private readonly Pen UpPen = new Pen(Brushes.Lime, 4);
        private readonly Pen DownPen = new Pen(Brushes.Red, 4);
    }

    public interface ICandle
    {
        public bool UP { get; }
        public DateTime TimeStamp { get; }
        public decimal Open { get; }
        public double OpenD { get; }
        public decimal High { get; }
        public double HighD { get; }
        public decimal Low { get; }
        public double LowD { get; }
        public decimal Close { get; }
        public double CloseD { get; }
        public decimal Volume { get; }
        public double VolumeD { get; }
    }
}
