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
using System.Windows;
using System.Windows.Media;

namespace ChartModules.PaintingModule.Elements
{
    public class Level : ChangingElement
    {
        public Level(double Price, SolidColorBrush TextBrush, SolidColorBrush MarkFill,
            SolidColorBrush LineBrush = null, double LineThikness = 0, double LineDash = 0, double LineIndent = 0)
        {
            this.Price = Price;
            this.TextBrush = TextBrush;
            this.MarkFill = MarkFill;
            this.LineBrush = LineBrush;
            this.LineDash = LineDash;
            this.LineIndent = LineIndent;
            this.LineThikness = LineThikness;

            this.TextBrush?.Freeze(); this.MarkFill?.Freeze(); this.LineBrush?.Freeze();
        }

        public override string ElementName { get => "Level"; }
        public override double GetMagnetRadius() => LineThikness / 2 + 2;
        public override bool VisibilityOnChart { get 
            {
                var heght = Chart.PriceToHeight(this.Price);
                if (heght > 0 && heght < Chart.ChHeight) return true;
                else return false;
            } }

        private protected override List<Setting> GetSets()
        {
            return new List<Setting>
            {
                new Setting(SetType.DoublePicker, "Price", () => this.Price, pr => { this.Price = (double)pr; ApplyChangesToAll((double)pr); }),
                new Setting("Line", () => this.LineBrush, br => { this.LineBrush = br as SolidColorBrush; ApplyChangesToAll(); }),
                new Setting("Text", () => this.TextBrush, br => { this.TextBrush = br as SolidColorBrush; ApplyChangesToAll(); }),
                new Setting("Mark", () => this.MarkFill, br => { this.MarkFill = br as SolidColorBrush; ApplyChangesToAll(); }),
                new Setting(SetType.DoubleSlider, "Thickness", () => this.LineThikness, pr => { this.LineThikness = (double)pr; ApplyChangesToAll(); }, 1d, 5d),
                new Setting(SetType.DoubleSlider, "Gap", () => this.LineIndent, pr => { this.LineIndent = (double)pr; ApplyChangesToAll(); }, 0d, 10d),
                new Setting(SetType.DoubleSlider, "Dash", () => this.LineDash, pr => { this.LineDash = (double)pr; ApplyChangesToAll(); }, 1d, 10d)
            };
        }

        public double Price { get; set; }
        public SolidColorBrush TextBrush { get; set; }
        public SolidColorBrush MarkFill { get; set; }
        public SolidColorBrush LineBrush { get; set; }
        public double LineDash { get; set; }
        public double LineIndent { get; set; }
        public double LineThikness { get; set; }

        private protected override double GetDistance(double x, double y)
        {
            return y - Chart.PriceToHeight(this.Price);
        }
        private protected override Point GetHookPoint(Point P)
        {
            return new Point(P.X, Chart.PriceToHeight(this.Price));
        }
        private protected override void DrawShadow(Point point, DrawingVisual ElementsVisual, DrawingVisual PricesVisual, DrawingVisual TimesVisual)
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
                     VisualTreeHelper.GetDpi(PricesVisual).PixelsPerDip
                 );

            var linpen = new Pen(br2, this.LineThikness + 1); linpen.Freeze();
            var geopen = new Pen(br2, 3); geopen.Freeze();

            var linps = new List<Point>();
            if (this.LineIndent == 0)
            {
                linps.Add(new Point(0, height));
                linps.Add(new Point(width, height));
            }
            else
            {
                double s = 0;
                while (s < width)
                {
                    linps.Add(new Point(s, height)); s += this.LineDash;
                    linps.Add(new Point(s, height)); s += this.LineIndent;
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
                using (var dc = ElementsVisual.RenderOpen())
                {
                    for (int i = 0; i < linps.Count; i += 2)
                        dc.DrawLine(linpen, linps[i], linps[i + 1]);
                }
                using (var dc = PricesVisual.RenderOpen())
                {
                    dc.DrawGeometry(br2, geopen, geo);
                    dc.DrawText(ft, new Point(Chart.PriceShift + 1, height - ft.Height / 2));
                }
            });
        }
        public override Action<DrawingContext>[] PrepareToDrawing(Point? point, double PixelsPerDip)
        {
            var c = Dispatcher.Invoke(() => { return ((SolidColorBrush)Chart.ChartBackground).Color; });

            var br = new SolidColorBrush(Color.FromArgb(255,
                (byte)(255 - c.R),
                (byte)(255 - c.G),
                (byte)(255 - c.B)));

            var pricesMax = (Chart.PricesMin + Chart.PricesDelta) * Chart.TickSize;

            double height, price, width;
            if (point != null)
            {
                height = point.Value.Y;
                price = Chart.HeightToPrice(height);
                width = Chart.ChWidth + 2;
            }
            else
            {
                price = this.Price;
                height = Chart.PriceToHeight(price);
                width = 4096;
            }

            var ft = new FormattedText
                 (
                     price.ToString(Chart.TickPriceFormat),
                     CultureInfo.CurrentCulture,
                     FlowDirection.LeftToRight,
                     Chart.FontNumeric,
                     Chart.BaseFontSize,
                     this.TextBrush,
                     PixelsPerDip
                 );

            var linpen = new Pen(this.LineBrush, this.LineThikness); linpen.Freeze();
            var geopen = new Pen(this.LineBrush, 2); geopen.Freeze();

            var linps = new List<Point>();
            if (this.LineIndent == 0)
            {
                linps.Add(new Point(0, height));
                linps.Add(new Point(width, height));
            }
            else
            {
                double s = 0;
                while (s < width)
                {
                    linps.Add(new Point(s, height)); s += this.LineDash;
                    linps.Add(new Point(s, height)); s += this.LineIndent;
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

            return new Action<DrawingContext>[]
                {
                    dc =>
                    {
                        for (int i = 0; i < linps.Count; i += 2)
                            dc.DrawLine(linpen, linps[i], linps[i + 1]);
                    },
                    dc =>
                    {
                        dc.DrawGeometry(this.MarkFill, geopen, geo);
                        dc.DrawText(ft, new Point(Chart.PriceShift + 1, height - ft.Height / 2));
                    },
                    null
                };
        }
        private protected override void NewCoordinates(Point Coordinates)
        {
            this.Price = Chart.HeightToPrice(Coordinates.Y);
        }

        public override List<(string Name, Action Act)> GetContextMenu()
        {
            return new List<(string Name, Action Act)>();
        }
    }
}
