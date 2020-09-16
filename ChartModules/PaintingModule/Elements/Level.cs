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
using System.Windows.Media;

namespace ChartModules.PaintingModule.Elements
{
    public class Level : PaintingElement
    {
        private static SolidColorBrush StTextBrush { get; set; } = Brushes.White;
        private static SolidColorBrush StMarkFill { get; set; } = Brushes.Black;
        private static SolidColorBrush StLineBrush { get; set; } = Brushes.White;
        private static int StLineThikness { get; set; } = 3;
        private static int StLineDash { get; set; } = 5;
        private static int StLineIndent { get; set; } = 0;
        
        static Level()
        {
            StTextBrush?.Freeze(); StMarkFill?.Freeze(); StLineBrush?.Freeze();
        }

        public static List<Setting> StGetSets()
        {
            return new List<Setting>
            {
                new Setting("Line", () => StLineBrush, br => { StLineBrush = br; }),
                new Setting("Text", () => StTextBrush, br => { StTextBrush = br; }),
                new Setting("Mark", () => StMarkFill, br => { StMarkFill = br; }),
                new Setting(IntType.Slider, "Thickness", () => StLineThikness, pr => { StLineThikness = pr; }, 1, 5),
                new Setting(IntType.Slider, "Gap", () => StLineIndent, pr => { StLineIndent = pr; }, 0, 10),
                new Setting(IntType.Slider, "Dash", () => StLineDash, pr => { StLineDash = pr; }, 1, 10)
            };
        }

