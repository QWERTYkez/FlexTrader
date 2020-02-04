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
using System.Diagnostics;
using System.Globalization;
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

            //ChartWindowInitialize
            {
                StartYScaling += mainView.StartYScaling;
                mainView.ScalingY += ScalingY;
                StartMoveChart += mainView.StartMoveChart;
                mainView.Moving += MoveChart;
            }

            //ChartGRDInitialize
            {
                CurrentScale.X = ScaleX.ScaleX;
                CurrentScale.Y = ScaleY.ScaleY;

                var Layers = new List<DrawingCanvas>();

                CandlesLayer = new DrawingCanvas(); Layers.Add(CandlesLayer);
                // add layer
                LayersControl.ItemsSource = Layers;
            }

            PriceLineInitialize();

            //DataContextInitialize
            {
                var DC = DataContext as ChartViewModel;
                DC.PropertyChanged += DContext_PropertyChanged;
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
                HorizontalReset();
            });
        }
        private bool VerticalLock = true;
        private void VerticalReset()
        {
            if (VerticalLock)
            {
                CurrentTranslate.Y = Min + Delta * 0.5 - ChHeight * 0.5;
                CurrentScale.Y = ChHeight / Delta;
                Dispatcher.Invoke(() => 
                { 
                    Translate.Y = CurrentTranslate.Y;
                    ScaleY.ScaleY = CurrentScale.Y; 
                });
            }
            RedrawPrices();
        }
        private void HorizontalReset()
        {
            if (StartTime.HasValue && DeltaTime.HasValue)
            {
                var TimeA = StartTime.Value - Math.Ceiling(((ChWidth / CurrentScale.X + CurrentTranslate.X) / 15)) * DeltaTime.Value;
                var TimeB = StartTime.Value - Math.Floor((CurrentTranslate.X / 15)) * DeltaTime.Value;

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
        private Pen UpPen = new Pen(Brushes.Lime, 4);
        private Pen DownPen = new Pen(Brushes.Red, 4);
        private readonly object parallelkey = new object();
        private void DrawNewCandles(List<Candle> newCandles)
        {
            if (StartTime == null)
                StartTime = newCandles.Last().TimeStamp;

            var DrawTeplates = new List<(Pen ShadowPen, Point PointA,
                Point PointB, Brush BodyBrush, Rect Rect)>();
            Parallel.ForEach(newCandles, c => 
            {
                var open = c.OpenD / TickSize;
                var close = c.CloseD / TickSize;
                var low = c.LowD / TickSize;
                var high = c.HighD / TickSize;
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

        private readonly DrawingCanvas CandlesLayer;

        public event Action<MouseButtonEventArgs> StartMoveChart;
        private void MovingChart(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            LastTranslateCector = CurrentTranslate;
            StartMoveChart?.Invoke(e);
        }
        private Vector LastTranslateCector;
        private Vector CurrentTranslate;
        private Vector CurrentScale;
        private async void MoveChart(Vector vec)
        {
            if (VerticalLock)
            {
                CurrentTranslate.X = LastTranslateCector.X + vec.X / CurrentScale.X;
                await Dispatcher.InvokeAsync(() =>
                {
                    Translate.X = CurrentTranslate.X;
                });
                _ = Task.Run(() => HorizontalReset());
            }
            else
            {
                CurrentTranslate.X = LastTranslateCector.X + vec.X / CurrentScale.X;
                CurrentTranslate.Y = LastTranslateCector.Y + vec.Y / CurrentScale.Y;
                Dispatcher.Invoke(() =>
                {
                    Translate.X = CurrentTranslate.X;
                    Translate.Y = CurrentTranslate.Y;
                });
            }
            
        }

        private async void MouseWheelSpinning(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                CurrentScale.X *= 1.1;
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
            
        }

        //PriceLine
        private void PriceLineInitialize()
        {
            PricesFontSize = 18;
            Font = new Typeface(new FontFamily("Agency FB"), 
                FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            FontBrush = Brushes.White;
            PricePen = new Pen(Brushes.DarkGray, 1);

            PriceGridVisual = new DrawingVisual();
            PriceGridLayer.AddVisual(PriceGridVisual);

            PricesVisual = new DrawingVisual();
            PriceLine.AddVisual(PricesVisual);
        }
        private DrawingVisual PricesVisual;
        private DrawingVisual PriceGridVisual;

        private double PricesFontSize;
        private Brush FontBrush;
        private Typeface Font;
        private Pen PricePen;
        private void RedrawPrices()
        {
            if (ChHeight == 0) return;
            var delta = (ChHeight / CurrentScale.Y);
            var min = Min + (Delta - delta) / 2;

            double count = Math.Floor((ChHeight / (PricesFontSize * 6)));
            var step = (delta * TickSize) / count;
            double n = 1;
            int d = 0;
            while (step > 10)
            {
                step /= 10;
                n *= 10;
            }
            while (step < 1)
            {
                step *= 10;
                n /= 10;
                d += 1;
            }

            if (step > 5) step = 5 * n;
            else if (step > 4) step = 4 * n;
            else if (step > 2.5) { step = 2.5 * n; d += 1; }
            else if (step > 2) step = 2 * n;
            else if (step > 1) step = 1 * n;

            string f = null;
            if (d > 0)
            {
                f = "0.";
                var x = 0;
                while (d != x)
                {
                    f += "0"; x += 1;
                }
            }

            var price = Math.Round(step * Math.Ceiling((min * TickSize) / step), d);
            var coordiate = PriceToHeight(price, delta, min);
            var pricesToDraw = new List<(FormattedText price, Point coor, 
                Pen pen, Point A, Point B, Point G, Point H)>();
            var pixelsPerDip = VisualTreeHelper.GetDpi(PricesVisual).PixelsPerDip;
            do
            {
                var ft = new FormattedText
                        (
                            price.ToString(f),
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            Font,
                            PricesFontSize,
                            FontBrush,
                            pixelsPerDip
                        );
                var Y = coordiate - ft.Height / 2;
                pricesToDraw.Add((ft, 
                    new Point(6, Y), PricePen, 
                    new Point(0, coordiate), new Point(3, coordiate),
                    new Point(0, coordiate), new Point(4096, coordiate)));
                price = Math.Round(price + step, d);
                coordiate = PriceToHeight(price, delta, min);
            } 
            while (coordiate > 0);

            var nWidth = pricesToDraw.Select(p => p.price.Width).Max() + 6;
            Dispatcher.Invoke(() => 
            {
                using var pvc = PricesVisual.RenderOpen();
                using var pgvc = PriceGridVisual.RenderOpen();
                foreach (var pr in pricesToDraw)
                {
                    pvc.DrawText(pr.price, pr.coor);
                    pvc.DrawLine(pr.pen, pr.A, pr.B);
                    pgvc.DrawLine(pr.pen, pr.G, pr.H);
                }
                PriceLine.Width = nWidth;
            });
        }

        private double PriceToHeight(double price, double delta, double min)
        {
            return (ChHeight * (delta * TickSize - price + min * TickSize)) / (delta * TickSize);
        }

        public event Action<MouseButtonEventArgs> StartYScaling;
        private double LastScaleY;
        private void PriceLine_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (e.ClickCount == 2) { VerticalLock = true; VerticalReset(); return; }
            LastScaleY = CurrentScale.Y;
            VerticalLock = false;
            StartYScaling?.Invoke(e);
        }
        private void ScalingY(double Y)
        {
            var X = Y / 50;
            if (Y > 0)
            {
                var Z = ChHeight / (CurrentScale.Y * TickSize) - 5 * ChHeight;
                if (Z > 0)
                {
                    CurrentScale.Y = LastScaleY * (1 + X);
                    ScaleY.ScaleY = CurrentScale.Y;
                    RedrawPrices();
                }
                return;
            }
            else if (Y < 0)
            {
                CurrentScale.Y = LastScaleY / (1 - X);
                ScaleY.ScaleY = CurrentScale.Y;
                RedrawPrices();
                return;
            }
            ScaleY.ScaleY = LastScaleY;
            RedrawPrices();
        }
    }
}
