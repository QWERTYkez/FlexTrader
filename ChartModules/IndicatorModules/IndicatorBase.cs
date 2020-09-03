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

using ChartModules.StandardModules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ChartModules.IndicatorModules
{
    public abstract class IndicatorBase : ChartModule
    {
        public IndicatorBase(IChart Chart, Grid BaseGrd, Grid ScaleGrd, DrawingCanvas CursorLinesLayer, DrawingCanvas TimeLine) : base(Chart)
        {
            this.BaseGrd = BaseGrd;
            this.TimeMarkLayer = TimeLine;
            this.CursorLinesLayer = CursorLinesLayer;

            pixelsPerDip = VisualTreeHelper.GetDpi(ScaleVisual).PixelsPerDip;
            IndicatorCanvas.AddVisual(BaseIndicatorVisual);
            GridCanvas.AddVisual(GridVisual);
            ScaleCanvas.AddVisual(ScaleVisual);

            BaseGrd.MouseEnter += CursorShow;
            BaseGrd.MouseLeave += CursorLeave;
            BaseGrd.MouseMove += CursorRedraw;
            BaseGrd.MouseEnter += (s, e) => Chart.Moving = Chart.MovingChart;
            BaseGrd.MouseLeave += (s, e) => { if (Chart.Moving == Chart.MovingChart) Chart.Moving = null; };

            BaseGrd.Children.Add(GridCanvas);
            BaseGrd.ClipToBounds = true;
            {
                var L2grd = new Grid();
                {
                    var L3grd = new Grid
                    {
                        RenderTransformOrigin = new Point(1, 0.5),
                        RenderTransform = ScX
                    };
                    {
                        var L4grd = new Grid { RenderTransformOrigin = new Point(0.5, 0.5) };
                        {
                            var tgr = new TransformGroup();
                            {
                                tgr.Children.Add(new RotateTransform(180));
                                tgr.Children.Add(Translate);
                                tgr.Children.Add(ScY);
                            }
                            L4grd.RenderTransform = tgr;
                            L4grd.Children.Add(IndicatorCanvas);
                        }
                        L3grd.Children.Add(L4grd);
                    }
                    L2grd.Children.Add(L3grd);
                }
                BaseGrd.Children.Add(L2grd);
            }
            BaseGrd.Children.Add(CursorLayer);

            ScaleGrd.ClipToBounds = true;
            ScaleGrd.Children.Add(ScaleCanvas);
            ScaleGrd.Children.Add(ValueMarkLayer);

            CursorLinesTransform = (TranslateTransform)CursorLinesLayer.RenderTransform;
            CursorLayer.RenderTransform = CursorTransform;

            Chart.CandlesChanged += ac => Task.Run(() => CandlesChanged(ac));
            Chart.AllHorizontalReset += HorizontalReset;
            Chart.NewXScale += sc => Task.Run(() => Dispatcher.Invoke(() => ScaleX = sc));
            Chart.NewXTrans += tr => Task.Run(() => Dispatcher.Invoke(() => Translate.X = tr));
            Chart.NewFSF += fsf => RedrawScale();
        }
        private readonly ScaleTransform ScX = new ScaleTransform();
        private readonly ScaleTransform ScY = new ScaleTransform();
        private readonly Grid BaseGrd;
        private readonly DrawingCanvas IndicatorCanvas = new DrawingCanvas();
        private readonly DrawingCanvas GridCanvas = new DrawingCanvas();
        private readonly DrawingCanvas ScaleCanvas = new DrawingCanvas();
        private double ScaleX { set => ScX.ScaleX = value; }
        private double ScaleY { set => ScY.ScaleY = value; }
        private readonly TranslateTransform Translate = new TranslateTransform();
        private readonly DrawingVisual GridVisual = new DrawingVisual();
        private readonly DrawingVisual ScaleVisual = new DrawingVisual();
        private readonly double pixelsPerDip;

        private readonly DrawingCanvas CursorLinesLayer;
        private readonly DrawingCanvas CursorLayer = new DrawingCanvas();
        private readonly DrawingCanvas TimeMarkLayer;
        private readonly DrawingCanvas ValueMarkLayer = new DrawingCanvas();
        private readonly DrawingVisual CursorTimeVisual = new DrawingVisual();
        private readonly DrawingVisual CursorValueVisual = new DrawingVisual();
        private readonly TranslateTransform CursorLinesTransform = new TranslateTransform();
        private readonly TranslateTransform CursorTransform = new TranslateTransform();
        private void CursorShow(object sender, MouseEventArgs e)
        {
            CursorLinesLayer.AddVisual(Chart.CursorLinesVisual);
            CursorLayer.AddVisual(Chart.CursorVisual);
            TimeMarkLayer.AddVisual(CursorTimeVisual);
            ValueMarkLayer.AddVisual(CursorValueVisual);
        }
        private void CursorLeave(object sender, MouseEventArgs e)
        {
            CursorLinesLayer.DeleteVisual(Chart.CursorLinesVisual);
            CursorLayer.DeleteVisual(Chart.CursorVisual);
            TimeMarkLayer.DeleteVisual(CursorTimeVisual);
            ValueMarkLayer.DeleteVisual(CursorValueVisual);
        }
        private CursorPosition CursorPosition { get; } = new CursorPosition();
        private void CursorRedraw(object s, MouseEventArgs e)
        {
            var npos = CursorPosition.Current = e.GetPosition(BaseGrd);
            var vec = e.GetPosition(Chart.ChartGrid) - npos;
            Task.Run(() =>
            {
                DateTime dt = DateTime.Now; string value = "";

                dt = CorrectTimePosition(ref npos);
                value = HeightToValue(npos.Y).ToString(sf);
                
                CursorPosition.Corrected = npos;
                CursorPosition.NMP();

                var ft = new FormattedText
                            (
                                value + suff,
                                CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                Chart.FontNumeric,
                                Chart.BaseFontSize,
                                Chart.CursorFontBrush,
                                pixelsPerDip
                            );
                var Tpont = new Point(Chart.PriceShift + 1, CursorPosition.Magnet.Y - ft.Height / 2);
                var startPoint = new Point(0, CursorPosition.Magnet.Y);
                var Points = new Point[]
                {
                        new Point(Chart.PriceShift, CursorPosition.Magnet.Y + ft.Height / 2),
                        new Point(Chart.PriceLineWidth - 2, CursorPosition.Magnet.Y + ft.Height / 2),
                        new Point(Chart.PriceLineWidth - 2, CursorPosition.Magnet.Y - ft.Height / 2),
                        new Point(Chart.PriceShift, CursorPosition.Magnet.Y - ft.Height / 2)
                };
                var ft2 = new FormattedText
                        (
                            dt.ToString("yy-MM-dd HH:mm"),
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            Chart.FontNumeric,
                            Chart.BaseFontSize,
                            Chart.CursorFontBrush,
                            pixelsPerDip
                        );
                var Tpont2 = new Point(CursorPosition.Magnet.X - ft2.Width / 2, Chart.PriceShift + 2);
                var startPoint2 = new Point(CursorPosition.Magnet.X, 0);
                var Points2 = new Point[]
                {
                        new Point(CursorPosition.Magnet.X + Chart.PriceShift, Chart.PriceShift),
                        new Point(CursorPosition.Magnet.X + ft2.Width / 2 + 4, Chart.PriceShift),
                        new Point(CursorPosition.Magnet.X + ft2.Width / 2 + 4, ft2.Height + 3 + Chart.PriceShift),
                        new Point(CursorPosition.Magnet.X - ft2.Width / 2 - 4, ft2.Height + 3 + Chart.PriceShift),
                        new Point(CursorPosition.Magnet.X - ft2.Width / 2 - 4, Chart.PriceShift),
                        new Point(CursorPosition.Magnet.X - Chart.PriceShift, Chart.PriceShift)
                };

                var geo = new PathGeometry(new[] { new PathFigure(startPoint,
                            new[]
                            {
                                new LineSegment(Points[0], true),
                                new LineSegment(Points[1], true),
                                new LineSegment(Points[2], true),
                                new LineSegment(Points[3], true)
                            },
                            true)
                        }); geo.Freeze();
                var geo2 = new PathGeometry(new[] { new PathFigure(startPoint2,
                            new[]
                            {
                                new LineSegment(Points2[0], true),
                                new LineSegment(Points2[1], true),
                                new LineSegment(Points2[2], true),
                                new LineSegment(Points2[3], true),
                                new LineSegment(Points2[4], true),
                                new LineSegment(Points2[5], true)
                            },
                            true)
                        }); geo2.Freeze();

                Dispatcher.Invoke(() =>
                {
                    using var dcP = CursorValueVisual.RenderOpen();
                    using var dcT = CursorTimeVisual.RenderOpen();

                    CursorLinesTransform.X = (CursorPosition.Magnet + vec).X;
                    CursorLinesTransform.Y = (CursorPosition.Magnet + vec).Y;
                    CursorTransform.X = CursorPosition.Current.X;
                    CursorTransform.Y = CursorPosition.Current.Y;

                    dcP.DrawGeometry(Chart.ChartBackground, Chart.CursorMarksPen, geo);
                    dcP.DrawText(ft, Tpont);

                    dcT.DrawGeometry(Chart.ChartBackground, Chart.CursorMarksPen, geo2);
                    dcT.DrawText(ft2, Tpont2);
                });
            });
        }

        private protected readonly DrawingVisual BaseIndicatorVisual = new DrawingVisual();
        private protected List<ICandle> AllCandles { get => Chart.AllCandles; }
        private protected double GrdHeight { get => BaseGrd.ActualHeight; }
        private protected double GrdWidth { get => BaseGrd.ActualWidth; }
        //private protected double ScaleWidth { get => ScaleGrd.ActualWidth; }
        private protected DateTime StartTime { get => Chart.StartTime; }
        private protected TimeSpan DeltaTime { get => Chart.DeltaTime; }

        private protected abstract double gmin(IEnumerable<ICandle> currentCandles);
        private protected abstract double gmax(IEnumerable<ICandle> currentCandles);
        private protected abstract void CandlesChanged(List<ICandle> allCandles);
        private protected abstract void Redraw();

        private double Min;
        private double Delta;
        private double CurrTrY;
        private double CurrScY;
        private protected async void VerticalReset()
        {
            LastY = CurrTrY = Min + Delta * 0.5 - GrdHeight * 0.5;
            CurrScY = GrdHeight / Delta;

            Dispatcher.Invoke(() =>
            {
                Translate.Y = CurrTrY;
                ScaleY = CurrScY;
            });

            await RedrawScale();
        }
        public void HorizontalReset(bool HeightChanged, IEnumerable<ICandle> currentCandles)
        {
            var mmm = gmin(currentCandles);
            var max = gmax(currentCandles);
            var delta = max - mmm;
            max += delta * 0.05;
            var nMin = mmm - delta * 0.05;
            var nDelta = max - nMin;
            if (Min == nMin && Delta == nDelta && !HeightChanged) return;
            Min = nMin;
            Delta = nDelta;

            LastMin = Min;
            LastY = CurrTrY;
            LastDelta = Delta;

            VerticalReset();
        }

        private double LastMin;
        private double LastDelta;
        private double LastY;
        private double ValuesDelta;
        private double ValuesMin;
        private int digits;
        private int kilos;
        private string suff;
        private string sf;
        private Task RedrawScale()
        {
            return Task.Run(() =>
            {
                if (GrdHeight == 0) return;
                ValuesDelta = (GrdHeight / CurrScY);
                ValuesMin = LastMin - (LastY - CurrTrY) + (LastDelta - ValuesDelta) / 2;

                double count = Math.Floor((GrdHeight / (Chart.BaseFontSize * 6)));
                var step = ValuesDelta / count;

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

                var nums = Math.Max(ValuesMin.ToString().Split(",")[0].Length,
                        (ValuesMin + ValuesDelta).ToString().Split(",")[0].Length);

                var chars = Chart.FSF.Replace(".", "").Length;

                digits = chars - nums;
                kilos = 0;

                if (digits < 0) 
                {
                    digits--;
                    while (digits < 0)
                    {
                        kilos++;
                        digits += 3;
                    }
                }
                switch (kilos)
                {
                    case 0: suff = ""; break;
                    case 1: suff = "K"; break;
                    case 2: suff = "M"; break;
                    case 3: suff = "G"; break;
                    case 4: suff = "T"; break;
                }

                var val = Math.Round(step * Math.Ceiling((ValuesMin) / step), d);
                var coordiate = ValueToHeight(val);
                var pricesToDraw = new List<(FormattedText val, Point coor,
                    Point A, Point B, Point G, Point H)>();

                sf = "0";
                if (digits > 0)
                {
                    sf += ".";
                    for (int i = 0; i < digits; i++)
                    {
                        sf += "0";
                    }
                }
                
                do
                {
                    var ft = new FormattedText
                            (
                                HeightToValue(coordiate).ToString(sf) + suff,
                                CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                Chart.FontNumeric,
                                Chart.BaseFontSize,
                                Chart.FontBrush,
                                pixelsPerDip
                            );
                    var Y = coordiate - ft.Height / 2;
                    pricesToDraw.Add((ft, new Point(Chart.PriceShift, Y),
                        new Point(0, coordiate), new Point(3, coordiate),
                        new Point(0, coordiate), new Point(4096, coordiate)));
                    val = Math.Round(val + step, d);
                    coordiate = ValueToHeight(val);
                }
                while (coordiate > 0);

                Dispatcher.Invoke(() =>
                {
                    using var pvc = ScaleVisual.RenderOpen();
                    using var pgvc = GridVisual.RenderOpen();
                    foreach (var pr in pricesToDraw)
                    {
                        pvc.DrawText(pr.val, pr.coor);
                        pvc.DrawLine(Chart.LinesPen, pr.A, pr.B);
                        pgvc.DrawLine(Chart.LinesPen, pr.G, pr.H);
                    }
                });
            });
        }

        private double ValueToHeight(in double value) =>
            (GrdHeight * (ValuesDelta - value + ValuesMin)) / ValuesDelta;
        private DateTime CorrectTimePosition(ref Point pos)
        {
            var dt = StartTime -
                Math.Round((StartTime - WidthToTime(pos.X)) / DeltaTime) * DeltaTime;
            pos.X = TimeToWidth(dt);
            return dt;
        }
        private DateTime WidthToTime(in double width)
        {
            if (GrdWidth == 0) return Chart.TimeA + (Chart.TimeB - Chart.TimeA) / 2;
            return Chart.TimeA + ((width) / GrdWidth) * (Chart.TimeB - Chart.TimeA);
        }
        private double TimeToWidth(in DateTime dt) =>
            GrdWidth * ((dt - Chart.TimeA) / (Chart.TimeB - Chart.TimeA));
        public double HeightToValue(in double height) =>
            Math.Round((ValuesMin + ValuesDelta * (GrdHeight - height) / GrdHeight) / Math.Pow(1000,kilos), digits);
    }
}
