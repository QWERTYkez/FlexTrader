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
                StartMoveCursor += mainView.StartMoveCursor;
                mainView.Moving += vec => 
                {
                    switch (EventType)
                    {
                        case 1: MoveChart(vec); break;
                        case 2: ScalingY(vec.Y); break;
                        case 3: ScalingX(vec.X);  break;
                    }
                };
            }

            //ChartGRDInitialize
            {
                CurrentScale.X = ScaleX.ScaleX;
                CurrentScale.Y = ScaleY.ScaleY;
            }

            FontNumeric = new Typeface(new FontFamily("Agency FB"),
                FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            FontText = new Typeface(new FontFamily("Myriad Pro Cond"),
                FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            PriceLineInitialize();
            TimeLineInitialize();

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
                //CursorPen
                case "CursorBrush": CursorPen = new Pen(DC.CursorBrush, DC.CursorThickness); break;
                case "CursorThickness":
                    if (DC.CursorBrush != null)
                        CursorPen = new Pen(DC.CursorBrush, DC.CursorThickness); break;
                //LinesPen
                case "LinesBrush": LinesPen = new Pen(DC.LinesBrush, DC.LinesThickness); break;
                case "LinesThickness": if (DC.LinesBrush != null) 
                        LinesPen = new Pen(DC.LinesBrush, DC.LinesThickness); break;

                case "ChartBackground": ChartBackground = DC.ChartBackground; break;
                case "FontBrush": FontBrush = DC.FontBrush; break;
                case "BaseFontSize": 
                    {
                        BaseFontSize = DC.BaseFontSize;
                        YearFontSize = Math.Round(DC.BaseFontSize * 1.4);

                        var ft = new FormattedText
                        (
                            "0",
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            FontNumeric,
                            YearFontSize,
                            FontBrush,
                            VisualTreeHelper.GetDpi(TimeLine).PixelsPerDip
                        );
                        TimeLineHeight = Shift + ft.Height;
                        TimeLine.Height = TimeLineHeight;
                    }
                    break;
                case "TickSize":
                    {
                        TickSize = DC.TickSize;
                        CursorPriceFormat = TickSize.ToString().Replace('1', '0').Replace(',', '.');
                    }
                    break;
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

        private int EventType = 0;
        private double TimeLineHeight;
        private Brush ChartBackground;
        private double ChHeight;
        private double ChWidth;
        private double Delta;
        private double Min;
        private int ChangesCounter = 0;
        private readonly List<Candle> AllCandles = new List<Candle>();
        private void ChartGRD_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Task.Run(() => 
            {
                ChHeight = ChartGRD.ActualHeight;
                ChWidth = ChartGRD.ActualWidth;
                ChangesCounter += 1;
                var x = ChangesCounter;
                Thread.Sleep(100);
                if (x != ChangesCounter) return;
                HorizontalReset();
            });
        }
        private bool VerticalLock = true;
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
                if (VerticalLock) 
                {
                    LastMin = Min;
                    LastY = CurrentTranslate.Y;
                    LastDelta = Delta;
                }
                

                VerticalReset();
                RedrawTimeLine();
            }
        }
        private void VerticalReset()
        {
            if (VerticalLock)
            {
                CurrentTranslate.Y = Min + Delta * 0.5 - ChHeight * 0.5;
                LastY = CurrentTranslate.Y;
                CurrentScale.Y = ChHeight / Delta;
                Dispatcher.Invoke(() =>
                {
                    Translate.Y = CurrentTranslate.Y;
                    ScaleY.ScaleY = CurrentScale.Y;
                });
            }
            RedrawPrices();
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

        public event Action<MouseButtonEventArgs> StartMoveCursor;
        private void MovingChart(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            LastTranslateCector = CurrentTranslate;
            EventType = 1;
            StartMoveCursor?.Invoke(e);
        }
        private Vector LastTranslateCector;
        private Vector CurrentTranslate;
        private Vector CurrentScale;
        private async void MoveChart(Vector vec)
        {
            var X = LastTranslateCector.X + vec.X / CurrentScale.X;
            var TimeA = StartTime.Value - Math.Floor(((ChWidth / CurrentScale.X + X) / 15)) * DeltaTime.Value;
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
                CurrentTranslate.Y = LastTranslateCector.Y + vec.Y / CurrentScale.Y;
                Dispatcher.Invoke(() =>
                {
                    Translate.X = CurrentTranslate.X;
                    Translate.Y = CurrentTranslate.Y;
                });
                _ = RedrawTimeLine();
                RedrawPrices();
            }
        }
        private void MouseWheelSpinning(object sender, MouseWheelEventArgs e)
        {
            Task.Run(async () => 
            {
                if (e.Delta > 0)
                {
                    var nScale = CurrentScale.X * 1.1;
                    var TimeA = StartTime.Value - Math.Ceiling(((ChWidth / nScale + CurrentTranslate.X) / 15)) * DeltaTime.Value;
                    var TimeB = StartTime.Value - Math.Floor((CurrentTranslate.X / 15)) * DeltaTime.Value;
                    var currentCandles = from c in AllCandles.AsParallel()
                                         where c.TimeStamp >= TimeA && c.TimeStamp <= TimeB
                                         select c;


                    if (currentCandles.Count() < 1 || ChWidth / MaxCandleSize > (TimeB - TimeA) / DeltaTime.Value) return;
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

        private double BaseFontSize;
        private Brush FontBrush;
        private readonly Typeface FontNumeric;
        private readonly Typeface FontText;
        private Pen LinesPen;
         
        //PriceLine
        private void PriceLineInitialize()
        {
            GridLayer.AddVisual(PriceGridVisual);

            PriceLine.AddVisual(PricesVisual);
        }
        private readonly DrawingVisual PricesVisual = new DrawingVisual();
        private readonly DrawingVisual PriceGridVisual = new DrawingVisual();

        private double PricesDelta;
        private double PricesMin;
        private double LastY;
        private double LastDelta;
        private double LastMin;
        private void RedrawPrices()
        {
            if (ChHeight == 0) return;
            PricesDelta = (ChHeight / CurrentScale.Y);
            PricesMin = LastMin - (LastY - CurrentTranslate.Y) + (LastDelta - PricesDelta) / 2;
            var pixelsPerDip = VisualTreeHelper.GetDpi(PricesVisual).PixelsPerDip;

            double count = Math.Floor((ChHeight / (BaseFontSize * 6)));
            var step = (PricesDelta * TickSize) / count;
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

            var maxP = (PricesMin + PricesDelta) * TickSize;
            var raz = 1;
            while (maxP > 10)
            {
                maxP /= 10;
                raz *= 10;
            }
            var sf = TickSize.ToString().Replace('1', '0').Replace(',', '.');
            var fsf = sf;
            if (raz > 10) 
                for (int i = Convert.ToInt32(Math.Log10(raz)); i > 0; i--)
                    fsf = "0" + fsf;
            var fsfFT = new FormattedText
                        (
                            fsf,
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            FontNumeric,
                            BaseFontSize,
                            FontBrush,
                            pixelsPerDip
                        );

            var price = Math.Round(step * Math.Ceiling((PricesMin * TickSize) / step), d);
            var coordiate = PriceToHeight(price);
            var pricesToDraw = new List<(FormattedText price, Point coor,
                Point A, Point B, Point G, Point H)>();
            
            do
            {
                var ft = new FormattedText
                        (
                            price.ToString(sf),
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            FontNumeric,
                            BaseFontSize,
                            FontBrush,
                            pixelsPerDip
                        );
                var Y = coordiate - ft.Height / 2;
                pricesToDraw.Add((ft, new Point(Shift, Y), 
                    new Point(0, coordiate), new Point(3, coordiate),
                    new Point(0, coordiate), new Point(4096, coordiate)));
                price = Math.Round(price + step, d);
                coordiate = PriceToHeight(price);
            } 
            while (coordiate > 0);

            PriceLineWidth = fsfFT.Width + Shift + 4;
            Dispatcher.Invoke(() => 
            {
                using var pvc = PricesVisual.RenderOpen();
                using var pgvc = PriceGridVisual.RenderOpen();
                foreach (var pr in pricesToDraw)
                {
                    pvc.DrawText(pr.price, pr.coor);
                    pvc.DrawLine(LinesPen, pr.A, pr.B);
                    pgvc.DrawLine(LinesPen, pr.G, pr.H);
                }
                PriceLine.Width = PriceLineWidth;
            });
        }
        private double PriceLineWidth;
        private double PriceToHeight(double price) =>
            (ChHeight * (PricesDelta * TickSize - price + PricesMin * TickSize)) / (PricesDelta * TickSize);

        private double LastScaleY;
        private void PriceLine_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (e.ClickCount == 2) { VerticalLock = true; VerticalReset(); return; }
            LastScaleY = CurrentScale.Y;
            VerticalLock = false;
            EventType = 2;
            StartMoveCursor?.Invoke(e);
        }
        private void ScalingY(double Y)
        {
            Task.Run(() =>
            {
                var X = Y / 50;
                if (Y < 0)
                {
                    var Z = ChHeight / (CurrentScale.Y * TickSize) - 5 * ChHeight;
                    if (Z > 0)
                    {
                        CurrentScale.Y = LastScaleY * (1 - X);
                        Dispatcher.Invoke(() => ScaleY.ScaleY = CurrentScale.Y);
                        RedrawPrices();
                    }
                    return;
                }
                else if (Y > 0)
                {
                    CurrentScale.Y = LastScaleY / (1 + X);
                    Dispatcher.Invoke(() => ScaleY.ScaleY = CurrentScale.Y);
                    RedrawPrices();
                    return;
                }
                Dispatcher.Invoke(() => ScaleY.ScaleY = CurrentScale.Y);
                RedrawPrices();
            });
        }

        //TimeLine
        private DateTime TimeA = DateTime.Now;
        private DateTime TimeB = DateTime.Now;
        private void TimeLineInitialize()
        {
            GridLayer.AddVisual(TimeGridVisual);

            TimeLine.AddVisual(TimesVisual);
        }
        private readonly DrawingVisual TimesVisual = new DrawingVisual();
        private readonly DrawingVisual TimeGridVisual = new DrawingVisual();

        private Task RedrawTimeLine()
        {
            if (ChWidth == 0) return null;
            return Task.Run(() => 
            {
                TimeA = StartTime.Value - ((ChWidth / CurrentScale.X + CurrentTranslate.X - 7.5) / 15) * DeltaTime.Value;
                TimeB = StartTime.Value - ((CurrentTranslate.X - 7.5) / 15) * DeltaTime.Value;

                double count = Math.Floor((ChWidth / (BaseFontSize * 10)));
                var step = (TimeB - TimeA) / count;
                int Ystep = 0; int Mstep = 0; int Dstep = 0; int Hstep = 0; int Mnstep = 0;

                if (step.Days > 3650) { Ystep = 10; }
                else if (step.Days > 3285) { Ystep = 9; }
                else if (step.Days > 2920) { Ystep = 8; }
                else if (step.Days > 2555) { Ystep = 7; }
                else if (step.Days > 2190) { Ystep = 6; }
                else if (step.Days > 1825) { Ystep = 5; }
                else if (step.Days > 1460) { Ystep = 4; }
                else if (step.Days > 1095) { Ystep = 3; }
                else if (step.Days > 730) { Ystep = 2; }
                else if (step.Days > 365) { Ystep = 1; }
                else if (step.Days > 240) { Mstep = 8; }
                else if (step.Days > 180) { Mstep = 6; }
                else if (step.Days > 120) { Mstep = 4; }
                else if (step.Days > 90) { Mstep = 3; }
                else if (step.Days > 60) { Mstep = 2; }
                else if (step.Days > 30) { Mstep = 1; }
                else if (step.Days > 15) { Dstep = 15; }
                else if (step.Days > 10) { Dstep = 10; }
                else if (step.Days > 6) { Dstep = 6; }
                else if (step.Days > 5) { Dstep = 5; }
                else if (step.Days > 3) { Dstep = 3; }
                else if (step.Days > 2) { Dstep = 2; }
                else if (step.Days > 1) { Dstep = 1; }
                else if (step.TotalHours > 16) { Hstep = 16; }
                else if (step.TotalHours > 12) { Hstep = 12; }
                else if (step.TotalHours > 8) { Hstep = 8; }
                else if (step.TotalHours > 6) { Hstep = 6; }
                else if (step.TotalHours > 4) { Hstep = 4; }
                else if (step.TotalHours > 3) { Hstep = 3; }
                else if (step.TotalHours > 2) { Hstep = 2; }
                else if (step.TotalHours > 1) { Hstep = 1; }
                else if (step.TotalMinutes > 30) { Mnstep = 30; }
                else if (step.TotalMinutes > 15) { Mnstep = 15; }
                else if (step.TotalMinutes > 10) { Mnstep = 10; }
                else if (step.TotalMinutes > 5) { Mnstep = 5; }
                else if (step.TotalMinutes > 4) { Mnstep = 4; }
                else if (step.TotalMinutes > 2) { Mnstep = 2; }
                else if (step.TotalMinutes > 1) { Mnstep = 1; }

                var pixelsPerDip = VisualTreeHelper.GetDpi(PricesVisual).PixelsPerDip;
                var timesToDraw = new List<(FormattedText Text, 
                    Point Tpoint, Point A, Point B, Point G, Point H)>();

                var stTime = new DateTime(TimeA.Year, 1, 1);
                var Y = StartTime.Value.Year;
                if (Ystep > 0)
                {
                    var Yn = Y;
                    while (Yn > TimeA.Year) Yn -= Ystep;
                    Yn += Ystep;
                    while (Yn < TimeB.Year)
                    {
                        AddYear(timesToDraw, Yn, pixelsPerDip);
                        Yn += Ystep;
                    }
                }
                else 
                {
                    var M = StartTime.Value.Month;
                    if (Mstep > 0)
                    {
                        var currentDT = new DateTime(Y, M, 1);
                        while (currentDT > TimeA) currentDT = currentDT.AddMonths(-Mstep);
                        currentDT = currentDT.AddMonths(Mstep);
                        while (currentDT <= TimeB)
                        {
                            AddMounth(timesToDraw, currentDT, pixelsPerDip);
                            currentDT = currentDT.AddMonths(Mstep);
                        }
                    }
                    else
                    {
                        DateTime ndt;
                        var D = StartTime.Value.Day;
                        if (Dstep > 0)
                        {
                            var currentDT = new DateTime(TimeA.Year, TimeA.Month, 1);

                            if (Dstep > 4)
                            {
                                while (currentDT <= TimeB)
                                {
                                    AddDay(timesToDraw, currentDT, pixelsPerDip);
                                    ndt = currentDT.AddDays(Dstep);
                                    if (ndt.Month != currentDT.Month || ndt.Day > 28)
                                        currentDT = new DateTime(ndt.Year, ndt.Month, 1);
                                    if (ndt.Day > 28)
                                        currentDT = new DateTime(ndt.Year, ndt.Month, 1).AddMonths(1);
                                    else currentDT = ndt;
                                }
                            }
                            else
                            {
                                while (currentDT <= TimeB)
                                {
                                    AddDay(timesToDraw, currentDT, pixelsPerDip);
                                    ndt = currentDT.AddDays(Dstep);
                                    if (ndt.Month != currentDT.Month)
                                        currentDT = new DateTime(ndt.Year, ndt.Month, 1);
                                    else currentDT = ndt;
                                }
                            }
                        }
                        else
                        {
                            var H = StartTime.Value.Hour;
                            if (Hstep > 0)
                            {
                                if (Hstep == 16)
                                {
                                    var currentDT = new DateTime(StartTime.Value.Year, StartTime.Value.Month, StartTime.Value.Day, 0, 0, 0);
                                    while (currentDT > TimeA) currentDT = currentDT.AddHours(-Hstep);
                                    currentDT = currentDT.AddHours(Hstep);
                                    while (currentDT <= TimeB)
                                    {
                                        AddHourMinute(timesToDraw, currentDT, pixelsPerDip);
                                        currentDT = currentDT.AddHours(Hstep);
                                    }
                                }
                                else
                                {
                                    var currentDT = new DateTime(TimeA.Year, TimeA.Month, TimeA.Day, 0, 0, 0);
                                    while (currentDT <= TimeB)
                                    {
                                        AddHourMinute(timesToDraw, currentDT, pixelsPerDip);
                                        currentDT = currentDT.AddHours(Hstep);
                                    }
                                }
                            }
                            else
                            {
                                var currentDT = new DateTime(TimeA.Year, TimeA.Month, TimeA.Day, 0, 0, 0);
                                while (currentDT <= TimeB)
                                {
                                    AddHourMinute(timesToDraw, currentDT, pixelsPerDip);
                                    currentDT = currentDT.AddMinutes(Mnstep);
                                }
                            }
                        }
                    }
                }

                if (timesToDraw.Count > 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        using var tvc = TimesVisual.RenderOpen();
                        using var tgvc = TimeGridVisual.RenderOpen();
                        foreach (var (Text, Tpoint, A, B, G, H) in timesToDraw)
                        {
                            tvc.DrawText(Text, Tpoint);
                            tvc.DrawLine(LinesPen, A, B);
                            tgvc.DrawLine(LinesPen, G, H);
                        }
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        using var tvc = TimesVisual.RenderOpen();
                        using var tgvc = TimeGridVisual.RenderOpen();
                    });
                }
            });
        }
        private double YearFontSize;
        private void AddYear(dynamic container, int Y, double pixelsPerDip)
        {
            var ft = new FormattedText
                        (
                            Y.ToString(),
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            FontNumeric,
                            YearFontSize,
                            FontBrush,
                            pixelsPerDip
                        );
            var width = TimeToWidth(new DateTime(Y, 1, 1));
            if (width < 0 || width > ChWidth) return;
            container.Add((ft, new Point(width - ft.Width / 2, Shift),
                new Point(width, 0), new Point(width, Shift),
                new Point(width, 0), new Point(width, 4096)));
        }
        private void AddMounth(dynamic container, DateTime dt, double pixelsPerDip)
        {
            if (dt.Month == 1) AddYear(container, dt.Year, pixelsPerDip);
            else
            {
                var ft = new FormattedText
                        (
                            dt.ToString("MMMM"),
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            FontText,
                            BaseFontSize,
                            FontBrush,
                            pixelsPerDip
                        );
                var width = TimeToWidth(dt);
                if (width < 0 || width > ChWidth) return;
                container.Add((ft, new Point(width - ft.Width / 2, Shift),
                    new Point(width, 0), new Point(width, Shift),
                    new Point(width, 0), new Point(width, 4096)));
            }
        }
        private void AddDay(dynamic container, DateTime dt, double pixelsPerDip)
        {
            if (dt.Day == 1) AddMounth(container, dt, pixelsPerDip);
            else
            {
                var ft = new FormattedText
                        (
                            dt.Day.ToString(),
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            FontNumeric,
                            BaseFontSize,
                            FontBrush,
                            pixelsPerDip
                        );
                var width = TimeToWidth(dt);
                if (width < 0 || width > ChWidth) return;
                container.Add((ft, new Point(width - ft.Width / 2, Shift),
                    new Point(width, 0), new Point(width, Shift),
                    new Point(width, 0), new Point(width, 4096)));
            }
        }
        private void AddHourMinute(dynamic container, DateTime dt, double pixelsPerDip)
        {
            if (dt.Hour == 0 && dt.Minute == 0) AddDay(container, dt, pixelsPerDip);
            else
            {
                var ft = new FormattedText
                        (
                            dt.ToString("H:mm"),
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            FontNumeric,
                            BaseFontSize,
                            FontBrush,
                            pixelsPerDip
                        );
                var width = TimeToWidth(dt);
                if (width < 0 || width > ChWidth) return;
                container.Add((ft, new Point(width - ft.Width / 2, Shift),
                    new Point(width, 0), new Point(width, Shift),
                    new Point(width, 0), new Point(width, 4096)));
            }
        }
        private double TimeToWidth(DateTime dt) => ChWidth * ((dt - TimeA) / (TimeB - TimeA)) + 2;
        private DateTime WidthToTime(double width) => TimeA + ((width - 2) / ChWidth) * (TimeB - TimeA);

        private double LastScaleX;
        private const double MaxCandleSize = 175;
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
            EventType = 3;
            StartMoveCursor?.Invoke(e);
        }
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
                    var TimeA = StartTime.Value - Math.Ceiling(((ChWidth / nScale + CurrentTranslate.X) / 15)) * DeltaTime.Value;
                    var TimeB = StartTime.Value - Math.Floor((CurrentTranslate.X / 15)) * DeltaTime.Value;
                    var currentCandles = from c in AllCandles.AsParallel()
                                         where c.TimeStamp >= TimeA && c.TimeStamp <= TimeB
                                         select c;
                    

                    if (currentCandles.Count() < 1 || ChWidth / MaxCandleSize > (TimeB - TimeA) / DeltaTime.Value) return;
                    CurrentScale.X = nScale;
                    Dispatcher.Invoke(() => ScaleX.ScaleX = CurrentScale.X);
                    HorizontalReset();
                    return;
                }
                Dispatcher.Invoke(() => ScaleX.ScaleX = LastScaleX);
                HorizontalReset();
            });
        }

        //курсор
        private Pen CursorPen;
        private const double Shift = 6;
        private readonly DrawingVisual CursorVerticalVisual = new DrawingVisual();
        private readonly DrawingVisual CursorHorizontalVisual = new DrawingVisual();
        private readonly DrawingVisual CursorTimeVisual = new DrawingVisual();
        private readonly DrawingVisual CursorPriceVisual = new DrawingVisual();
        private void ShowCursor(object sender, MouseEventArgs e)
        {
            CursorLayer.AddVisual(CursorVerticalVisual);
            CursorLayer.AddVisual(CursorHorizontalVisual);
            TimeLine.AddVisual(CursorTimeVisual);
            PriceLine.AddVisual(CursorPriceVisual);
        }
        private void CursorLeave(object sender, MouseEventArgs e)
        {
            CursorLayer.ClearVisuals();
            TimeLine.DeleteVisual(CursorTimeVisual);
            PriceLine.DeleteVisual(CursorPriceVisual);
        }
        private DateTime LastCurosrDT;
        private string LastCurosrPrice;
        private double LastCursorPosX;
        private double LastCursorPosY;
        private string CursorPriceFormat;
        private void CursorRedraw(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(this);
            Task.Run(() =>
            {
                var dt = CorrectCursorPosition(ref pos);
                var price = HeightToPrice(pos.Y).ToString(CursorPriceFormat);
                pos.Y = PriceToHeight(Convert.ToDouble(price));
                price = HeightToPrice(pos.Y).ToString(CursorPriceFormat);

                if (LastCurosrPrice != price || LastCursorPosY != pos.X)
                {
                    LastCurosrPrice = price; LastCursorPosY = pos.X;

                    var ft = new FormattedText
                                (
                                    price,
                                    CultureInfo.CurrentCulture,
                                    FlowDirection.LeftToRight,
                                    FontNumeric,
                                    BaseFontSize,
                                    FontBrush,
                                    VisualTreeHelper.GetDpi(PricesVisual).PixelsPerDip
                                );
                    var Tpont = new Point(Shift + 1, pos.Y - ft.Height / 2);
                    var startPoint = new Point(0, pos.Y);
                    var Points = new Point[]
                    {
                        new Point(Shift, pos.Y + ft.Height / 2),
                        new Point(PriceLineWidth - 2, pos.Y + ft.Height / 2),
                        new Point(PriceLineWidth - 2, pos.Y - ft.Height / 2),
                        new Point(Shift, pos.Y - ft.Height / 2)
                    };

                    var PointA2 = new Point(0, pos.Y);
                    var PointB2 = new Point(ChWidth + 2, pos.Y);

                    Dispatcher.Invoke(() =>
                    {
                        var geo = new PathGeometry(new[] { new PathFigure(startPoint,
                            new[]
                            {
                                new LineSegment(Points[0], true),
                                new LineSegment(Points[1], true),
                                new LineSegment(Points[2], true),
                                new LineSegment(Points[3], true)
                            },
                            true)
                        });

                        using var dcCH = CursorHorizontalVisual.RenderOpen();
                        using var dcP = CursorPriceVisual.RenderOpen();

                        dcCH.DrawLine(CursorPen, PointA2, PointB2);

                        dcP.DrawGeometry(ChartBackground, CursorPen, geo);
                        dcP.DrawText(ft, Tpont);
                    });
                }
                if (LastCurosrDT != dt || LastCursorPosX != pos.X)
                {
                    LastCurosrDT = dt; LastCursorPosX = pos.X;

                    var PointA1 = new Point(pos.X, 0);
                    var PointB1 = new Point(pos.X, ChHeight);

                    var ft = new FormattedText
                            (
                                dt.ToString("yy-MM-dd HH:mm"),
                                CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                FontNumeric,
                                BaseFontSize,
                                FontBrush,
                                VisualTreeHelper.GetDpi(PricesVisual).PixelsPerDip
                            );
                    var Tpont = new Point(pos.X - ft.Width / 2, Shift + 2);
                    var startPoint = new Point(pos.X, 0);
                    var Points = new Point[]
                    {
                        new Point(pos.X + Shift, Shift),
                        new Point(pos.X + ft.Width / 2 + 4, Shift),
                        new Point(pos.X + ft.Width / 2 + 4, ft.Height + 3 + Shift),
                        new Point(pos.X - ft.Width / 2 - 4, ft.Height + 3 + Shift),
                        new Point(pos.X - ft.Width / 2 - 4, Shift),
                        new Point(pos.X - Shift, Shift)
                    };

                    Dispatcher.Invoke(() =>
                    {
                        var geo = new PathGeometry(new[] { new PathFigure(startPoint,
                            new[]
                            {
                                new LineSegment(Points[0], true),
                                new LineSegment(Points[1], true),
                                new LineSegment(Points[2], true),
                                new LineSegment(Points[3], true),
                                new LineSegment(Points[4], true),
                                new LineSegment(Points[5], true)
                            },
                            true)
                        });

                        using var dcCH = CursorVerticalVisual.RenderOpen();
                        using var dcT = CursorTimeVisual.RenderOpen();

                        dcCH.DrawLine(CursorPen, PointA1, PointB1);

                        dcT.DrawGeometry(ChartBackground, CursorPen, geo);
                        dcT.DrawText(ft, Tpont);
                    });
                }
            });
        }
        private double HeightToPrice(double height) =>
            PricesMin * TickSize + PricesDelta * (ChHeight * TickSize - TickSize * height) / ChHeight;
        private DateTime CorrectCursorPosition(ref double X)
        {
            var dt = StartTime.Value -
                Math.Round((StartTime.Value - WidthToTime(X)) / DeltaTime.Value) * DeltaTime.Value;
            X = TimeToWidth(dt);
            return dt;
        }
        private DateTime CorrectCursorPosition(ref Point pos)
        {
            var dt = StartTime.Value -
                Math.Round((StartTime.Value - WidthToTime(pos.X)) / DeltaTime.Value) * DeltaTime.Value;
            pos.X = TimeToWidth(dt);
            return dt;
        }
    }
}