        public static void DrawPrototype(IChart Chart,
            DrawingVisual ChartVisual, DrawingVisual PriceVisual, DrawingVisual TimeVisual)
        {
            Task.Run(() => 
            {
                var pricesMax = (Chart.PricesMin + Chart.PricesDelta) * Chart.TickSize;

                double height = Chart.CursorPosition.Magnet_Current.Y;
                double price = Chart.HeightToPrice(height);
                double width = Chart.ChWidth + 2;

                var ft = new FormattedText
                     (
                         price.ToString(Chart.TickPriceFormat),
                         CultureInfo.CurrentCulture,
                         FlowDirection.LeftToRight,
                         Chart.FontNumeric,
                         Chart.BaseFontSize,
                         StTextBrush,
                         VisualTreeHelper.GetDpi(PriceVisual).PixelsPerDip
                     );

                var linpen = new Pen(StLineBrush, StLineThikness); linpen.Freeze();
                var geopen = new Pen(StLineBrush, 2); geopen.Freeze();

                var linps = new List<Point>();
                if (StLineIndent == 0)
                {
                    linps.Add(new Point(0, height));
                    linps.Add(new Point(width, height));
                }
                else
                {
                    double z = 0;
                    while (z < width)
                    {
                        linps.Add(new Point(z, height)); z += StLineDash;
                        linps.Add(new Point(z, height)); z += StLineIndent;
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

                var pA = new Point(0, height);
                var pB = new Point(width, height);

                Chart.Dispatcher.Invoke(() =>
                {
                    using (var dc = ChartVisual.RenderOpen())
                    {
                        dc.DrawLine(new Pen(Chart.ChartBackground, StLineThikness + 2), pA, pB);
                        for (int i = 0; i < linps.Count; i += 2)
                            dc.DrawLine(linpen, linps[i], linps[i + 1]);
                    }
                    using (var dc = PriceVisual.RenderOpen())
                    {
                        dc.DrawGeometry(StMarkFill, geopen, geo);
                        dc.DrawText(ft, new Point(Chart.PriceShift + 1, height - ft.Height / 2));
                    }
                });
            });
        }

        public Level(double Price) : this()
        {
            this.Price = Price;
            this.TextBrush = StTextBrush;
            this.MarkFill = StMarkFill;
            this.LineBrush = StLineBrush;
            this.LineDash = StLineDash;
            this.LineIndent = StLineIndent;
            this.LineThikness = StLineThikness;
        }
        public Level(double Price, SolidColorBrush TextBrush, SolidColorBrush MarkFill, SolidColorBrush LineBrush, double LineThikness, double LineDash = 0, double LineIndent = 0) : this()
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
        private Level()
        {
            Sets.Add(new Setting("Price", () => this.Price, pr => { this.Price = pr; ApplyChangesToAll(pr); }));
            Sets.Add(new Setting("Line", () => this.LineBrush, br => { this.LineBrush = br; ApplyChangesToAll(); }));
            Sets.Add(new Setting("Text", () => this.TextBrush, br => { this.TextBrush = br; ApplyChangesToAll(); }));
            Sets.Add(new Setting("Mark", () => this.MarkFill, br => { this.MarkFill = br; ApplyChangesToAll(); }));
            Sets.Add(new Setting(IntType.Slider, "Thickness", () => (int)this.LineThikness, pr => { this.LineThikness = pr; ApplyChangesToAll(); }, 1, 5));
            Sets.Add(new Setting(IntType.Slider, "Gap", () => (int)this.LineIndent, pr => { this.LineIndent = pr; ApplyChangesToAll(); }, 0, 10));
            Sets.Add(new Setting(IntType.Slider, "Dash", () => (int)this.LineDash, pr => { this.LineDash = pr; ApplyChangesToAll(); }, 1, 10));
        }

        public override string ElementName { get => "Level"; }
        public override double GetMagnetRadius() => LineThikness / 2 + 2;
        public override bool VisibilityOnChart { get 
            {
                var heght = Chart.PriceToHeight(this.Price);
                if (heght > 0 && heght < Chart.ChHeight) return true;
                else return false;
            } }

        private double Price { get; set; }
        private double NPrice { get; set; }
        private SolidColorBrush TextBrush { get; set; }
        private SolidColorBrush MarkFill { get; set; }
        private SolidColorBrush LineBrush { get; set; }
        private double LineDash { get; set; }
        private double LineIndent { get; set; }
        private double LineThikness { get; set; }

        private protected override double GetDistance(Point P)
        {
            return P.Y - Chart.PriceToHeight(this.Price);
        }
        private protected override Point GetHookPoint(Point P)
        {
            return new Point(P.X, Chart.PriceToHeight(this.Price));
        }
        private protected override void DrawShadow(DrawingVisual ElementsVisual, DrawingVisual PricesVisual, DrawingVisual TimesVisual)
        {
            var c = Dispatcher.Invoke(() => { return ((SolidColorBrush)Chart.ChartBackground).Color; });
            var br = new SolidColorBrush(c);

            var br2 = new SolidColorBrush(Color.FromArgb(255,
                (byte)(c.R - 15),
                (byte)(c.G - 15),
                (byte)(c.B - 15)));

            var pricesMax = (Chart.PricesMin + Chart.PricesDelta) * Chart.TickSize;

            var price = this.Price;
            var height = Chart.PriceToHeight(price);
            var width = Chart.ChWidth + 2;

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
        private protected override void ChangeMethod(Vector? Changes)
        {
            if (Changes.HasValue)
            {
                this.NPrice = Chart.HeightToPrice(Chart.PriceToHeight(this.Price) + Changes.Value.Y);
            }
            else
            {
                this.NPrice = this.Price;
            }
        }
        public override Action<DrawingContext>[] PrepareToDrawing(Vector? vec, double PixelsPerDip, bool DrawOver = false)
        {
            var pricesMax = (Chart.PricesMin + Chart.PricesDelta) * Chart.TickSize;

            double price;
            if (vec.HasValue) price = this.NPrice;
            else price = this.Price;

            double height = Chart.PriceToHeight(price);
            double width = 4096;

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
        private protected override void NewCoordinates()
        {
            this.Price = this.NPrice;
        }

        public override List<(string Name, Action Act)> GetContextMenu()
        {
            return new List<(string Name, Action Act)>();
        }
    }
}
