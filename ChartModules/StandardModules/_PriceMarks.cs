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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ChartModules.StandardModules
{
    public class PriceMarksModule : ChartModule
    {
        public readonly ObservableCollection<PriceMark> Marks = new ObservableCollection<PriceMark>();

        private readonly IDrawingCanvas MarksLayer;
        private readonly IDrawingCanvas PriceLine;
        public PriceMarksModule(IChart chart, IDrawingCanvas MarksLayer, IDrawingCanvas PriceLine)
        {
            this.MarksLayer = MarksLayer;
            this.PriceLine = PriceLine;

            Marks.CollectionChanged += Marks_CollectionChanged;

            BaseConstruct(chart);
        }

        private readonly DrawingVisual PriceMarksVisual = new DrawingVisual();
        private readonly DrawingVisual ChartMarksVisual = new DrawingVisual();
        private protected override void Construct()
        {
            MarksLayer.AddVisual(ChartMarksVisual);
            PriceLine.AddVisual(PriceMarksVisual);
        }
        private protected override void Destroy()
        {
            MarksLayer.DeleteVisual(ChartMarksVisual);
            PriceLine.DeleteVisual(PriceMarksVisual);
        }
        private void Marks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            => Redraw();

        public override Task Redraw()
        {
            return Task.Run(() =>
            {
                if (Marks != null)
                {
                    var pricesMax = (Chart.PricesMin + Chart.PricesDelta) * Chart.TickSize;
                    var marksData = new List<(Point A, Point B, FormattedText ft, Brush Fill, Point T, Pen pen, PathGeometry geo)>();
                    foreach (var mark in Marks)
                    {
                        if (mark.Price > Chart.PricesMin * Chart.TickSize && mark.Price < pricesMax)
                        {
                            var height = Chart.PriceToHeight(mark.Price);

                            var ft = new FormattedText
                                            (
                                                mark.Price.ToString(Chart.TickPriceFormat),
                                                CultureInfo.CurrentCulture,
                                                FlowDirection.LeftToRight,
                                                Chart.FontNumeric,
                                                Chart.BaseFontSize,
                                                mark.TextBrush,
                                                VisualTreeHelper.GetDpi(PriceMarksVisual).PixelsPerDip
                                            );

                            var pen = new Pen(mark.LineBrush, 2); pen.Freeze();
                            var geo = new PathGeometry(new[] { new PathFigure(new Point(0, height),
                                    new[]
                                    {
                                        new LineSegment(new Point(Chart.PriceShift, height + ft.Height / 2), true),
                                        new LineSegment(new Point(Chart.PriceLineWidth - 2, height + ft.Height / 2), true),
                                        new LineSegment(new Point(Chart.PriceLineWidth - 2, height - ft.Height / 2), true),
                                        new LineSegment(new Point(Chart.PriceShift, height - ft.Height / 2), true)
                                    },
                                    true)
                                }); geo.Freeze();

                            marksData.Add((
                                    new Point(0, height),
                                    new Point(Chart.ChWidth + 2, height),
                                    ft,
                                    mark.Fill,
                                    new Point(Chart.PriceShift + 1, height - ft.Height / 2),
                                    pen,
                                    geo
                                    ));
                        }
                    }

                    Dispatcher.Invoke(() =>
                    {
                        using var dcCH = ChartMarksVisual.RenderOpen();
                        using var dcP = PriceMarksVisual.RenderOpen();

                        foreach (var (A, B, ft, Fill, T, pen, geo) in marksData)
                        {
                            dcCH.DrawLine(pen, A, B);

                            dcP.DrawGeometry(Fill, pen, geo);
                            dcP.DrawText(ft, T);
                        }
                    });
                }
            });
        }

        private protected override void SetsDefinition()
        {
            //////////////////
        }
    }

    public class PriceMark
    {
        private Brush textBrush;
        private Brush fill;
        private Brush lineBrush;

        public PriceMark(double Price, Brush TextBrush, Brush Fill, Brush LineBrush = null)
        {
            this.Price = Price;
            this.TextBrush = TextBrush;
            this.Fill = Fill;
            this.LineBrush = LineBrush;
        }

        public double Price { get; set; }
        public Brush TextBrush 
        { 
            get => textBrush; 
            set
            {
                value?.Freeze();
                textBrush = value;
            }
        }
        public Brush Fill
        {
            get => fill;
            set
            {
                value?.Freeze();
                fill = value;
            }
        }
        public Brush LineBrush
        {
            get => lineBrush;
            set
            {
                value?.Freeze();
                lineBrush = value;
            }
        }
    }
}