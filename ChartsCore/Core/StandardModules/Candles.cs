﻿/* 
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

using ExchangesCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ChartsCore.Core.StandardModules
{
    public class CandlesModule : ChartModule
    {
        private List<Candle> allCandles = new List<Candle>();
        public event Action<List<Candle>> CandlesChanged;
        public List<Candle> AllCandles { get => allCandles; private set 
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
        public CandlesModule(View chart, DrawingCanvas CandlesLayer, PriceLineModule PriceLineModule,
            TimeLineModule TimeLineModule, TranslateTransform Translate, ScaleTransform ScaleX, 
            ScaleTransform ScaleY, Grid TimeLine, Grid PriceLine, Vector CurrentScale) : base(chart)
        {
            Chart.Shell.Candles.Add(this);

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
            Chart.Shell.ToggleMagnet += b =>
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

        public void NewCandles(List<Candle> NewCandles)
        {
            DeltaTime = (NewCandles[1].TimeStamp - NewCandles[0].TimeStamp);
            Chart.Shell.ResetClips();
            AllCandles.Clear();  AddCandles(NewCandles);
        }
        public void AddCandles(List<Candle> NewCandles)
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

        private IEnumerable<CandlesModule> Clips => Chart.Shell.ClipsCandles[DeltaTime];
        #region Перерассчет шкал
        public event Action<IEnumerable<Candle>> AllHorizontalReset;
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
        public void HorizontalReset(IEnumerable<Candle> currentCandles = null)
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
            Chart.Window.MoveElement(e, async vec =>
            {
                var X = vec.Y / 50;
                if (vec.Y < 0)
                {
                    var Z = Chart.ChHeight / (CurrentScale.Y * Chart.TickSize) - 5 * Chart.ChHeight;
                    if (Z <= 0) return;
                    CurrentScale.Y = LastScaleY * (1 - X);
                }
                else if (vec.Y > 0) CurrentScale.Y = LastScaleY / (1 + X);
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
                Chart.Window.MoveElement(e, vec => TimeScaling(vec));
            }
        }
        private void TimeScaleAll(MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) 
                foreach (var cl in Clips) 
                    cl.ResetTimeScale(); 
            else
            {
                var fl = new List<Func<Vector, Task>>();
                foreach (var cl in Clips)
                {
                    cl.LastScaleX = CurrentScale.X;
                    fl.Add(cl.TimeScaling);
                }
                Chart.Window.MoveElements(e, fl);
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
        public Task TimeScaling(Vector vec)
        {
            return Task.Run(async () =>
            {
                var Y = -vec.X / 50;
                IEnumerable<Candle> currentCandles = null;
                if (vec.X > 0)
                {
                    CurrentScale.X = LastScaleX / (1 - Y);
                    await Dispatcher.InvokeAsync(() => ScaleX.ScaleX = CurrentScale.X);
                }
                else if (vec.X < 0)
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
            Chart.Window.MoveElement(e, vec => MovingChart(vec));
        }
        private void MovingAllCharts(MouseButtonEventArgs e)
        {
            var fl = new List<Func<Vector, Task>>();
            foreach (var cl in Clips)
            {
                cl.LastTranslateVector = CurrentTranslate;
                fl.Add(cl.MovingChart);
            }
            Chart.Window.MoveElements(e, fl);
        }
        public Task MovingChart(Vector vec)
        {
            return Task.Run(async () => 
            {
                var X = LastTranslateVector.X + vec.X / CurrentScale.X;

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
                    CurrentTranslate.Y = LastTranslateVector.Y + vec.Y / CurrentScale.Y;
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
        public event Action WheelScalled;
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
        private int WhellScallingCounter = 0;
        public Task WhellScalling(MouseWheelEventArgs e)
        {
            return Task.Run(async () =>
            {
                if (e.Delta > 0) WhellScallingCounter += 1;
                else WhellScallingCounter -= 1;
                var x = WhellScallingCounter;
                Thread.Sleep(20);
                if (x != WhellScallingCounter || x == 0) return;
                WhellScallingCounter = 0;

                IEnumerable<Candle> currentCandles = null;
                if (x > 0)
                {
                    var nScale = CurrentScale.X * x switch
                    {
                        1 => 1.1,
                        2 => 1.2100000000000002,
                        3 => 1.3310000000000004,
                        4 => 1.4641000000000004,
                        5 => 1.6105100000000006,
                        6 => 1.7715610000000008,
                        7 => 1.9487171000000012,
                        8 => 2.1435888100000016,
                        9 => 2.357947691000002,
                        10 => 2.5937424601000023,
                        11 => 2.8531167061100025,
                        12 => 3.138428376721003,
                        13 => 3.452271214393104,
                        14 => 3.7974983358324144,
                        15 => 4.177248169415656,
                        16 => 4.594972986357222,
                        17 => 5.054470284992945,
                        18 => 5.55991731349224,
                        19 => 6.115909044841464,
                        20 => 6.727499949325611,
                        _ => throw new Exception()
                    };
                    var TimeA = StartTime - Math.Ceiling(((Chart.ChWidth / nScale + CurrentTranslate.X) / 15)) * DeltaTime;
                    var TimeB = StartTime - Math.Floor((CurrentTranslate.X / 15)) * DeltaTime;
                    currentCandles = from c in AllCandles
                                     where c.TimeStamp >= TimeA && c.TimeStamp <= TimeB
                                     select c;


                    if (currentCandles.Count() < 2 || Chart.ChWidth / MaxCandleWidth > (TimeB - TimeA) / DeltaTime) return;
                    CurrentScale.X = nScale;
                }
                else CurrentScale.X *= x switch
                {
                    -1 => 0.9090909090909091,
                    -2 => 0.8264462809917354,
                    -3 => 0.7513148009015775,
                    -4 => 0.6830134553650705,
                    -5 => 0.6209213230591549,
                    -6 => 0.5644739300537772,
                    -7 => 0.5131581182307065,
                    -8 => 0.4665073802097331,
                    -9 => 0.42409761837248455,
                    -10 => 0.3855432894295314,
                    -11 => 0.3504938994813922,
                    -12 => 0.3186308177103565,
                    -13 => 0.2896643797366877,
                    -14 => 0.26333125430607973,
                    -15 => 0.23939204936916333,
                    -16 => 0.2176291357901485,
                    -17 => 0.19784466890013497,
                    -18 => 0.17985878990921358,
                    -19 => 0.16350799082655781,
                    -20 => 0.14864362802414344,
                    _ => throw new Exception()
                };

                await Dispatcher.InvokeAsync(() => ScaleX.ScaleX = CurrentScale.X);
                HorizontalReset(currentCandles);
                NewXScale.Invoke(CurrentScale.X);

                await UpdateMagnetData();
                WheelScalled?.Invoke();
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
}
