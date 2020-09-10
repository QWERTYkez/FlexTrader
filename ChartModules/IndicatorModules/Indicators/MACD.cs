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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChartModules.IndicatorModules.Indicators
{
    public class MACD : Indicator
    {
        public MACD(IChart Chart, Grid BaseGrd, Grid ScaleGrd, DrawingCanvas CursorLinesLayer, DrawingCanvas TimeLine)
            : base(Chart, BaseGrd, ScaleGrd, CursorLinesLayer, TimeLine, true)
        {
            Sets.AddLevel("MA", new Setting[] 
            {
                new Setting(NumericType.Picker, "Fast", () => N1, x =>
                {
                    var d = (int)x;
                    if (d > N2) N2 = d;
                    else N1 = d;
                    Redraw();
                }),
                new Setting(NumericType.Picker, "Slow", () => N2, x =>
                {
                    var d = (int)x;
                    if (d < N1) N1 = d;
                    else N2 = d;
                    Redraw();
                })
            });
            Sets.Add(new Setting("MACD", () => { return MACDbr; },
                Br => { this.MACDbr = Br as SolidColorBrush; Rendering(); }));

            Sets.AddLevel("fast", new Setting[] 
            {
                new Setting("color", () => { return Pn1.Brush; }, 
                    Br => 
                    {
                        Dispatcher.Invoke(() => 
                        {
                            Pn1 = new Pen(Br as SolidColorBrush, Pn1.Thickness); 
                            Pn1.Freeze();
                        });
                        RedrawSecond(); 
                    }),
                new Setting(NumericType.Slider, "weight", () => Pn1.Thickness * 10, 
                    d => { Pn1 = new Pen(Pn1.Brush, d / 10); Pn1.Freeze(); RedrawSecond(); }, 10, 50)
            });

            Sets.AddLevel("slow", new Setting[]
            {
                new Setting("color", () => { return Pn2.Brush; },
                    Br =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Pn2 = new Pen(Br as SolidColorBrush, Pn2.Thickness);
                            Pn2.Freeze();
                        });
                        RedrawSecond();
                    }),
                new Setting(NumericType.Slider, "weight", () => Pn2.Thickness * 10,
                    d => { Pn2 = new Pen(Pn2.Brush, d / 10); Pn2.Freeze(); RedrawSecond(); }, 10, 50)
            });

            Pn1.Freeze(); Pn2.Freeze();
        }

        private protected override string SetsName => "MACD";
        private SolidColorBrush MACDbr = Brushes.Teal;
        private Pen Pn1 { get; set; } = new Pen(Brushes.Lime, 2);
        private Pen Pn2 { get; set; } = new Pen(Brushes.White, 2);

        private protected override void DestroyThis() { }

        private protected override void GetBaseMinMax(IEnumerable<ICandle> currentCandles, 
            out double min, out double max)
        {
            var TimeA = currentCandles.First().TimeStamp;
            var TimeB = currentCandles.Last().TimeStamp;

            int n = 0;
            min = 0;
            max = 0;

            if (Values.Count < 1) return;
            while (Values[n].TimeStamp != TimeA) n++;
            while (Values[n].TimeStamp < TimeB)
            {
                if (Values[n].MACD < min) min = Values[n].MACD;
                if (Values[n].MACD > max) max = Values[n].MACD;
                n++;
            }
            if (Values[n].MACD < min) min = Values[n].MACD;
            if (Values[n].MACD > max) max = Values[n].MACD;
        }
        private protected override void GetBaseMinMax(DateTime tA, DateTime tB,
            out double min, out double max)
        {
            int n = 0;
            min = 0;
            max = 0;

            if (Values.Count == 0) return;
            while (n < Values.Count && Values[n].TimeStamp < tA) n++;
            for (int i = n; i < Values.Count && Values[i].TimeStamp < tB; i++)
            {
                if (Values[i].MACD < min) min = Values[i].MACD;
                if (Values[i].MACD > max) max = Values[i].MACD;
            }
        }

        private int N1 = 12; //FastLength
        private int N2 = 26; //SliwLength
        private readonly List<Data> Values = new List<Data>();
        private void CalculateValues()
        {
            var A1 = 2 / ((double)N1 + 1);
            var A2 = 2 / ((double)N2 + 1);

            Values.Clear();

            if (N1 >= AllCandles.Count) return;

            double S = 0;
            for (int i = 0; i < N1; i++)
            {
                Values.Add(new Data(AllCandles[i].TimeStamp));
                S += AllCandles[i].CloseD;
            }
            Values.Add(new Data(AllCandles[N1].TimeStamp, S / N1));
            S += AllCandles[N1].CloseD;
            for (int i = N1 + 1; i < N2 && i < AllCandles.Count; i++)
            {
                Values.Add(new Data(AllCandles[i].TimeStamp,
                    A1 * AllCandles[i].CloseD + (1 - A1) * Values[i - 1].EMA_fast));
                S += AllCandles[i].CloseD;
            }
            if (N2 >= AllCandles.Count) return;
            Values.Add(new Data(AllCandles[N2].TimeStamp,
                    A1 * AllCandles[N2].CloseD + (1 - A1) * Values[N2 - 1].EMA_fast, S / N2));
            for (int i = N2 + 1; i < AllCandles.Count; i++)
            {
                Values.Add(new Data(AllCandles[i].TimeStamp,
                    A1 * AllCandles[i].CloseD + (1 - A1) * Values[i - 1].EMA_fast,
                    A2 * AllCandles[i].CloseD + (1 - A2) * Values[i - 1].EMA_Slow));
            }
        }
        private protected override void Calculate()
        {
            CalculateValues();

            Rects = new Rect[Values.Count];
            Parallel.For(1, Values.Count, i =>
            {
                if (Values[i].Slow)
                {
                    var x = 7.5 + (StartTime - Values[i].TimeStamp) * 15 / DeltaTime;
                    var x1 = x + 6;
                    var x2 = x - 6;

                    Rects[i] = new Rect(new Point(x1, 0), new Point(x2, Values[i].MACD));
                }
            });
        }
        private Rect[] Rects;
        private protected override void Rendering()
        {
            Dispatcher.Invoke(() =>
            {
                using var dc = IndicatorVisualBase.RenderOpen();

                foreach (var rect in Rects)
                    dc.DrawRectangle(MACDbr, null, rect);
            });
        }

        private protected override void GetSecondMinMax(DateTime tA, DateTime tB,
            out double min, out double max)
        {
            int n = 0; 
            min = Double.MaxValue;
            max = Double.MinValue;

            while (n < Values.Count && Values[n].TimeStamp < tA) n++;
            for (int i = n; i < Values.Count && Values[i].TimeStamp < tB; i++)
            {
                if (Values[i].Fast)
                {
                    if (Values[i].EMA_fast < min) min = Values[i].EMA_fast;
                    if (Values[i].EMA_fast > max) max = Values[i].EMA_fast;

                    if (Values[i].Slow)
                    {
                        if (Values[i].EMA_Slow < min) min = Values[i].EMA_Slow;
                        if (Values[i].EMA_Slow > max) max = Values[i].EMA_Slow;
                    }
                }
            }
        }
        private protected override void RedrawSecond(DateTime tA, DateTime tB)
        {
            var dT = (Chart.TimeB - Chart.TimeA) / Chart.ChWidth;

            var fastValues = (from v in Values where v.TimeStamp > tA && 
                             v.TimeStamp < tB && v.Fast select v).ToList();
            var slowValues = (from v in fastValues where v.Slow select v).ToList();

            if(fastValues.Count < 2)
            {
                Dispatcher.Invoke(() => IndicatorVisualSecond.RenderOpen().Close());
                return;
            }

            var LS = new LineSegment[fastValues.Count - 1];
            Parallel.For(1, fastValues.Count, i =>
            {
                LS[i - 1] = new LineSegment(new Point(
                    (fastValues[i].TimeStamp - Chart.TimeA) / dT,
                     ToHeight(fastValues[i].EMA_fast)), true);
                LS[i - 1].Freeze();
            });
            var geo1 = new PathGeometry(new[] { new PathFigure(
                new Point((fastValues[0].TimeStamp - Chart.TimeA) / dT,
                ToHeight(fastValues[0].EMA_fast)), LS, false) }); geo1.Freeze();

            if (slowValues.Count < 2) 
            {
                Dispatcher.Invoke(() =>
                {
                    using var dc = IndicatorVisualSecond.RenderOpen();
                    dc.DrawGeometry(null, Pn1, geo1);
                });
                return;
            }

            LS = new LineSegment[slowValues.Count - 1];
            Parallel.For(1, slowValues.Count, i =>
            {
                LS[i - 1] = new LineSegment(new Point(
                    (slowValues[i].TimeStamp - Chart.TimeA) / dT,
                    ToHeight(slowValues[i].EMA_Slow)), true);
                LS[i - 1].Freeze();
            });
            var geo2 = new PathGeometry(new[] { new PathFigure(
                new Point((slowValues[0].TimeStamp - Chart.TimeA) / dT,
                ToHeight(slowValues[0].EMA_Slow)), LS, false) }); geo2.Freeze();

            
            Dispatcher.Invoke(() =>
            {
                using var dc = IndicatorVisualSecond.RenderOpen();

                dc.DrawGeometry(null, Pn1, geo1);
                dc.DrawGeometry(null, Pn2, geo2);
            });
        }

        private struct Data
        {
            public Data(DateTime TimeStamp)
            {
                this.TimeStamp = TimeStamp;
                EMA_fast = 0;
                Fast = false;
                EMA_Slow = 0;
                Slow = false;
                MACD = 0;
            }

            public Data(DateTime TimeStamp, double fastValue)
            {
                this.TimeStamp = TimeStamp;
                EMA_fast = fastValue;
                Fast = true;
                EMA_Slow = 0;
                Slow = false;
                MACD = 0;
            }

            public Data(DateTime TimeStamp, double fastValue, double slowValue)
            {
                this.TimeStamp = TimeStamp;
                EMA_fast = fastValue;
                Fast = true;
                EMA_Slow = slowValue;
                Slow = true;
                MACD = fastValue - slowValue;
            }

            public DateTime TimeStamp { get; private set; }
            public bool Fast { get; private set; }
            public double EMA_fast { get; private set; }
            public bool Slow { get; private set; }
            public double EMA_Slow { get; private set; }
            public double MACD { get; private set; }

            public void Set_EMA_fast(double val)
            {
                EMA_fast = val; Fast = true;
                if (Slow) MACD = val - EMA_Slow;
            }
            public void Set_EMA_Slow(double val)
            {
                EMA_Slow = val; Slow = true;
                MACD = EMA_fast - val;
            }
        }
    }
}
