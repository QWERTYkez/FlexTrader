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
    public class CandlesModule : ChartModule, IClipCandles
    {
        private List<ICandle> allCandles = new List<ICandle>();
        public event Action<List<ICandle>> CandlesChanged;
        public List<ICandle> AllCandles { get => allCandles; private set 
            {
                allCandles = value;
                CandlesChanged.Invoke(value);
            } }
        
        public TimeSpan DeltaTime { get; private set; }
        public DateTime StartTime { get; private set; }

        public Vector CurrentTranslate;
        public Vector CurrentScale;

        private readonly DrawingCanvas CandlesLayer;
        private readonly PriceLineModule PriceLineModule;
        private readonly TimeLineModule TimeLineModule;
        private readonly TranslateTransform Translate;
        private readonly ScaleTransform ScaleX;
        private readonly ScaleTransform ScaleY;
        public CandlesModule(IChart chart, DrawingCanvas CandlesLayer, PriceLineModule PriceLineModule,
            TimeLineModule TimeLineModule, TranslateTransform Translate, ScaleTransform ScaleX, 
            ScaleTransform ScaleY, Grid TimeLine, Grid PriceLine, Vector CurrentScale) : base(chart)
        {
            Chart.MWindow.Candles.Add(this);

            this.CandlesLayer = CandlesLayer;
            this.PriceLineModule = PriceLineModule;
            this.TimeLineModule = TimeLineModule;
            this.Translate = Translate;
            this.ScaleX = ScaleX;
            this.ScaleY = ScaleY;
            this.CurrentScale = CurrentScale;

            var SetUpBrush = new Action<SolidColorBrush>(b => { UpBrush = b; Redraw(); });
            var SetUpPenBrush = new Action<SolidColorBrush>(b => { Dispatcher.Invoke(() => { UpPen.Brush = b; }); Redraw(); });
            var SetDownBrush = new Action<SolidColorBrush>(b => { DownBrush = b; Redraw(); });
            var SetDownPenBrush = new Action<SolidColorBrush>(b => { Dispatcher.Invoke(() => { DownPen.Brush = b; }); Redraw(); });
            var SetThicknesses = new Action<int>(b =>
            {
                Dispatcher.Invoke(() =>
                {
                    DownPen.Thickness = b;
                    UpPen.Thickness = b;
                });
                Redraw();
            });

            TimeLine.MouseEnter += (s, e) =>
            { if (Chart.Clipped) Chart.Moving = TimeScaleAll; else Chart.Moving = TimeScale; };
            TimeLine.MouseLeave += (s, e) =>
            { if (Chart.Moving == TimeScale || Chart.Moving == TimeScaleAll) Chart.Moving = null; };
            PriceLine.MouseEnter += (s, e) => Chart.Moving = PriceLine_MouseDown;
            PriceLine.MouseLeave += (s, e) => { if (Chart.Moving == PriceLine_MouseDown) Chart.Moving = null; };
            Chart.ChartGrid.MouseEnter += SetMoving;
            Chart.ChartGrid.MouseLeave += BreakMoving;
            Chart.MWindow.ToggleMagnet += b =>
            {
                Task.Run(() =>
                {
                    MagnetStatus = b;
                    if (b) UpdateMagnetData();
                    else ResetMagnetData();
                });
            };

            Sets.AddLevel("Бычья свеча", new Setting[]
            {
                new Setting("Цвет тела", () => this.UpBrush, SetUpBrush, Brushes.Lime),
                new Setting("Цвет фитиля", () => this.UpPen.Brush, SetUpPenBrush, Brushes.Lime)
            });

            Sets.AddLevel("Медвежья свеча", new Setting[]
            {
                new Setting("Цвет тела", () => this.DownBrush, SetDownBrush, Brushes.Red),
                new Setting("Цвет фитиля", () => this.DownPen.Brush, SetDownPenBrush, Brushes.Red)
            });

            Sets.Add(new Setting(IntType.Slider, "Толщина фитиля", () => (int)this.DownPen.Thickness, SetThicknesses, 2, 6, null, null, 4));
        }

        private protected override string SetsName => "Настройки свечей";

        private double Delta = 1;
        private double Min;

        public void NewCandles(List<ICandle> NewCandles)
        {
            DeltaTime = (NewCandles[1].TimeStamp - NewCandles[0].TimeStamp);
            Chart.MWindow.ResetClips();
            AllCandles.Clear();  AddCandles(NewCandles);
        }
        public void AddCandles(List<ICandle> NewCandles)
        {
            AllCandles.AddRange(NewCandles);
            AllCandles = AllCandles.OrderBy(c => c.TimeStamp).ToList();

            //DrawCandles
            {
                StartTime = NewCandles.Last().TimeStamp;

                var DrawTeplates = new (Pen ShadowPen, Point PointA,
                    Point PointB, Brush BodyBrush, Rect Rect)[NewCandles.Count];

                Parallel.For(0, NewCandles.Count, i => 
                {
                    var open = NewCandles[i].OpenD / Chart.TickSize;
                    var close = NewCandles[i].CloseD / Chart.TickSize;
                    var low = NewCandles[i].LowD / Chart.TickSize;
                    var high = NewCandles[i].HighD / Chart.TickSize;
                    var x = 7.5 + (StartTime - NewCandles[i].TimeStamp) * 15 / DeltaTime;
                    var x1 = x + 6;
                    var x2 = x - 6;

                    var PointA = new Point(x, low);
                    var PointB = new Point(x, high);
                    var Rect = new Rect(new Point(x1, open), new Point(x2, close));

                    if (NewCandles[i].UP) DrawTeplates[i] = (UpPen, PointA, PointB, UpBrush, Rect);
                    else DrawTeplates[i] = (DownPen, PointA, PointB, DownBrush, Rect);
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

                HorizontalReset();
            }
        }

        private IEnumerable<IClipCandles> Clips => Chart.MWindow.ClipsCandles[DeltaTime];
        #region Перерассчет шкал
        public event Action<IEnumerable<ICandle>> AllHorizontalReset;
        private bool VerticalLock = true;
        public async void VerticalReset()
        {
            if (VerticalLock)
            {
                PriceLineModule.LastY = CurrentTranslate.Y = Min + Delta * 0.5 - Chart.ChHeight * 0.5;
                CurrentScale.Y = Chart.ChHeight / Delta;
                Dispatcher.Invoke(() =>
                {
                    Translate.Y = CurrentTranslate.Y;
                    ScaleY.ScaleY = CurrentScale.Y;
                });
            }
            await PriceLineModule.Redraw();
        }
        public void HorizontalReset(IEnumerable<ICandle> currentCandles = null)
        {
            TimeLineModule.Redraw();
            if (currentCandles == null)
            {
                var TimeA = StartTime - Math.Ceiling(((Chart.ChWidth / CurrentScale.X + CurrentTranslate.X) / 15)) * DeltaTime;
                var TimeB = StartTime - Math.Floor((CurrentTranslate.X / 15)) * DeltaTime;

                currentCandles = from c in AllCandles
                                 where c.TimeStamp >= TimeA && c.TimeStamp <= TimeB
                                 select c;
            }

            if (currentCandles.Count() < 2) goto Return;

            AllHorizontalReset.Invoke(currentCandles);

            var mmm = Convert.ToDouble(currentCandles.Select(c => c.Low).Min()) / Chart.TickSize;
            var max = Convert.ToDouble(currentCandles.Select(c => c.High).Max()) / Chart.TickSize;
            var delta = max - mmm;
            max += delta * 0.05;
            var nMin = mmm - delta * 0.05;
            var nDelta = max - nMin;
            if (Min == nMin && Delta == nDelta) goto Return;
            Min = nMin;
            Delta = nDelta;

            if (VerticalLock)
            {
                PriceLineModule.LastMin = Min;
                PriceLineModule.LastY = CurrentTranslate.Y;
                PriceLineModule.LastDelta = Delta;
            }

            VerticalReset();
        Return:
            if (MagnetStatus) ResetMagnetData();
        }
        #endregion
        #region скалирование по ценовой шкале
        private double LastScaleY;
        private void PriceLine_MouseDown(MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (e.ClickCount == 2) { VerticalLock = true; VerticalReset(); return; }
            LastScaleY = CurrentScale.Y;
            VerticalLock = false;
            Chart.MWindow.MoveElement(e, async vec =>
            {
                if (vec == null) return;
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
        }
        #endregion
        #region скалирование по временной шкале
        public double LastScaleX { get; set; }
        private const double MaxCandleWidth = 175;
        private void TimeScale(MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) 
                ResetTimeScale();
            else
            {
                LastScaleX = CurrentScale.X;
                Chart.MWindow.MoveElement(e, vec => TimeScaling(vec));
            }
        }
        private void TimeScaleAll(MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) 
                foreach (var cl in Clips) 
                    cl.ResetTimeScale(); 
            else
            {
                var fl = new List<Func<Vector?, Task>>();
                foreach (var cl in Clips)
                {
                    cl.LastScaleX = CurrentScale.X;
                    fl.Add(cl.TimeScaling);
                }
                Chart.MWindow.MoveElements(e, fl);
            }
        }
        public void ResetTimeScale()
        {
            Task.Run(() => 
            {
                Dispatcher.Invoke(async () =>
                {
                    CurrentScale.X = 1;
                    ScaleX.ScaleX = 1;
                    CurrentTranslate.X = 0;
                    Translate.X = 0;
                    HorizontalReset();
                    NewXScale.Invoke(1);
                    NewXTrans.Invoke(0);

                    await UpdateMagnetData();
                });
            });
        }
        public Task TimeScaling(Vector? vec)
        {
            return Task.Run(async () =>
            {
                if (vec == null) return;
                var Y = -vec.Value.X / 50;
                IEnumerable<ICandle> currentCandles = null;
                if (vec.Value.X > 0)
                {
                    CurrentScale.X = LastScaleX / (1 - Y);
                    await Dispatcher.InvokeAsync(() => ScaleX.ScaleX = CurrentScale.X);
                }
                else if (vec.Value.X < 0)
                {
                    var nScale = LastScaleX * (1 + Y);
                    var TimeA = StartTime - Math.Ceiling(((Chart.ChWidth / nScale + CurrentTranslate.X) / 15)) * DeltaTime;
                    var TimeB = StartTime - Math.Floor((CurrentTranslate.X / 15)) * DeltaTime;
                    currentCandles = from c in AllCandles
                                     where c.TimeStamp >= TimeA && c.TimeStamp <= TimeB
                                     select c;


                    if (currentCandles.Count() < 2 || Chart.ChWidth / MaxCandleWidth > (TimeB - TimeA) / DeltaTime) return;
                    CurrentScale.X = nScale;
                    await Dispatcher.InvokeAsync(() => ScaleX.ScaleX = CurrentScale.X);
                }
                HorizontalReset(currentCandles);
                NewXScale.Invoke(CurrentScale.X);

                await UpdateMagnetData();
            });
        }
        #endregion
        #region перемещение графика 
        public Vector LastTranslateVector { get; set; }
        public event Action<double> NewXTrans;
        public void SetMoving(object sender, MouseEventArgs e)
        { if (Chart.Clipped) Chart.Moving = MovingAllCharts; else Chart.Moving = MovingChart; }
        public void BreakMoving(object sender, MouseEventArgs e)
        { if (Chart.Moving == MovingChart || Chart.Moving == MovingAllCharts) Chart.Moving = null; }
        private void MovingChart(MouseButtonEventArgs e)
        {
            LastTranslateVector = CurrentTranslate;
            Chart.MWindow.MoveElement(e, vec => MovingChart(vec));
        }
        private void MovingAllCharts(MouseButtonEventArgs e)
        {
            var fl = new List<Func<Vector?, Task>>();
            foreach (var cl in Clips)
            {
                cl.LastTranslateVector = CurrentTranslate;
                fl.Add(cl.MovingChart);
            }
            Chart.MWindow.MoveElements(e, fl);
        }
        public Task MovingChart(Vector? vec)
        {
            return Task.Run(async () => 
            {
                if (vec == null) return;
                var X = LastTranslateVector.X + vec.Value.X / CurrentScale.X;

                var TimeA = StartTime - Math.Floor(((Chart.ChWidth / CurrentScale.X + X) / 15)) * DeltaTime;
                var TimeB = StartTime - Math.Ceiling((X / 15)) * DeltaTime;
                var currentCandles = from c in AllCandles
                                     where c.TimeStamp >= TimeA && c.TimeStamp <= TimeB
                                     select c;

                if (currentCandles.Count() < 1)
                {
                    if (!(TimeB < AllCandles.First().TimeStamp && CurrentTranslate.X < X)) return;
                    if (!(TimeA < AllCandles.Last().TimeStamp && CurrentTranslate.X > X)) return;
                }

                CurrentTranslate.X = X;
                if (VerticalLock)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        Translate.X = CurrentTranslate.X;
                    });
                    HorizontalReset(currentCandles);
                    NewXTrans.Invoke(CurrentTranslate.X);
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
                    AllHorizontalReset.Invoke(currentCandles);
                    NewXTrans.Invoke(CurrentTranslate.X);
                }
            });
        }
        #endregion
        #region Скалирование колесом 
        public event Action WhellScalled;
        public event Action<double> NewXScale;
        public Task WheelSpinning(MouseWheelEventArgs e)
        {
            e.Handled = true;
            if (Chart.Clipped)
            {
                var tl = new List<Task>();
                foreach (var cl in Clips)
                    tl.Add(cl.WhellScalling(e));
                return Task.WhenAll(tl);
            }
            else return WhellScalling(e);
        }
        public Task WhellScalling(MouseWheelEventArgs e)
        {
            return Task.Run(async () =>
            {
                IEnumerable<ICandle> currentCandles = null;
                if (e.Delta > 0)
                {
                    var nScale = CurrentScale.X * 1.1;
                    var TimeA = StartTime - Math.Ceiling(((Chart.ChWidth / nScale + CurrentTranslate.X) / 15)) * DeltaTime;
                    var TimeB = StartTime - Math.Floor((CurrentTranslate.X / 15)) * DeltaTime;
                    currentCandles = from c in AllCandles
                                     where c.TimeStamp >= TimeA && c.TimeStamp <= TimeB
                                     select c;


                    if (currentCandles.Count() < 2 || Chart.ChWidth / MaxCandleWidth > (TimeB - TimeA) / DeltaTime) return;
                    CurrentScale.X = nScale;
                }
                else CurrentScale.X /= 1.1;

                await Dispatcher.InvokeAsync(() => ScaleX.ScaleX = CurrentScale.X);
                HorizontalReset(currentCandles);
                NewXScale.Invoke(CurrentScale.X);

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

            public Point ToPoint() => new Point(X, Y);

            public double X;
            public double Y;
            public decimal Price;

            public bool Equals(MagnetPoint other) => Equals(other, this);
            public override int GetHashCode() => $"{X}||{Y}||{Price}".GetHashCode();
            public static bool operator ==(MagnetPoint c1, MagnetPoint c2) => c1.Equals(c2);
            public static bool operator !=(MagnetPoint c1, MagnetPoint c2) => !c1.Equals(c2);
        }
        #endregion

        public void Redraw() 
        { 
            Task.Run(() => 
            {
                //DrawCandles
                {
                    StartTime = AllCandles.Last().TimeStamp;

                    var DrawTeplates = new (Pen ShadowPen, Point PointA,
                        Point PointB, Brush BodyBrush, Rect Rect)[AllCandles.Count];

                    Parallel.For(0, AllCandles.Count, i => 
                    {
                        var open = AllCandles[i].OpenD / Chart.TickSize;
                        var close = AllCandles[i].CloseD / Chart.TickSize;
                        var low = AllCandles[i].LowD / Chart.TickSize;
                        var high = AllCandles[i].HighD / Chart.TickSize;
                        var x = 7.5 + (StartTime - AllCandles[i].TimeStamp) * 15 / DeltaTime;
                        var x1 = x + 6;
                        var x2 = x - 6;

                        var PointA = new Point(x, low);
                        var PointB = new Point(x, high);
                        var Rect = new Rect(new Point(x1, open), new Point(x2, close));

                        if (AllCandles[i].UP) DrawTeplates[i] = (UpPen, PointA, PointB, UpBrush, Rect);
                        else DrawTeplates[i] = (DownPen, PointA, PointB, DownBrush, Rect);
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
        private protected override void Destroy() { }

        public Brush UpBrush = Brushes.Lime;
        public Brush DownBrush = Brushes.Red;
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
