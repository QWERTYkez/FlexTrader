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
        public event Action<MouseButtonEventArgs, int> StartMoveCursor;

        private readonly IDrawingCanvas CandlesLayer;
        private readonly PriceLineModule PriceLineModule;
        private readonly TimeLineModule TimeLineModule;
        private readonly List<ChartModule> NormalModules;
        private readonly TranslateTransform Translate;
        private readonly ScaleTransform ScaleX;
        private readonly ScaleTransform ScaleY;
        private readonly IChartWindow mainView;
        private readonly Grid ChartGRD;
        private readonly IDrawingCanvas TimeLine;
        private readonly IDrawingCanvas PriceLine;
        public CandlesModule(IChart chart, IDrawingCanvas CandlesLayer, PriceLineModule PriceLineModule,
            TimeLineModule TimeLineModule, List<ChartModule> NormalModules, TranslateTransform Translate,
            ScaleTransform ScaleX, ScaleTransform ScaleY, IChartWindow mainView, Grid ChartGRD, IDrawingCanvas TimeLine,
            IDrawingCanvas PriceLine, Vector CurrentScale)
        {
            this.CandlesLayer = CandlesLayer;
            this.PriceLineModule = PriceLineModule;
            this.TimeLineModule = TimeLineModule;
            this.NormalModules = NormalModules;
            this.Translate = Translate;
            this.ScaleX = ScaleX;
            this.ScaleY = ScaleY;
            this.mainView = mainView;
            this.ChartGRD = ChartGRD;
            this.TimeLine = TimeLine;
            this.PriceLine = PriceLine;
            this.CurrentScale = CurrentScale;

            BaseConstruct(chart);
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
        private void VerticalReset()
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
            PriceLineModule.Redraw();
            foreach (var m in NormalModules) m.Redraw();
        }
        public void HorizontalReset()
        {
            if (StartTime.HasValue && DeltaTime.HasValue)
            {
                var TimeA = StartTime.Value - Math.Ceiling(((Chart.ChWidth / CurrentScale.X + CurrentTranslate.X) / 15)) * DeltaTime.Value;
                var TimeB = StartTime.Value - Math.Floor((CurrentTranslate.X / 15)) * DeltaTime.Value;

                var currentCandles = from c in AllCandles.AsParallel()
                                     where c.TimeStamp >= TimeA && c.TimeStamp <= TimeB
                                     select c;

                if (currentCandles.Count() < 1) return;

                Min = Convert.ToDouble(currentCandles.Select(c => c.Low).Min()) / Chart.TickSize;
                var max = Convert.ToDouble(currentCandles.Select(c => c.High).Max()) / Chart.TickSize;
                var delta = max - Min;
                max += delta * 0.05;
                Min -= delta * 0.05;
                Delta = max - Min;
                if (VerticalLock)
                {
                    PriceLineModule.LastMin = Min;
                    PriceLineModule.LastY = CurrentTranslate.Y;
                    PriceLineModule.LastDelta = Delta;
                }

                VerticalReset();
                TimeLineModule.Redraw();
            }
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
            StartMoveCursor?.Invoke(e, 2);
        }
        private void ScalingY(double Y)
        {
            Task.Run(() =>
            {
                var X = Y / 50;
                if (Y < 0)
                {
                    var Z = Chart.ChHeight / (CurrentScale.Y * Chart.TickSize) - 5 * Chart.ChHeight;
                    if (Z > 0)
                    {
                        CurrentScale.Y = LastScaleY * (1 - X);
                        Dispatcher.Invoke(() => ScaleY.ScaleY = CurrentScale.Y);
                        PriceLineModule.Redraw();
                        foreach (var m in NormalModules) m.Redraw();
                    }
                    return;
                }
                else if (Y > 0)
                {
                    CurrentScale.Y = LastScaleY / (1 + X);
                    Dispatcher.Invoke(() => ScaleY.ScaleY = CurrentScale.Y);
                    PriceLineModule.Redraw();
                    foreach (var m in NormalModules) m.Redraw();
                    return;
                }
                Dispatcher.Invoke(() => ScaleY.ScaleY = CurrentScale.Y);
                PriceLineModule.Redraw();
                foreach (var m in NormalModules) m.Redraw();
            });
        }
        #endregion
        #region скалирование по временной шкале
        private double LastScaleX;
        private void TimeLine_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (e.ClickCount == 2)
            {
                CurrentScale.X = 1;
                ScaleX.ScaleX = 1;
                CurrentTranslate.X = 0;
                Translate.X = 0;
                Task.Run(() => HorizontalReset());
                return;
            }
            LastScaleX = CurrentScale.X;
            StartMoveCursor?.Invoke(e, 3);
        }
        private const double MaxCandleWidth = 175;
        private void ScalingX(double X)
        {
            Task.Run(() =>
            {
                var Y = -X / 50;
                if (X > 0)
                {
                    CurrentScale.X = LastScaleX / (1 - Y);
                    Dispatcher.Invoke(() => ScaleX.ScaleX = CurrentScale.X);
                    HorizontalReset();
                    return;
                }
                else if (X < 0)
                {
                    var nScale = LastScaleX * (1 + Y);
                    var TimeA = StartTime.Value - Math.Ceiling(((Chart.ChWidth / nScale + CurrentTranslate.X) / 15)) * DeltaTime.Value;
                    var TimeB = StartTime.Value - Math.Floor((CurrentTranslate.X / 15)) * DeltaTime.Value;
                    var currentCandles = from c in AllCandles.AsParallel()
                                         where c.TimeStamp >= TimeA && c.TimeStamp <= TimeB
                                         select c;


                    if (currentCandles.Count() < 1 || Chart.ChWidth / MaxCandleWidth > (TimeB - TimeA) / DeltaTime.Value) return;
                    CurrentScale.X = nScale;
                    Dispatcher.Invoke(() => ScaleX.ScaleX = CurrentScale.X);
                    HorizontalReset();
                    return;
                }
                Dispatcher.Invoke(() => ScaleX.ScaleX = LastScaleX);
                HorizontalReset();
            });
        }
        #endregion
        #region перемещение графика 
        public void MovingChart(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            LastTranslateVector = CurrentTranslate;
            StartMoveCursor?.Invoke(e, 1);
        }
        private Vector LastTranslateVector;
        private async void MoveChart(Vector vec)
        {
            var X = LastTranslateVector.X + vec.X / CurrentScale.X;
            var TimeA = StartTime.Value - Math.Floor(((Chart.ChWidth / CurrentScale.X + X) / 15)) * DeltaTime.Value;
            var TimeB = StartTime.Value - Math.Ceiling((X / 15)) * DeltaTime.Value;
            var currentCandles = from c in AllCandles.AsParallel()
                                 where c.TimeStamp >= TimeA && c.TimeStamp <= TimeB
                                 select c;
            if (currentCandles.Count() > 1) goto Move;
            else
            {
                var TSS = AllCandles.AsParallel().Select(c => c.TimeStamp);
                var MaxT = TSS.Max(); var MinT = TSS.Min();
                if (TimeB < MaxT && CurrentTranslate.X < X) goto Move;
                if (TimeA < MinT && CurrentTranslate.X > X) goto Move;
            }
            return;
        Move:
            CurrentTranslate.X = X;
            if (VerticalLock)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    Translate.X = CurrentTranslate.X;
                });
                _ = Task.Run(() => HorizontalReset());
            }
            else
            {
                CurrentTranslate.Y = LastTranslateVector.Y + vec.Y / CurrentScale.Y;
                Dispatcher.Invoke(() =>
                {
                    Translate.X = CurrentTranslate.X;
                    Translate.Y = CurrentTranslate.Y;
                });
                _ = TimeLineModule.Redraw();
                _ = PriceLineModule.Redraw();
                foreach (var m in NormalModules) _ = m.Redraw();
            }
        }
        private void MainView_Moving(Vector vec, int t)
        {
            Task.Run(() =>
            {
                switch (t)
                {
                    case 1: MoveChart(vec); break;
                    case 2: ScalingY(vec.Y); break;
                    case 3: ScalingX(vec.X); break;
                }
            });
        }
        #endregion
        #region Скалирование колесом 
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
            });
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
        private protected override void Construct() 
        {
            StartMoveCursor += mainView.StartMoveCursor;
            ChartGRD.PreviewMouseDown += MovingChart;
            TimeLine.PreviewMouseDown += TimeLine_MouseDown;
            PriceLine.PreviewMouseDown += PriceLine_MouseDown;
            mainView.Moving += MainView_Moving;
        }
        private protected override void Destroy() 
        {
            StartMoveCursor -= mainView.StartMoveCursor;
            ChartGRD.PreviewMouseDown -= MovingChart;
            TimeLine.PreviewMouseDown -= TimeLine_MouseDown;
            PriceLine.PreviewMouseDown -= PriceLine_MouseDown;
            mainView.Moving -= MainView_Moving;
        }

        private Brush UpBrush = Brushes.Lime;
        private Brush DownBrush = Brushes.Red;
        private readonly Pen UpPen = new Pen(Brushes.Lime, 4);
        private readonly Pen DownPen = new Pen(Brushes.Red, 4);

        private protected override void SetsDefinition()
        {
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

            SetsName = "Настройки свечей";

            Setting.SetsLevel(Sets, "Бычья свеча", new Setting[]
            {
                new Setting(SetType.Brush, "Цвет тела", this.UpBrush, SetUpBrush, Brushes.Lime),
                new Setting(SetType.Brush, "Цвет фитиля", this.UpPen.Brush, SetUpPenBrush, Brushes.Lime)
            });

            Setting.SetsLevel(Sets, "Медвежья свеча", new Setting[]
            {
                new Setting(SetType.Brush, "Цвет тела", this.DownBrush, SetDownBrush, Brushes.Red),
                new Setting(SetType.Brush, "Цвет фитиля", this.DownPen.Brush, SetDownPenBrush, Brushes.Red)
            });

            Sets.Add(new Setting(SetType.DoubleSlider, "Толщина фитиля", this.DownPen.Thickness, SetThicknesses, 4d, 2d, 6d));
        }
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
