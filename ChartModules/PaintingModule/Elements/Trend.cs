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
using System.Windows;
using System.Windows.Media;

namespace ChartModules.PaintingModule.Elements
{
    public class Trend : ChangingElement
    {
        private static SolidColorBrush StTextBrush { get; set; } = Brushes.White;
        private static SolidColorBrush StMarkFill { get; set; } = Brushes.Black;
        private static SolidColorBrush StLineBrush { get; set; } = Brushes.White;
        private static double StLineThikness { get; set; } = 3;
        private static double StLineDash { get; set; } = 5;
        private static double StLineIndent { get; set; } = 0;

        static Trend()
        {
            StTextBrush?.Freeze(); StMarkFill?.Freeze(); StLineBrush?.Freeze();
        }

        public static List<Setting> StGetSets()
        {
            return new List<Setting>
            {
                new Setting("Line", () => StLineBrush, br => { StLineBrush = br as SolidColorBrush; }),
                new Setting("Text", () => StTextBrush, br => { StTextBrush = br as SolidColorBrush; }),
                new Setting("Mark", () => StMarkFill, br => { StMarkFill = br as SolidColorBrush; }),
                new Setting(SetType.DoubleSlider, "Thickness", () => StLineThikness, pr => { StLineThikness = (double)pr; }, 1d, 5d),
                new Setting(SetType.DoubleSlider, "Gap", () => StLineIndent, pr => { StLineIndent = (double)pr; }, 0d, 10d),
                new Setting(SetType.DoubleSlider, "Dash", () => StLineDash, pr => { StLineDash = (double)pr; }, 1d, 10d)
            };
        }

        public static void DrawFirstPoint(IChart C, DrawingVisual dv,
            DrawingVisual p, DrawingVisual t)
        {
            using var dc = dv.RenderOpen();
            dc.DrawEllipse(Brushes.Black, null, C.CurrentCursorPosition, 14, 14);
            dc.DrawEllipse(Brushes.White, null, C.CurrentCursorPosition, 12, 12);
            dc.DrawEllipse(Brushes.Black, null, C.CurrentCursorPosition, 10, 10);
        }
        public static void DrawSecondPoint(IChart C, DrawingVisual dv,
            DrawingVisual p, DrawingVisual t)
        {
            var P1 = C.PaintingPoints[0];
            var P2 = C.CurrentCursorPosition;

            P1.GetCoeffs(P2, out double A, out double B);

            var linpen = new Pen(StLineBrush, StLineThikness); linpen.Freeze();
            var linps = new List<Point>();
            if (StLineIndent == 0)
            {
                linps.Add(new Point(0, B)); 
                linps.Add(new Point(C.ChWidth, A * C.ChWidth + B));
            }
            else
            {
                double z = 0, x = 0; 
                double len = Math.Sqrt(Math.Pow(C.ChWidth, 2) + Math.Pow(B - A * C.ChWidth + B, 2));
                double dx1 = StLineDash / Math.Sqrt(A + 1);
                double dx2 = StLineIndent / Math.Sqrt(A + 1);
                
                while (z < len)
                {
                    linps.Add(new Point(x, A * x + B)); z += StLineDash; x += dx1;
                    linps.Add(new Point(x, A * x + B)); z += StLineIndent; x += dx2;
                }
            }

            using var dc = dv.RenderOpen();

            for (int i = 0; i < linps.Count; i += 2)
                dc.DrawLine(linpen, linps[i], linps[i + 1]);

            dc.DrawEllipse(Brushes.Black, null, C.PaintingPoints[0], 14, 14);
            dc.DrawEllipse(Brushes.White, null, C.PaintingPoints[0], 12, 12);
            dc.DrawEllipse(Brushes.Black, null, C.PaintingPoints[0], 10, 10);
            dc.DrawEllipse(Brushes.Black, null, C.CurrentCursorPosition, 14, 14);
            dc.DrawEllipse(Brushes.White, null, C.CurrentCursorPosition, 12, 12);
            dc.DrawEllipse(Brushes.Black, null, C.CurrentCursorPosition, 10, 10);
        }

