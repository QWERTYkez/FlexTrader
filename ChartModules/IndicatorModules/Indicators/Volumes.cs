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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChartModules.IndicatorModules
{
    public class Volumes : Indicator
    {
        public Volumes(IChart Chart, Grid BaseGrd, Grid ScaleGrd, DrawingCanvas CursorLinesLayer, DrawingCanvas TimeLine)
            : base(Chart, BaseGrd, ScaleGrd, CursorLinesLayer, TimeLine)
        {
            CandleBrushUp = Chart.CandleBrushUp;
            CandleBrushDown = Chart.CandleBrushDown;

            Sets.Add(new Setting("Bullish Volume", () => { return CandleBrushUp; }, 
                Br => { this.CandleBrushUp = Br; Rendering(); }));
            Sets.Add(new Setting("Bearish Volume", () => { return CandleBrushDown; },
                Br => { this.CandleBrushDown = Br; Rendering(); }));
        }

        private protected override void DestroyThis() { }

        private protected override string SetsName => "Volume";

        private protected override void GetBaseMinMax(IEnumerable<ICandle> currentCandles, out double min, out double max)
        { min = 0; max = Convert.ToDouble(currentCandles.Max(c => c.Volume)); }
        private protected override void GetBaseMinMax(DateTime tA, DateTime tB, out double min, out double max)
        { 
            decimal m = 0;
            int n = 0;
            min = 0;
            max = 1;

            if (AllCandles.Count == 0) return;
            while (n < AllCandles.Count && AllCandles[n].TimeStamp < tA) 
                n++;
            for (int i = n; i < AllCandles.Count && AllCandles[i].TimeStamp < tB; i++)
                if (AllCandles[i].Volume > m) 
                    m = AllCandles[i].Volume;
            max = Convert.ToDouble(m);
        }

        private Brush CandleBrushUp;
        private Brush CandleBrushDown;
        private readonly List<Rect> VolumesUp = new List<Rect>();
        private readonly List<Rect> VolumesDown = new List<Rect>();
        private protected override void Calculate()
        {
            VolumesUp.Clear(); VolumesDown.Clear();
            for (int i = 0; i < AllCandles.Count; i++)
            {
                var x = 7.5 + (StartTime - AllCandles[i].TimeStamp) * 15 / DeltaTime;
                var x1 = x + 6;
                var x2 = x - 6;

                if (AllCandles[i].UP) VolumesUp.Add(new Rect(new Point(x1, 0), new Point(x2, AllCandles[i].VolumeD)));
                else VolumesDown.Add(new Rect(new Point(x1, 0), new Point(x2, AllCandles[i].VolumeD)));
            }
        }
        private protected override void Rendering()
        {
            Dispatcher.Invoke(() =>
            {
                using var dc = IndicatorVisualBase.RenderOpen();

                foreach (var rect in VolumesUp)
                    dc.DrawRectangle(CandleBrushUp, null, rect);
                foreach (var rect in VolumesDown)
                    dc.DrawRectangle(CandleBrushDown, null, rect);
            });
        }
    }
}
