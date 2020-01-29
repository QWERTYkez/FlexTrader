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

using FlexTrader.Exchanges;
using FlexTrader.MVVM.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlexTrader.MVVM.Views
{
    public partial class ChartView : UserControl
    {
        public ChartView() { } //конструктор для intellisense
        public ChartView(ChartWindow mainView)
        {
            InitializeComponent();
            {
                StartMoveChart += mainView.StartMoveChart;
                mainView.Moving += MoveChart;
            }
            {
                var DC = DataContext as ChartViewModel;
                DC.PropertyChanged += DContext_PropertyChanged;
                {
                    var Layers = new List<DrawingCanvas>();
                    CandlesLayer = new DrawingCanvas(); Layers.Add(CandlesLayer);
                    // add layer
                    LayersControl.ItemsSource = Layers;
                }
                DC.Inicialize();
            }
        }

        private void DContext_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var DC = sender as ChartViewModel;

            switch (e.PropertyName)
            {
                case "TickSize": TickSize = DC.TickSize; break;
                case "NewCandles":
                    {
                        if (DC.NewCandles != null && DC.NewCandles.Count > 0)
                        {
                            if (!DeltaTime.HasValue) DeltaTime = 
                                    (DC.NewCandles[1].TimeStamp - DC.NewCandles[0].TimeStamp);
                            else
                            {
                                var ts = DC.NewCandles[1].TimeStamp - DC.NewCandles[0].TimeStamp;
                                if (DeltaTime.Value != ts) DeltaTime = ts;
                            }
                            AllCandles.AddRange(DC.NewCandles);
                            DrawNewCandles(DC.NewCandles);
                        }
                    }
                    break;
            }
        }

        private double ChHeight => ChartGRD.ActualHeight;
        private double ChWidth => ChartGRD.ActualWidth;
        private double Delta;
        private double Min;
        private int ChangesCounter = 0;
        private readonly List<Candle> AllCandles = new List<Candle>();
        private void ChartGRD_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Task.Run(() => 
            {
                ChangesCounter += 1;
                var x = ChangesCounter;
                Thread.Sleep(100);
                if (x != ChangesCounter) return;
                if (e.WidthChanged) HorizontalReset();
                else if (e.HeightChanged) VerticalReset();
            });
        }
        private bool VerticalLock = true;
        private void VerticalReset()
        {
            if (VerticalLock)
            {
                var Y = Min + Delta * 0.5 - ChHeight * 0.5;
                var ScaleY = ChHeight / Delta;
                Dispatcher.Invoke(() => { Translate.Y = Y; Scale.ScaleY = ScaleY; });
            }
        }
        private async void HorizontalReset()
        {
            if (StartTime.HasValue && DeltaTime.HasValue)
            {
                DateTime TimeA = DateTime.Now;
                DateTime TimeB = DateTime.Now;
                await Dispatcher.InvokeAsync(() => 
                {
                    TimeA = StartTime.Value - Math.Ceiling((ChWidth + Translate.X) / 15) * DeltaTime.Value;
                    TimeB = StartTime.Value - Math.Ceiling(Translate.X / 15) * DeltaTime.Value;
                });

                var currentCandles = from c in AllCandles.AsParallel()
                                     where c.TimeStamp >= TimeA && c.TimeStamp <= TimeB
                                     select c;

                if (currentCandles.Count() < 1) return;

                Min = Convert.ToDouble(currentCandles.Select(c => c.Low).Min()) / TickSize;
                var max = Convert.ToDouble(currentCandles.Select(c => c.High).Max()) / TickSize;
                var delta = max - Min;
                max += delta * 0.05;
                Min -= delta * 0.05;
                Delta = max - Min;

                VerticalReset();
            }
        }
        private DateTime? StartTime;
        private TimeSpan? DeltaTime;
        private double TickSize = 0.00000001;
        private Brush UpBrush = Brushes.Lime;
        private Brush DownBrush = Brushes.Red;
        private Pen ShadowPen = new Pen(Brushes.Gray, 2);
        private readonly object parallelkey = new object();
        private void DrawNewCandles(List<Candle> newCandles)
        {
            if (StartTime == null)
                StartTime = newCandles.Last().TimeStamp;

            var DrawTeplates = new List<(Point PointA,
                Point PointB, Brush BodyBrush, Rect Rect)>();
            Parallel.ForEach(newCandles, c => 
            {
                var open = c.OpenD / TickSize;
                var close = c.CloseD / TickSize;
                var low = c.LowD / TickSize;
                var high = c.HighD / TickSize;
                var x = 7.5 + (StartTime.Value - c.TimeStamp) * 15 / DeltaTime.Value;
                var x1 = x + 5;
                var x2 = x - 5;

                var PointA = new Point(x, low);
                var PointB = new Point(x, high);
                var Rect = new Rect(new Point(x1, open), new Point(x2, close));

                lock (parallelkey)
                {
                    if (c.UP) DrawTeplates.Add((PointA, PointB, UpBrush, Rect));
                    else DrawTeplates.Add((PointA, PointB, DownBrush, Rect));
                }
            });

            Dispatcher.Invoke(() =>
            {
                var dvisual = new DrawingVisual();
                CandlesLayer.AddVisual(dvisual);
                CandlesLayer.Background = Brushes.Black;

                using var dc = dvisual.RenderOpen();
                foreach (var dt in DrawTeplates)
                {
                    dc.DrawLine(ShadowPen, dt.PointA, dt.PointB);
                    dc.DrawRectangle(dt.BodyBrush, null, dt.Rect);
                }
            });

            VerticalReset();
        }
        
        private readonly DrawingCanvas CandlesLayer;

        public event Action<ChartView, MouseButtonEventArgs> StartMoveChart;
        private async void ChartGRD_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            await Dispatcher.InvokeAsync(() => LastTranslateCector = new Vector(Translate.X, Translate.Y));
            StartMoveChart?.Invoke(this, e);
        }
        private Vector LastTranslateCector;
        private void MoveChart(Vector vec)
        {
            if (VerticalLock)
            {
                Dispatcher.Invoke(() =>
                {
                    Translate.X = LastTranslateCector.X + vec.X;
                    Task.Run(() => HorizontalReset());
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    Translate.X = LastTranslateCector.X + vec.X;
                    Translate.Y = LastTranslateCector.Y + vec.Y / Scale.ScaleY;
                });
            }
            
        }
    }
}
