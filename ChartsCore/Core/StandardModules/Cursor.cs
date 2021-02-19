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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ChartsCore.Core.StandardModules
{
    public class CursorModule : ChartModule
    {
        private static readonly List<CursorModule> Cursors = new List<CursorModule>();
        static CursorModule()
        {
            LinesPen.Freeze();
        }

        private readonly DrawingCanvas CursorLayer;
        private readonly DrawingCanvas MagnetLayer;
        private readonly DrawingCanvas TimeLine;
        private readonly DrawingCanvas PriceLine;
        public CursorModule(View chart, DrawingCanvas CursorLayer,
            DrawingCanvas MagnetLayer, DrawingCanvas TimeLine,
            DrawingCanvas PriceLine) : base(chart)
        {
            Cursors.Add(this);

            this.CursorLayer = CursorLayer;
            this.MagnetLayer = MagnetLayer;
            this.TimeLine = TimeLine;
            this.PriceLine = PriceLine;

            Chart.ChartGrid.MouseEnter += ShowCursor;
            Chart.ChartGrid.MouseLeave += CursorLeave;
            Chart.ChartGrid.MouseMove += Redraw;
            Chart.MWindow.SetCursor += SetCursor;
            Chart.MWindow.ToggleMagnet += b =>
            {
                Task.Run(() =>
                {
                    if (b) MagnetAdd();
                    else MagnetRemove();
                });
            };
            SetCursorLines();
            Chart.CursorLinesLayer.RenderTransform = CursorLinesTransform;
            CursorLayer.RenderTransform = CursorTransform;
            MagnetLayer.RenderTransform = MagnetTransform;

            var SetCursorArea = new Action<int>(b => { CursorArea = b; SetCursorLines(); });
            var SetMagnetRadius = new Action<int>(b => { MagnetRadius = b; if (MagnetState) MagnetAdd(); });
            var SetCursorDash = new Action<int>(b => { CursorDash = b; SetCursorLines(); });
            var SetCursorIndent = new Action<int>(b => { CursorIndent = b; SetCursorLines(); });
            var SetCursorThikness = new Action<int>(b => 
            { Dispatcher.Invoke(() => { LinesPen = new Pen(LinesPen.Brush, b); LinesPen.Freeze(); }); SetCursorLines(); });
            var SetCursorColor = new Action<SolidColorBrush>(b => 
            { Dispatcher.Invoke(() => { LinesPen = new Pen(b, LinesPen.Thickness); LinesPen.Freeze(); }); SetCursorLines(); });

            Sets.Add(new Setting(IntType.Slider, "Радиус отступа", () => (int)CursorArea, SetCursorArea, 20, 50, null, null, 25));
            Sets.Add(new Setting(IntType.Slider, "Радиус магнита", () => (int)MagnetRadius, SetMagnetRadius, 20, 50, null, null, 25));
            Sets.Add(new Setting(IntType.Slider, "Штрих", () => (int)CursorDash, SetCursorDash, 1, 10, null, null, 5));
            Sets.Add(new Setting(IntType.Slider, "Отступ", () => (int)CursorIndent, SetCursorIndent, 0, 10, null, null, 2));
            Sets.Add(new Setting(IntType.Slider, "Толщина", () => (int)LinesPen.Thickness, SetCursorThikness, 1, 5, null, null, 2));
            Sets.Add(new Setting("Цвет курсора", () => LinesPen.Brush, SetCursorColor, Brushes.White));
            Sets.Add(new Setting("Цвет текста", () => FontBrush, b => { FontBrush = b; }, Brushes.White));
        }

        private protected override string SetsName => "Настройки курсора";

        public DrawingVisual CursorLinesVisual { get; private set; } = new DrawingVisual();
        public DrawingVisual CursorVisual { get; private set; } = new DrawingVisual();
        private readonly DrawingVisual MagnetVisual = new DrawingVisual();
        private readonly TranslateTransform CursorLinesTransform = new TranslateTransform();
        private readonly TranslateTransform CursorTransform = new TranslateTransform();
        private readonly TranslateTransform MagnetTransform = new TranslateTransform();
        private readonly DrawingVisual CursorTimeVisual = new DrawingVisual();
        private readonly DrawingVisual CursorPriceVisual = new DrawingVisual();
        private void ShowCursor(object sender, MouseEventArgs e)
        {
            Chart.CursorLinesLayer.AddVisual(CursorLinesVisual);
            CursorLayer.AddVisual(CursorVisual);
            MagnetLayer.AddVisual(MagnetVisual);
            TimeLine.AddVisual(CursorTimeVisual);
            PriceLine.AddVisual(CursorPriceVisual);
        }
        private void CursorLeave(object sender, MouseEventArgs e)
        {
            Chart.CursorLinesLayer.RemoveVisual(CursorLinesVisual);
            CursorLayer.RemoveVisual(CursorVisual);
            MagnetLayer.RemoveVisual(MagnetVisual);
            TimeLine.RemoveVisual(CursorTimeVisual);
            PriceLine.RemoveVisual(CursorPriceVisual);
        }
        private protected override void Destroy()
        {
            Cursors.Remove(this);

            Chart.ChartGrid.MouseEnter -= ShowCursor;
            Chart.ChartGrid.MouseLeave -= CursorLeave;
            Chart.ChartGrid.MouseMove -= Redraw;
        }

        public CursorPosition CursorPosition { get; } = new CursorPosition();
        public bool Hide { get; private set; } = false;
        private bool Correcting { get; set; }

        public void Redraw(object s, MouseEventArgs e) => Redraw(e.GetPosition(Chart.ChartGrid));
        public void Redraw(Point P)
        {
            Task.Run(() =>
            {
                var npos = CursorPosition.Current = P; DateTime dt = DateTime.Now; string price = "";

                if (Correcting)
                {
                    dt = Chart.CorrectTimePosition(ref npos);
                    price = Chart.HeightToPrice(npos.Y).ToString(Chart.TickPriceFormat);
                    npos.Y = Chart.PriceToHeight(Convert.ToDouble(price));
                    price = Chart.HeightToPrice(npos.Y).ToString(Chart.TickPriceFormat);
                    CursorPosition.Corrected = npos;
                }
                else CursorPosition.Corrected = CursorPosition.Current;

                if (MagnetState)
                {
                    if (CurrentCursor == CursorT.Hook && !Chart.Manipulating) 
                        CursorPosition.NMP();
                    else
                    {
                        if (Chart.MagnetPoints.Count > 0)
                        {
                            var mp = from c in Chart.MagnetPoints
                                     let r = Math.Pow(c.X - CursorPosition.Current.X, 2) + Math.Pow(c.Y - CursorPosition.Current.Y, 2)
                                     let R = Math.Pow(MagnetRadius, 2)
                                     where r < R
                                     orderby r
                                     select c;
                            if (mp.Count() > 0)
                            {
                                var p = mp.First();
                                CursorPosition.Magnet = p.ToPoint();
                                var cpm = CursorPosition.Magnet;
                                dt = Chart.CorrectTimePosition(ref cpm);
                                price = p.Price.ToString(Chart.TickPriceFormat);

                            }
                            else CursorPosition.NMP();
                        } 
                        else CursorPosition.NMP();
                    }
                } else CursorPosition.NMP();

                ////////
                Chart.MWindow.MMInstrument?.Invoke();

                if (Hide)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MagnetTransform.X = CursorPosition.Current.X;
                        MagnetTransform.Y = CursorPosition.Current.Y;
                        CursorTransform.X = CursorPosition.Magnet.X;
                        CursorTransform.Y = CursorPosition.Magnet.Y;
                    });
                    return;
                }

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
                            FontBrush,
                            VisualTreeHelper.GetDpi(CursorPriceVisual).PixelsPerDip
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
                    using var dcP = CursorPriceVisual.RenderOpen();
                    using var dcT = CursorTimeVisual.RenderOpen();

                    MagnetTransform.X = CursorPosition.Current.X;
                    MagnetTransform.Y = CursorPosition.Current.Y;
                    CursorLinesTransform.X = CursorPosition.Magnet.X;
                    CursorLinesTransform.Y = CursorPosition.Magnet.Y;
                    CursorTransform.X = CursorPosition.Current.X;
                    CursorTransform.Y = CursorPosition.Current.Y;

                    dcP.DrawGeometry(Chart.ChartBackground, LinesPen, geo);
                    dcP.DrawText(ft, Tpont);

                    dcT.DrawGeometry(Chart.ChartBackground, LinesPen, geo2);
                    dcT.DrawText(ft2, Tpont2);
                });
            });
        }

        public static Brush FontBrush { get; private set; } = Brushes.White;
        public static Pen LinesPen { get; private set; } = new Pen(Brushes.White, 2);
        private static double CursorArea = 25;
        private static double CursorDash = 5;
        private static double CursorIndent = 2;
        public static Task SetCursorLines()
        {
            var tasks = new Task[Cursors.Count];
            for (int i = 0; i < Cursors.Count; i++)
                tasks[i] = Cursors[i].SetCLines();
            return Task.WhenAll(tasks);
        }
        private Task SetCLines()
        {
            return Task.Run(() =>
            {
                var rt = new RotateTransform(90); rt.Freeze();

                var Points = new List<Point>();

                if (CursorIndent == 0)
                {
                    Points.Add(new Point(CursorArea, 0));
                    Points.Add(new Point(4096, 0));
                }
                else
                {
                    double s = CursorArea;
                    while (s < 4096)
                    {
                        Points.Add(new Point(s, 0)); s += CursorDash;
                        Points.Add(new Point(s, 0)); s += CursorIndent;
                    }
                }
                Dispatcher.Invoke(() =>
                {
                    using var dcCH = CursorLinesVisual.RenderOpen();

                    for (int i = 0; i < Points.Count; i += 2)
                        dcCH.DrawLine(LinesPen, Points[i], Points[i + 1]);
                    dcCH.PushTransform(rt);
                    for (int i = 0; i < Points.Count; i += 2)
                        dcCH.DrawLine(LinesPen, Points[i], Points[i + 1]);
                    dcCH.PushTransform(rt);
                    for (int i = 0; i < Points.Count; i += 2)
                        dcCH.DrawLine(LinesPen, Points[i], Points[i + 1]);
                    dcCH.PushTransform(rt);
                    for (int i = 0; i < Points.Count; i += 2)
                        dcCH.DrawLine(LinesPen, Points[i], Points[i + 1]);
                });
                SetCursor(CurrentCursor);
            });
        }

        private CursorT CurrentCursor = CursorT.None;
        private void SetCursor(CursorT t)
        {
            Task.Run(() => 
            {
                if (CurrentCursor == t) return;
                CurrentCursor = t;
                switch (t)
                {
                    case CursorT.Paint:
                        {
                            Correcting = true;
                            Dispatcher.Invoke(() =>
                            {
                                using var dc = CursorVisual.RenderOpen();
                            });
                        }
                        break;
                    case CursorT.Standart:
                        {
                            Correcting = true;
                            Dispatcher.Invoke(() =>
                            {
                                using var dc = CursorVisual.RenderOpen();
                                dc.DrawLine(LinesPen, new Point(-15, 0), new Point(15, 0));
                                dc.DrawLine(LinesPen, new Point(0, -15), new Point(0, 15));
                            });
                        }
                        break;
                    case CursorT.Select:
                        {
                            Correcting = false;
                            var geo = new PathGeometry(new[] { new PathFigure(new Point(0, 0),
                            new[]
                            {
                                new LineSegment(new Point(0, 75), true),
                                new LineSegment(new Point(75, 0), true)
                            },
                            true)}); geo.Freeze();

                            var clip = new GeometryGroup();
                            #region Clip Geometry
                            clip.Children.Add(new CombinedGeometry 
                            {
                                GeometryCombineMode = GeometryCombineMode.Exclude,
                                Geometry1 = new EllipseGeometry() 
                                {
                                    Center = new Point(150, 235),
                                    RadiusX = 65, RadiusY = 65
                                },
                                Geometry2 = new RectangleGeometry(new Rect(
                                    new Point(60, 100), new Size(180, 135)))
                            });
                            clip.Children.Add(new CombinedGeometry
                            {
                                GeometryCombineMode = GeometryCombineMode.Exclude,
                                Geometry1 = new EllipseGeometry()
                                {
                                    Center = new Point(150, 235),
                                    RadiusX = 50,
                                    RadiusY = 50
                                },
                                Geometry2 = new RectangleGeometry(new Rect(
                                    new Point(60, 100), new Size(180, 135)))
                            });

                            clip.Children.Add(new CombinedGeometry
                            {
                                GeometryCombineMode = GeometryCombineMode.Exclude,
                                Geometry1 = new EllipseGeometry()
                                {
                                    Center = new Point(135, 50),
                                    RadiusX = 50,
                                    RadiusY = 50
                                },
                                Geometry2 = new RectangleGeometry(new Rect(
                                    new Point(85, 50), new Size(180, 150)))
                            });
                            clip.Children.Add(new CombinedGeometry
                            {
                                GeometryCombineMode = GeometryCombineMode.Exclude,
                                Geometry1 = new EllipseGeometry()
                                {
                                    Center = new Point(135, 50),
                                    RadiusX = 35,
                                    RadiusY = 35
                                },
                                Geometry2 = new RectangleGeometry(new Rect(
                                    new Point(85, 50), new Size(180, 150)))
                            });

                            clip.Children.Add(new CombinedGeometry
                            {
                                GeometryCombineMode = GeometryCombineMode.Exclude,
                                Geometry1 = new EllipseGeometry()
                                {
                                    Center = new Point(150, 200),
                                    RadiusX = 35,
                                    RadiusY = 35
                                },
                                Geometry2 = new RectangleGeometry(new Rect(
                                    new Point(85, 100), new Size(100, 100)))
                            });
                            clip.Children.Add(new CombinedGeometry
                            {
                                GeometryCombineMode = GeometryCombineMode.Exclude,
                                Geometry1 = new EllipseGeometry()
                                {
                                    Center = new Point(150, 200),
                                    RadiusX = 20,
                                    RadiusY = 20
                                },
                                Geometry2 = new RectangleGeometry(new Rect(
                                    new Point(85, 100), new Size(100, 100)))
                            });

                            clip.Children.Add(new CombinedGeometry
                            {
                                GeometryCombineMode = GeometryCombineMode.Union,
                                Geometry1 = new EllipseGeometry()
                                {
                                    Center = new Point(122.5, 80),
                                    RadiusX = 7.5,
                                    RadiusY = 7.5
                                },
                                Geometry2 = new RectangleGeometry(new Rect(
                                    new Point(115, 80), new Size(15, 120)))
                            });
                            clip.Children.Add(new CombinedGeometry
                            {
                                GeometryCombineMode = GeometryCombineMode.Union,
                                Geometry1 = new EllipseGeometry()
                                {
                                    Center = new Point(207.5, 80),
                                    RadiusX = 7.5,
                                    RadiusY = 7.5
                                },
                                Geometry2 = new RectangleGeometry(new Rect(
                                    new Point(200, 80), new Size(15, 155)))
                            });

                            clip.Children.Add(new RectangleGeometry(new Rect(new Point(85, 50), new Size(15, 185))));
                            clip.Children.Add(new RectangleGeometry(new Rect(new Point(170, 50), new Size(15, 150))));
                            #endregion
                            clip.Freeze();

                            Dispatcher.Invoke(() =>
                            {
                                using var dc = CursorVisual.RenderOpen();
                                //dc.PushTransform(new TranslateTransform(-4, 3));
                                //dc.PushTransform(new RotateTransform(-30));
                                //dc.PushTransform(new ScaleTransform(1.5, 1.5));

                                //dc.DrawGeometry(LinesPen.Brush, null, geo);

                                //dc.Pop();
                                //dc.Pop();
                                //dc.Pop();

                                //dc.PushTransform(new TranslateTransform(0, 0));
                                dc.PushTransform(new ScaleTransform(0.10, 0.10));

                                dc.DrawGeometry(LinesPen.Brush, null, geo);
                                dc.DrawGeometry(LinesPen.Brush, null, clip);
                            });
                        }
                        break;
                    case CursorT.Hook:
                        {
                            Correcting = false;
                            var geo = new PathGeometry(new[] { new PathFigure(new Point(4, 0),
                            new[]
                            {
                                new LineSegment(new Point(8, 12), true),
                                new LineSegment(new Point(6, 13), true),
                                new LineSegment(new Point(5, 10), true),
                                new LineSegment(new Point(5, 18), true),
                                new LineSegment(new Point(3, 18), true),
                                new LineSegment(new Point(3, 10), true),
                                new LineSegment(new Point(2, 13), true),
                                new LineSegment(new Point(0, 12), true)
                            },
                            true)}); geo.Freeze();

                            Dispatcher.Invoke(() =>
                            {
                                using var dc = CursorVisual.RenderOpen();
                                dc.PushTransform(new TranslateTransform(-4, 3));
                                dc.PushTransform(new RotateTransform(-30));
                                dc.PushTransform(new ScaleTransform(1.5, 1.5));

                                dc.DrawGeometry(LinesPen.Brush, null, geo);
                            });
                        }
                        break;
                    case CursorT.None:
                        {
                            Correcting = true;
                            Dispatcher.Invoke(() =>
                            {
                                using var dc = CursorVisual.RenderOpen();
                            });
                        }
                        break;
                }
                if (MagnetState) MagnetAdd();

                if (t == CursorT.Hook || t == CursorT.Select)
                {
                    Hide = true;
                    Dispatcher.Invoke(() => 
                    {
                        CursorPriceVisual.RenderOpen().Close();
                        CursorTimeVisual.RenderOpen().Close();
                        CursorLinesVisual.RenderOpen().Close();
                    });
                }
                    
                else { Hide = false; SetCursorLines(); }
            });
        }

        private static double MagnetRadius = 25;
        private bool MagnetState = false;
        private void MagnetAdd()
        {
            MagnetState = true;
            Dispatcher.InvokeAsync(() =>
            {
                using var dc = MagnetVisual.RenderOpen();
                dc.DrawEllipse(null, new Pen(LinesPen.Brush, 2), new Point(0, 0), MagnetRadius, MagnetRadius);
            });
        }
        private void MagnetRemove()
        {
            MagnetState = false;
            Dispatcher.InvokeAsync(() => MagnetVisual.RenderOpen().Close());
        }
    }

    public class CursorPosition
    {
        public Point Current { get; set; }
        public Point Corrected { get; set; }
        public Point Magnet_Current { get; set; }
        private Point magnet;
        public Point Magnet 
        {
            get => magnet;
            set 
            {
                magnet = value;
                Magnet_Current = value;
            } 
        }

        public void NMP()
        {
            Magnet = Corrected;
            Magnet_Current = Current;
        }
    }

    public enum CursorT
    {
        Paint,
        Standart,
        Select,
        Hook,
        None
    }
}
