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
using System.Windows.Media;

namespace ChartModules.StandardModules
{
    public class PriceLineModule : ChartModule
    {
        public double PricesDelta { get; private set; }
        public double PricesMin { get; private set; }
        public double PriceLineWidth { get; private set; }
        public double LastY;
        public double LastDelta;
        public double LastMin;

        private readonly ColumnDefinition PriceLineCD;
        private readonly IDrawingCanvas GridLayer;
        private readonly IDrawingCanvas PriceLine;
        public PriceLineModule(IChart chart, ColumnDefinition PriceLineCD, IDrawingCanvas GridLayer, IDrawingCanvas PriceLine)
        {
            this.PriceLineCD = PriceLineCD;
            this.GridLayer = GridLayer;
            this.PriceLine = PriceLine;

            BaseConstruct(chart);
        }
        private Pen LinesPen => Chart.LinesPen;

        private readonly DrawingVisual PricesVisual = new DrawingVisual();
        private readonly DrawingVisual PriceGridVisual = new DrawingVisual();
        private protected override void Construct()
        {
            Chart.FontBrushChanged += () => Redraw();
            GridLayer.AddVisual(PriceGridVisual);
            PriceLine.AddVisual(PricesVisual);
        }
        private protected override void Destroy()
        {
            Chart.FontBrushChanged -= () => Redraw();
            GridLayer.DeleteVisual(PriceGridVisual);
            PriceLine.DeleteVisual(PricesVisual);
        }
        
        public override Task Redraw()
        {
            return Task.Run(() => 
            {
                if (Chart.ChHeight == 0) return;
                PricesDelta = (Chart.ChHeight / Chart.CurrentScale.Y);
                PricesMin = LastMin - (LastY - Chart.CurrentTranslate.Y) + (LastDelta - PricesDelta) / 2;
                var pixelsPerDip = VisualTreeHelper.GetDpi(PricesVisual).PixelsPerDip;

                double count = Math.Floor((Chart.ChHeight / (Chart.BaseFontSize * 6)));
                var step = (Chart.PricesDelta * Chart.TickSize) / count;
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

                var maxP = (Chart.PricesMin + Chart.PricesDelta) * Chart.TickSize;
                var raz = 1;
                while (maxP > 10)
                {
                    maxP /= 10;
                    raz *= 10;
                }
                var sf = Chart.TickSize.ToString().Replace('1', '0').Replace(',', '.');
                var fsf = sf;
                if (raz > 10)
                    for (int i = Convert.ToInt32(Math.Log10(raz)); i > 0; i--)
                        fsf = "0" + fsf;
                var fsfFT = new FormattedText
                            (
                                fsf,
                                CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                Chart.FontNumeric,
                                Chart.BaseFontSize,
                                Chart.FontBrush,
                                pixelsPerDip
                            );

                var price = Math.Round(step * Math.Ceiling((Chart.PricesMin * Chart.TickSize) / step), d);
                var coordiate = Chart.PriceToHeight(price);
                var pricesToDraw = new List<(FormattedText price, Point coor,
                    Point A, Point B, Point G, Point H)>();

                do
                {
                    var ft = new FormattedText
                            (
                                price.ToString(sf),
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
                    price = Math.Round(price + step, d);
                    coordiate = Chart.PriceToHeight(price);
                }
                while (coordiate > 0);

                PriceLineWidth = fsfFT.Width + Chart.PriceShift + 4;
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
                    PriceLineCD.Width = new GridLength(PriceLineWidth);
                });
            });
        }

        private protected override void SetsDefinition()
        {
            //SetsName = "Настройки шкалы времени";
        }
    }
}