        public Trend(ChartPoint Point1, ChartPoint Point2)
        {
            this.Point1 = Point1;
            this.Point2 = Point2;
            this.TextBrush = StTextBrush;
            this.MarkFill = StMarkFill;
            this.LineBrush = StLineBrush;
            this.LineDash = StLineDash;
            this.LineIndent = StLineIndent;
            this.LineThikness = StLineThikness;

            this.TextBrush?.Freeze(); this.MarkFill?.Freeze(); this.LineBrush?.Freeze();
        }
        public Trend(ChartPoint Point1, ChartPoint Point2, SolidColorBrush TextBrush, SolidColorBrush MarkFill,
            SolidColorBrush LineBrush = null, double LineThikness = 0, double LineDash = 0, double LineIndent = 0)
        {
            this.Point1 = Point1;
            this.Point2 = Point2;
            this.TextBrush = TextBrush;
            this.MarkFill = MarkFill;
            this.LineBrush = LineBrush;
            this.LineDash = LineDash;
            this.LineIndent = LineIndent;
            this.LineThikness = LineThikness;

            this.TextBrush?.Freeze(); this.MarkFill?.Freeze(); this.LineBrush?.Freeze();
        }

        public override string ElementName { get => "Trend"; }
        public override double GetMagnetRadius() => LineThikness / 2 + 2;
        public override bool VisibilityOnChart
        {
            get
            {
                var P1 = Point1.ToPoint();
                var P2 = Point2.ToPoint();
                P1.GetCoeffs(P2, out double A, out double B);
                double C = A * Chart.ChWidth + B;

                if ((B < 0 && C < 0) || (A > Chart.ChHeight && B > Chart.ChHeight)) return false;
                else return true;
            }
        }

        private protected override List<Setting> GetSets()
        {
            return new List<Setting>
            {
                new Setting("Line", () => this.LineBrush, br => { this.LineBrush = br as SolidColorBrush; ApplyChangesToAll(); }),
                new Setting("Text", () => this.TextBrush, br => { this.TextBrush = br as SolidColorBrush; ApplyChangesToAll(); }),
                new Setting("Mark", () => this.MarkFill, br => { this.MarkFill = br as SolidColorBrush; ApplyChangesToAll(); }),
                new Setting(SetType.DoubleSlider, "Thickness", () => this.LineThikness, pr => { this.LineThikness = (double)pr; ApplyChangesToAll(); }, 1d, 5d),
                new Setting(SetType.DoubleSlider, "Gap", () => this.LineIndent, pr => { this.LineIndent = (double)pr; ApplyChangesToAll(); }, 0d, 10d),
                new Setting(SetType.DoubleSlider, "Dash", () => this.LineDash, pr => { this.LineDash = (double)pr; ApplyChangesToAll(); }, 1d, 10d)
            };
        }

        private ChartPoint Point1 { get; set; }
        private ChartPoint Point2 { get; set; }
        private Point NP1 { get; set; }
        private Point NP2 { get; set; }
        private SolidColorBrush TextBrush { get; set; }
        private SolidColorBrush MarkFill { get; set; }
        private SolidColorBrush LineBrush { get; set; }
        private double LineDash { get; set; }
        private double LineIndent { get; set; }
        private double LineThikness { get; set; }

        private protected override double GetDistance(Point P)
        {
            var P1 = Point1.ToPoint();
            var P2 = Point2.ToPoint();
            double A = P1.GetDistance(P2);
            double B = P.GetDistance(P2);
            double C = P.GetDistance(P1);

            double pp = (A + B + C) / 2;
            return (2 * Math.Sqrt(pp * (pp - A) * (pp - B) * (pp - C))) / A;
        }
        private protected override Point GetHookPoint(Point P)
        {
            var P1 = Point1.ToPoint();
            var P2 = Point2.ToPoint();
            double A = P1.GetDistance(P2);
            double B = P.GetDistance(P2);
            double C = P.GetDistance(P1);
            double pp = (A + B + C) / 2;
            double h = (2 * Math.Sqrt(pp * (pp - A) * (pp - B) * (pp - C))) / A;

            double z = Math.Sqrt(Math.Pow(C, 2) - Math.Pow(h, 2));
            P1.GetCoeffs(P2, out double a, out double b);
            double x = z / Math.Sqrt(Math.Pow(a, 2) + 1);
            return new Point(P1.X + x, P1.Y + a*x);
        }

