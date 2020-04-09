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
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FlexTrader.MVVM.Views.ChartModules.Normal
{
    class CursorModule : ChartModule
    {
        private readonly DrawingCanvas CursorLayer;
        private readonly DrawingCanvas TimeLine;
        private readonly DrawingCanvas PriceLine;
        private readonly Grid ChartGRD;
        public CursorModule(IChart chart, Grid ChartGRD, DrawingCanvas CursorLayer, DrawingCanvas TimeLine, DrawingCanvas PriceLine)
        {
            this.CursorLayer = CursorLayer;
            this.TimeLine = TimeLine;
            this.PriceLine = PriceLine;
            this.ChartGRD = ChartGRD;

            FontBrush = Brushes.White;
            MarksPen = new Pen(Brushes.White, 2); MarksPen.Freeze();

            BaseConstruct(chart);
        }

        private readonly DrawingVisual CursorVisual = new DrawingVisual();
        private readonly DrawingVisual CursorTimeVisual = new DrawingVisual();
        private readonly DrawingVisual CursorPriceVisual = new DrawingVisual();
        private readonly TranslateTransform CursorTransform = new TranslateTransform();
        private protected override void Construct()
        {
            ChartGRD.MouseEnter += ShowCursor;
            ChartGRD.MouseLeave += CursorLeave;
            ChartGRD.MouseMove += CursorRedraw;
            SetCursor();
            CursorLayer.RenderTransform = CursorTransform;
        }
        private void ShowCursor(object sender, MouseEventArgs e)
        {
            CursorLayer.AddVisual(CursorVisual);
            TimeLine.AddVisual(CursorTimeVisual);
            PriceLine.AddVisual(CursorPriceVisual);
        }
        private void CursorLeave(object sender, MouseEventArgs e)
        {
            CursorLayer.DeleteVisual(CursorVisual);
            TimeLine.DeleteVisual(CursorTimeVisual);
            PriceLine.DeleteVisual(CursorPriceVisual);
        }
        private void CursorRedraw(object sender, MouseEventArgs e)
        {
            Pos = e.GetPosition(ChartGRD); Redraw();
        }
        private protected override void Destroy()
        {
            ChartGRD.MouseEnter -= ShowCursor;
            ChartGRD.MouseLeave -= CursorLeave;
            ChartGRD.MouseMove -= CursorRedraw;
        }

        private Point Pos;
        public override Task Redraw()
        {
            return Task.Run(() =>
            {
                var dt = Chart.CorrectTimePosition(ref Pos);
                var price = Chart.HeightToPrice(Pos.Y).ToString(Chart.TickPriceFormat);
                Pos.Y = Chart.PriceToHeight(Convert.ToDouble(price));
                price = Chart.HeightToPrice(Pos.Y).ToString(Chart.TickPriceFormat);

                var ft = new FormattedText
                            (
                                price,
                                CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                Chart.FontNumeric,
                                Chart.BaseFontSize,
                                FontBrush,
                                VisualTreeHelper.GetDpi(CursorPriceVisual).PixelsPerDip
                            );
                var Tpont = new Point(Chart.PriceShift + 1, Pos.Y - ft.Height / 2);
                var startPoint = new Point(0, Pos.Y);
                var Points = new Point[]
                {
                        new Point(Chart.PriceShift, Pos.Y + ft.Height / 2),
                        new Point(Chart.PriceLineWidth - 2, Pos.Y + ft.Height / 2),
                        new Point(Chart.PriceLineWidth - 2, Pos.Y - ft.Height / 2),
                        new Point(Chart.PriceShift, Pos.Y - ft.Height / 2)
                };

                var ft2 = new FormattedText
                        (
                            dt.ToString("yy-MM-dd HH:mm"),
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            Chart.FontNumeric,
                            Chart.BaseFontSize,
                            FontBrush,
                            VisualTreeHelper.GetDpi(CursorPriceVisual).PixelsPerDip
                        );
                var Tpont2 = new Point(Pos.X - ft2.Width / 2, Chart.PriceShift + 2);
                var startPoint2 = new Point(Pos.X, 0);
                var Points2 = new Point[]
                {
                        new Point(Pos.X + Chart.PriceShift, Chart.PriceShift),
                        new Point(Pos.X + ft2.Width / 2 + 4, Chart.PriceShift),
                        new Point(Pos.X + ft2.Width / 2 + 4, ft2.Height + 3 + Chart.PriceShift),
                        new Point(Pos.X - ft2.Width / 2 - 4, ft2.Height + 3 + Chart.PriceShift),
                        new Point(Pos.X - ft2.Width / 2 - 4, Chart.PriceShift),
                        new Point(Pos.X - Chart.PriceShift, Chart.PriceShift)
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
                    using var dcP = CursorPriceVisual.RenderOpen();
                    using var dcT = CursorTimeVisual.RenderOpen();

                    CursorTransform.X = Pos.X;
                    CursorTransform.Y = Pos.Y;

                    dcP.DrawGeometry(Chart.ChartBackground, MarksPen, geo);
                    dcP.DrawText(ft, Tpont);

                    dcT.DrawGeometry(Chart.ChartBackground, MarksPen, geo2);
                    dcT.DrawText(ft2, Tpont2);
                });
            });
        }

        private Brush FontBrush;
        private Pen MarksPen;
        private double CursorArea = 25;
        private double CursorDash = 5;
        private double CursorIndent = 2;
        private double CursorThikness = 2;
        public Task SetCursor()
        {
            return Task.Run(() =>
            {
                var rt = new RotateTransform(90); rt.Freeze();
                var pn = new Pen(MarksPen.Brush, CursorThikness); pn.Freeze();

                var Points = new List<Point>();

                if (CursorIndent == 0)
                {
                    var A = new Point(CursorArea, 0);
                    var B = new Point(4096, 0);

                    Dispatcher.Invoke(() =>
                    {
                        using var dcCH = CursorVisual.RenderOpen();

                        dcCH.DrawLine(pn, A, B);
                        dcCH.PushTransform(rt);
                        dcCH.DrawLine(pn, A, B);
                        dcCH.PushTransform(rt);
                        dcCH.DrawLine(pn, A, B);
                        dcCH.PushTransform(rt);
                        dcCH.DrawLine(pn, A, B);
                    });

                    return;
                }

                double s = CursorArea;
                while(s < 4096)
                {
                    Points.Add(new Point(s, 0)); s += CursorDash;
                    Points.Add(new Point(s, 0)); s += CursorIndent;
                }

                Dispatcher.Invoke(() =>
                {
                    using var dcCH = CursorVisual.RenderOpen();

                    for (int i = 0; i < Points.Count; i += 2)
                        dcCH.DrawLine(pn, Points[i], Points[i + 1]);
                    dcCH.PushTransform(rt);
                    for (int i = 0; i < Points.Count; i += 2)
                        dcCH.DrawLine(pn, Points[i], Points[i + 1]);
                    dcCH.PushTransform(rt);
                    for (int i = 0; i < Points.Count; i += 2)
                        dcCH.DrawLine(pn, Points[i], Points[i + 1]);
                    dcCH.PushTransform(rt);
                    for (int i = 0; i < Points.Count; i += 2)
                        dcCH.DrawLine(pn, Points[i], Points[i + 1]);
                });
            });
        }

        private Action<object> SetCursorArea;
        private Action<object> SetCursorDash;
        private Action<object> SetCursorIndent;
        private Action<object> SetCursorThikness;
        private Action<object> SetCursorColor;

        private protected override void SetsDefinition()
        {
            SetCursorArea = b => { CursorArea = (b as double?).Value; SetCursor(); };
            SetCursorDash = b => { CursorDash = (b as double?).Value; SetCursor(); };
            SetCursorIndent = b => { CursorIndent = (b as double?).Value; SetCursor(); };
            SetCursorThikness = b => { CursorThikness = (b as double?).Value; SetCursor(); };
            SetCursorColor = b => { Dispatcher.Invoke(() => { MarksPen = new Pen(b as Brush, 2); MarksPen.Freeze(); }); SetCursor(); };

            SetsName = "Настройки курсора";

            Sets.Add(new Setting(SetType.DoubleSlider, "Радиус", CursorArea, SetCursorArea, 20d, 50d));
            Sets.Add(new Setting(SetType.DoubleSlider, "Штрих", CursorDash, SetCursorDash, 1d, 10d));
            Sets.Add(new Setting(SetType.DoubleSlider, "Отступ", CursorIndent, SetCursorIndent, 0d, 10d));
            Sets.Add(new Setting(SetType.DoubleSlider, "Толщина", CursorThikness, SetCursorThikness, 1d, 5d));
            Sets.Add(new Setting(SetType.Brush, "Цвет курсора", MarksPen.Brush, SetCursorColor));
            Sets.Add(new Setting(SetType.Brush, "Цвет текста", MarksPen.Brush, b => { FontBrush = b as Brush; }));
        }
    }
}
