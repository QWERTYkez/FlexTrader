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
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FlexTrader.MVVM.Views.ChartModules.Normal
{
    class CursorModule : ChartModuleNormal
    {
        private readonly DrawingCanvas CursorLayer;
        private readonly DrawingCanvas TimeLine;
        private readonly DrawingCanvas PriceLine;
        private readonly Grid ChartGRD;
        public CursorModule(INormalChart chart, Grid ChartGRD, DrawingCanvas CursorLayer, DrawingCanvas TimeLine, DrawingCanvas PriceLine)
        {
            this.CursorLayer = CursorLayer;
            this.TimeLine = TimeLine;
            this.PriceLine = PriceLine;
            this.ChartGRD = ChartGRD;

            FontBrush = Brushes.White;
            CursorPen = new Pen(Brushes.White, 2);

            BaseConstruct(chart);
        }

        private Pen cursorPen;
        public Pen CursorPen 
        {
            get => cursorPen;
            set 
            {
                value?.Freeze();
                cursorPen = value;
            } 
        }
        private Brush fontBrush;
        private Brush FontBrush
        {
            get => fontBrush;
            set
            {
                value?.Freeze();
                fontBrush = value;
            }
        }
        private const double CursorArea = 25;

        private readonly DrawingVisual CursorVerticalVisual = new DrawingVisual();
        private readonly DrawingVisual CursorHorizontalVisual = new DrawingVisual();
        private readonly DrawingVisual CursorTimeVisual = new DrawingVisual();
        private readonly DrawingVisual CursorPriceVisual = new DrawingVisual();
        private protected override void Construct()
        {
            ChartGRD.MouseEnter += ShowCursor;
            ChartGRD.MouseLeave += CursorLeave;
            ChartGRD.MouseMove += CursorRedraw;
        }
        private void ShowCursor(object sender, MouseEventArgs e)
        {
            CursorLayer.AddVisual(CursorVerticalVisual);
            CursorLayer.AddVisual(CursorHorizontalVisual);
            TimeLine.AddVisual(CursorTimeVisual);
            PriceLine.AddVisual(CursorPriceVisual);
        }
        private void CursorLeave(object sender, MouseEventArgs e)
        {
            CursorLayer.DeleteVisual(CursorVerticalVisual);
            CursorLayer.DeleteVisual(CursorHorizontalVisual);
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

                var PointA2 = new Point(0, Pos.Y);
                var PointB2 = new Point(Pos.X - CursorArea, Pos.Y);
                var PointC2 = new Point(Pos.X + CursorArea, Pos.Y);
                var PointD2 = new Point(Chart.ChWidth + 2, Pos.Y);

                var PointA1 = new Point(Pos.X, 0);
                var PointB1 = new Point(Pos.X, Pos.Y - CursorArea);
                var PointC1 = new Point(Pos.X, Pos.Y + CursorArea);
                var PointD1 = new Point(Pos.X, Chart.ChHeight);

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
                    using var dcCH = CursorHorizontalVisual.RenderOpen();
                    using var dcP = CursorPriceVisual.RenderOpen();
                    using var dcT = CursorTimeVisual.RenderOpen();

                    dcCH.DrawLine(CursorPen, PointA2, PointB2);
                    dcCH.DrawLine(CursorPen, PointC2, PointD2);
                    dcCH.DrawLine(CursorPen, PointA1, PointB1);
                    dcCH.DrawLine(CursorPen, PointC1, PointD1);

                    dcP.DrawGeometry(Chart.ChartBackground, CursorPen, geo);
                    dcP.DrawText(ft, Tpont);

                    dcT.DrawGeometry(Chart.ChartBackground, CursorPen, geo2);
                    dcT.DrawText(ft2, Tpont2);
                });
            });
        }
    }
}
