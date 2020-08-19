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
        private readonly Action<string> ResetInstrument;
        public LevelsModule(IChart chart, PriceMarksModule PMM, Action<string> ResetInstrument) : base(chart)
        {
            Levels = PMM.Levels;
            this.ResetInstrument = ResetInstrument;
            Chart.VerticalСhanges += () => Task.Run(() => ResetPrices());
            SetsName = "Уровни";

            Levels.Marks.CollectionChanged += (s, e) => 
            {
                Sets.Clear();
                for (int i = 0; i < Levels.Marks.Count; i++)
                {
                    Setting.SetsLevel(
                        Sets,
                        $"Level {i + 1}",
                        Levels.Marks[i].GetSets().ToArray());
                }
            };
        }

        public override Task Redraw() => null;
        private protected override void Destroy() { }
        
        private void ResetPrices()
        {
            Hooks = (from x in Levels.Marks
                     select new { Mark = x, Heght = Chart.PriceToHeight(x.Price) }
                               into z
                     where z.Heght > 0 && z.Heght < Chart.ChHeight
                     select new Hook
                     (
                         z.Mark,
                         (x, y) => y - Chart.PriceToHeight(z.Mark.Price),
                         P => new Point(P.X, Chart.PriceToHeight(z.Mark.Price)),
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
                         (point, hv, pv, tv) =>
                         {
                             var br = Dispatcher.Invoke(() => { return Chart.ChartBackground; });
                             var c = ((SolidColorBrush)br).Color;

                             var br2 = new SolidColorBrush(Color.FromArgb(255,
                                 (byte)(c.R - 15),
                                 (byte)(c.G - 15),
                                 (byte)(c.B - 15)));

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
                                      br,
                                      VisualTreeHelper.GetDpi(pv).PixelsPerDip
                                  );

                             var linpen = new Pen(br2, z.Mark.LineThikness + 1); linpen.Freeze();
                             var geopen = new Pen(br2, 3); geopen.Freeze();

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
                                     dc.DrawGeometry(br2, geopen, geo);
                                     dc.DrawText(ft, new Point(Chart.PriceShift + 1, height - ft.Height / 2));
                                 }
                             });
                         },
                         Point =>
                         {
                             var m = z.Mark;
                             m.Price = Chart.HeightToPrice(Point.Y);
                             m.ApplyChanges();
                         }
                     )
                               ).ToList();
        }
        public List<Hook> Hooks { get; private set; } = new List<Hook>();

        public void PaintingLevel(MouseEventArgs e)
        {
            AddLevel(Chart.HeightToPrice(Chart.CurrentCursorPosition.Y), 
                Brushes.White, Brushes.Black, Brushes.Lime, 3);

            if (!Chart.Controlled) 
                ResetInstrument.Invoke(null);
            else 
                Chart.ControlUsed = true;
            ResetPrices();
        }
        public void AddLevel(double Price, SolidColorBrush TextBrush, SolidColorBrush MarkFill,
            SolidColorBrush LineBrush = null, double LineThikness = 0, double LineDash = 0, double LineIndent = 0) => 
            Levels.AddMark(Price, TextBrush, MarkFill, LineBrush, LineThikness, LineDash, LineIndent);
    }
}
