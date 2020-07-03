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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ChartModules.PaintingModules
{
    public class LevelsModule : ChartModule, IHooksModule
    {
        private readonly MarksLayer Levels;
        private readonly Action ResetInstrument;
        public LevelsModule(IChart chart, PriceMarksModule PMM, Action ResetInstrument) : base(chart)
        {
            Levels = PMM.Levels;
            this.ResetInstrument = ResetInstrument;
            Chart.VerticalСhanges += () => Task.Run(() => ResetPrices());
        }

        public override Task Redraw() => null;
        private protected override void Destroy() { }

        private protected override void SetsDefinition()
        {
        }

        private void ResetPrices()
        {
            Hooks = (from x in Levels.Marks
                     select new { Mark = x, Heght = Chart.PriceToHeight(x.Price) }
                               into z
                     where z.Heght > 0 && z.Heght < Chart.ChHeight
                     select new Hook
                     (
                         (x, y) => y - Chart.PriceToHeight(z.Mark.Price),
                         (point, hv, pv, tv) =>
                         {

                             var c = Dispatcher.Invoke(() => { return ((SolidColorBrush)Chart.ChartBackground).Color; });

                             var br = new SolidColorBrush(Color.FromArgb(255,
                                 (byte)(255 - c.R),
                                 (byte)(255 - c.G),
                                 (byte)(255 - c.B)));

                             var pricesMax = (Chart.PricesMin + Chart.PricesDelta) * Chart.TickSize;
                             var width = Chart.ChWidth + 2;
                             var height = point.Y;

                             var price = Chart.HeightToPrice(height);
                             var ft = new FormattedText
                                  (
                                      Chart.HeightToPrice(height).ToString(Chart.TickPriceFormat),
                                      CultureInfo.CurrentCulture,
                                      FlowDirection.LeftToRight,
                                      Chart.FontNumeric,
                                      Chart.BaseFontSize,
                                      z.Mark.TextBrush,
                                      VisualTreeHelper.GetDpi(pv).PixelsPerDip
                                  );

                             var linpen = new Pen(z.Mark.LineBrush, z.Mark.LineThikness); linpen.Freeze();
                             var geopen = new Pen(z.Mark.LineBrush, 2); geopen.Freeze();

                             var linps = new List<Point>();
                             if (z.Mark.LineIndent == 0)
                             {
                                 linps.Add(new Point(0, height));
                                 linps.Add(new Point(width, height));
                             }
                             else
                             {
                                 double s = 0;
                                 while (s < width)
                                 {
                                     linps.Add(new Point(s, height)); s += z.Mark.LineDash;
                                     linps.Add(new Point(s, height)); s += z.Mark.LineIndent;
                                 }
                             }

                             var geo = new PathGeometry(new[] { new PathFigure(new Point(0, height),
                                            new[]
                                            {
                                                new LineSegment(new Point(Chart.PriceShift, height + ft.Height / 2), true),
                                                new LineSegment(new Point(Chart.PriceLineWidth - 2, height + ft.Height / 2), true),
                                                new LineSegment(new Point(Chart.PriceLineWidth - 2, height - ft.Height / 2), true),
                                                new LineSegment(new Point(Chart.PriceShift, height - ft.Height / 2), true)
                                            },
                                            true)}); geo.Freeze();

                             Dispatcher.Invoke(() =>
                             {
                                 using (var dc = hv.RenderOpen())
                                 {
                                     for (int i = 0; i < linps.Count; i += 2)
                                         dc.DrawLine(linpen, linps[i], linps[i + 1]);
                                 }
                                 using (var dc = pv.RenderOpen())
                                 {
                                     dc.DrawGeometry(z.Mark.MarkFill, geopen, geo);
                                     dc.DrawText(ft, new Point(Chart.PriceShift + 1, height - ft.Height / 2));
                                 }
                             });
                         },
                         (point, vec, hv, pv, tv) =>
                         {
                             hv.Transform = new TranslateTransform(0, vec.Y);

                             var height = point.Y + vec.Y;
                             var ft = new FormattedText
                              (
                                  Chart.HeightToPrice(height).ToString(Chart.TickPriceFormat),
                                  CultureInfo.CurrentCulture,
                                  FlowDirection.LeftToRight,
                                  Chart.FontNumeric,
                                  Chart.BaseFontSize,
                                  z.Mark.TextBrush,
                                  VisualTreeHelper.GetDpi(pv).PixelsPerDip
                              );
                             var geopen = new Pen(z.Mark.LineBrush, 2); geopen.Freeze();
                             var geo = new PathGeometry(new[] { new PathFigure(new Point(0, height),
                                            new[]
                                            {
                                                new LineSegment(new Point(Chart.PriceShift, height + ft.Height / 2), true),
                                                new LineSegment(new Point(Chart.PriceLineWidth - 2, height + ft.Height / 2), true),
                                                new LineSegment(new Point(Chart.PriceLineWidth - 2, height - ft.Height / 2), true),
                                                new LineSegment(new Point(Chart.PriceShift, height - ft.Height / 2), true)
                                            },
                                            true)}); geo.Freeze();

                             Dispatcher.Invoke(() =>
                             {
                                 using (var dc = pv.RenderOpen())
                                 {
                                     dc.DrawGeometry(z.Mark.MarkFill, geopen, geo);
                                     dc.DrawText(ft, new Point(Chart.PriceShift + 1, height - ft.Height / 2));
                                 }
                             });
                         },
                         () => new Point(0, Chart.PriceToHeight(z.Mark.Price)),
                         Point => 
                         {
                             var m = z.Mark;
                             m.Price = Chart.HeightToPrice(Point.Y);
                             m.ApplyChanges();
                         },
                         z.Mark.LineThikness / 2 + 2
                     )
                               ).ToList();
        }
        public List<Hook> Hooks { get; private set; } = new List<Hook>();

        public void PaintingLevel(MouseButtonEventArgs e)
        {
            Levels.AddMark(Chart.HeightToPrice(Chart.CurrentCursorPosition.Y), 
                Brushes.White, Brushes.Black, Brushes.Lime, 3);

            ResetInstrument.Invoke();
            ResetPrices();
        }
    }
}
