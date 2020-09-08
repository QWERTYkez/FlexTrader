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

namespace ChartModules.IndicatorModules
{
    public class Volumes : IndicatorBase
    {
        public Volumes(IChart Chart, Grid BaseGrd, Grid ScaleGrd, DrawingCanvas CursorLinesLayer, DrawingCanvas TimeLine)
            : base(Chart, BaseGrd, ScaleGrd, CursorLinesLayer, TimeLine)
        {
            CandleBrushUp = Chart.CandleBrushUp;
            CandleBrushDown = Chart.CandleBrushDown;

            Sets.Add(new Setting("Bullish Volume", () => { return CandleBrushUp; }, 
                Br => { this.CandleBrushUp = Br as SolidColorBrush; Redraw(); }));
            Sets.Add(new Setting("Bearish Volume", () => { return CandleBrushDown; },
                Br => { this.CandleBrushDown = Br as SolidColorBrush; Redraw(); }));
        }

        private protected override void DestroyThis() { }

        private protected override string SetsName { get => "Volume"; }
        private protected override List<Setting> Sets { get; } = new List<Setting>();

        private protected override double gmin(IEnumerable<ICandle> currentCandles) => 0;
        private protected override double gmax(IEnumerable<ICandle> currentCandles) => Convert.ToDouble(currentCandles.Max(c => c.Volume));

        private Brush CandleBrushUp;
        private Brush CandleBrushDown;
        private protected override void Redraw()
        {
            Task.Run(() => 
            {
                var DrawTeplates = new (Rect rect, Brush br)[AllCandles.Count];

                Parallel.For(0, AllCandles.Count, i =>
                {
                    var x = 7.5 + (StartTime - AllCandles[i].TimeStamp) * 15 / DeltaTime;
                    var x1 = x + 6;
                    var x2 = x - 6;
                    
                    if (AllCandles[i].UP) 
                        DrawTeplates[i] = (new Rect(new Point(x1, 0), new Point(x2, AllCandles[i].VolumeD)), CandleBrushUp);
                    else 
                        DrawTeplates[i] = (new Rect(new Point(x1, 0), new Point(x2, AllCandles[i].VolumeD)), CandleBrushDown);
                });

                Dispatcher.Invoke(() =>
                {
                    using var dc = BaseIndicatorVisual.RenderOpen();
                    foreach (var (rect, br) in DrawTeplates)
                        dc.DrawRectangle(br, null, rect);
                });

                VerticalReset();
            });
        }
    }
}
