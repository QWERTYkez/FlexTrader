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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ChartModules.CenterIndicators.Indicators
{
    public class MA : CenterIndicator
    {
        public MA()
        {
            LinePen.Freeze();

            Sets.Add(new Setting(IntType.Picker, "Period", () => Per, i => { Per = i; CalculateData(); DataChangedInvoke(); }, 2, null));
            Sets.AddLevel("Line", new Setting[]
            {
                new Setting("color", () => { return LinePen.Brush; },
                    Br =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LinePen = new Pen(Br, LinePen.Thickness);
                            LinePen.Freeze();
                        });
                        DataChangedInvoke();
                    }),
                new Setting(IntType.Slider, "weight", () => (int)(LinePen.Thickness * 10),
                    d => { LinePen = new Pen(LinePen.Brush, (double)d / 10); LinePen.Freeze(); DataChangedInvoke(); }, 10, 50)
            });
        }

        private int Per = 10;
        private Pen LinePen = new Pen(Brushes.Cyan, 3);
        private readonly List<ChartPoint> MAs = new List<ChartPoint>();
        private protected override void CalculateData()
        {
            double Sum = 0;
            for (int i = 0; i < Per - 1; i++) Sum += Chart.AllCandles[i].CloseD;
            for (int i = Per - 1; i < Chart.AllCandles.Count; i++)
            {
                Sum += Chart.AllCandles[i].CloseD;
                MAs.Add(new ChartPoint(Chart.AllCandles[i].TimeStamp, Sum / Per));
                Sum -= Chart.AllCandles[i - Per + 1].CloseD;
            }
        }

        public override Action<DrawingContext>[] PrepareToDrawing(Vector? vec, double PixelsPerDip, bool DrawOver = false)
        {
            var adcs = new Action<DrawingContext>[3];
            if (Chart.TimeA != DateTime.FromBinary(0))
            {
                var tA = Chart.TimeA - Chart.DeltaTime;
                var tB = Chart.TimeB + Chart.DeltaTime;
                dT = (Chart.TimeB - Chart.TimeA) / Chart.ChWidth;
                var Current = (from v in MAs where v.TimeStamp > tA && v.TimeStamp < tB select v).ToList();
                if (Current.Count > 2)
                {
                    var LS = new LineSegment[Current.Count - 1];
                    Parallel.For(1, Current.Count, i =>
                    {
                        LS[i - 1] = new LineSegment(GetPoint(Current[i].TimeStamp, Current[i].Price), true);
                        LS[i - 1].Freeze();
                    });
                    var geo = new PathGeometry(new[] { new PathFigure(GetPoint(Current[0].TimeStamp,
                        Current[0].Price), LS, false) }); geo.Freeze();

                    adcs[0] = dc => dc.DrawGeometry(null, LinePen, geo);
                }
            }
            return adcs;
        }

        public override string ElementName => "MA";
        public override double GetMagnetRadius() => LinePen.Thickness / 2 + 2;
        public override bool VisibilityOnChart 
        { 
            get 
            {
                int n = 0;
                while (n < MAs.Count && MAs[n].TimeStamp < Chart.TimeA) n++;
                for (int i = n; i < MAs.Count && MAs[i].TimeStamp < Chart.TimeB; i++)
                {
                    if (MAs[i].Price > 0 && MAs[i].Price < Chart.ChHeight) 
                        return true;
                }
                return false;
            } 
        }

        private protected override double GetDistance(Point P)
        {
            var r = GetMagnetRadius();
            var Ta = Chart.WidthToTime(P.X - r);
            var Tb = Chart.WidthToTime(P.X + r);
            r /= 2;

            int a = 1; while (a < MAs.Count && MAs[a].TimeStamp < Ta) a++;
            int b = a; while (b < MAs.Count && MAs[b].TimeStamp < Tb) b++; b++;
            double A, dX, d; Point Pa, Pb; int k; Vector v;
            double min = Double.MaxValue;
            for (int i = a; i < MAs.Count && i < b; i++)
            {
                Pa = MAs[i - 1].ToPoint(Chart);
                Pb = MAs[i].ToPoint(Chart);
                A= Pa.GetCoeffsA(Pb);
                dX = r / Math.Sqrt(Math.Pow(A, 2) + 1);
                v = new Vector(dX, A * dX);
                k = (int)((Pb.X - Pa.X) / dX) + 1;
                while (k > 0)
                {
                    d = Pa.DistanceTo(P);
                    if (d < min)
                    {
                        min = d;
                        MinP = Pa;
                    }
                    Pa += v; k--;
                }
            }
            return min;
        }
        private Point MinP;
        private protected override Point GetHookPoint(Point P) => MinP;

        public override List<(string Name, Action Act)> GetContextMenu()
        {
            return new List<(string Name, Action Act)>();
        }

        private struct Data
        {
            public Data(DateTime TimeStamp, double Value)
            {
                this.TimeStamp = TimeStamp;
                this.Value = Value;
            }
            public DateTime TimeStamp { get; }
            public double Value { get; }
        }
    }
}