        private protected override void DrawShadow(DrawingVisual ElementsVisual, DrawingVisual PricesVisual, DrawingVisual TimesVisual)
        {
            var br = Dispatcher.Invoke(() => { return Chart.ChartBackground; });

            var P1 = Point1.ToPoint();
            var P2 = Point2.ToPoint();
            P1.GetCoeffs(P2, out double A, out double B);

            var linpen = new Pen(br, this.LineThikness + 1); linpen.Freeze();
            var linps = new List<Point>();
            if (LineIndent == 0)
            {
                linps.Add(new Point(0, B));
                linps.Add(new Point(Chart.ChWidth, A * Chart.ChWidth + B));
            }
            else
            {
                double z = 0, x = 0;
                double len = Math.Sqrt(Math.Pow(Chart.ChWidth, 2) + Math.Pow(B - A * Chart.ChWidth + B, 2));
                double dx1 = LineDash / Math.Sqrt(A + 1);
                double dx2 = LineIndent / Math.Sqrt(A + 1);

                while (z < len)
                {
                    linps.Add(new Point(x, A * x + B)); z += LineDash; x += dx1;
                    linps.Add(new Point(x, A * x + B)); z += LineIndent; x += dx2;
                }
            }

            Dispatcher.Invoke(() => 
            {
                using var dc = ElementsVisual.RenderOpen();

                for (int i = 0; i < linps.Count; i += 2)
                    dc.DrawLine(linpen, linps[i], linps[i + 1]);

                dc.DrawEllipse(Brushes.Black, null, P1, 10, 10);
                dc.DrawEllipse(Brushes.Black, null, P2, 10, 10);
            });
        }
        private protected override void ChangeMethod(Vector? Changes)
        {
            if (Changes.HasValue)
            {
                NP1 = Point1.ToPoint() + Changes.Value;
                NP2 = Point2.ToPoint() + Changes.Value;
            }
        }
        public override Action<DrawingContext>[] PrepareToDrawing(Vector? vec, double PixelsPerDip)
        {
            Point P1, P2;
            if (vec.HasValue)
            {
                P1 = NP1;
                P2 = NP2;
            }
            else
            {
                P1 = Point1.ToPoint();
                P2 = Point2.ToPoint();
            }
            P1.GetCoeffs(P2, out double A, out double B);

            var linpen = new Pen(this.LineBrush, this.LineThikness + 1); linpen.Freeze();
            var linps = new List<Point>();
            if (LineIndent == 0)
            {
                linps.Add(new Point(0, B));
                linps.Add(new Point(Chart.ChWidth, A * Chart.ChWidth + B));
            }
            else
            {
                double z = 0, x = 0;
                double len = Math.Sqrt(Math.Pow(Chart.ChWidth, 2) + Math.Pow(B - A * Chart.ChWidth + B, 2));
                double dx1 = LineDash / Math.Sqrt(A + 1);
                double dx2 = LineIndent / Math.Sqrt(A + 1);

                while (z < len)
                {
                    linps.Add(new Point(x, A * x + B)); z += LineDash; x += dx1;
                    linps.Add(new Point(x, A * x + B)); z += LineIndent; x += dx2;
                }
            }
            return new Action<DrawingContext>[]
                {
                    dc =>
                    {
                        for (int i = 0; i < linps.Count; i += 2)
                            dc.DrawLine(linpen, linps[i], linps[i + 1]);

                        dc.DrawEllipse(Brushes.Black, null, P1, 14, 14);
                        dc.DrawEllipse(Brushes.White, null, P1, 12, 12);
                        dc.DrawEllipse(Brushes.Black, null, P1, 10, 10);
                        dc.DrawEllipse(Brushes.Black, null, P2, 14, 14);
                        dc.DrawEllipse(Brushes.White, null, P2, 12, 12);
                        dc.DrawEllipse(Brushes.Black, null, P2, 10, 10);
                    },
                    null,
                    null
                };
        }
        private protected override void NewCoordinates()
        {
            Point1 = NP1.ToChartPoint(Chart);
            Point2 = NP2.ToChartPoint(Chart);
        }

        private protected override List<Hook> CreateSubhooks()
        {
            return new List<Hook> 
            {
                new Hook
                (
                    this,
                    P => P.GetDistance(Point1.ToPoint()),
                    P => Point1.ToPoint(),
                    () => 14,
                    V => 
                    {
                        if (V.HasValue) NP1 = Point1.ToPoint() + V.Value;
                        else NP1 = Point1.ToPoint();
                        NP2 = Point2.ToPoint();
                    },
                    DrawElement,
                    DrawShadow,
                    AcceptNewCoordinates
                ),
                new Hook
                (
                    this,
                    P => P.GetDistance(Point2.ToPoint()),
                    P => Point2.ToPoint(),
                    () => 14,
                    V => 
                    {
                        if (V.HasValue) NP2 = Point2.ToPoint() + V.Value;
                        else NP2 = Point2.ToPoint();
                        NP1 = Point1.ToPoint();
                    },
                    DrawElement,
                    DrawShadow,
                    AcceptNewCoordinates
                )
            };
        }

        public override List<(string Name, Action Act)> GetContextMenu()
        {
            return new List<(string Name, Action Act)>();
        }
    }
}
