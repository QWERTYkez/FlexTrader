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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ChartModules.CenterIndicators
{
    public abstract class CenterIndicator : HookElement
    {
        public CenterIndicator()
        {
            this.Locked = true;

            Sets.Add(new Setting((int i) => Moving.Invoke(this, i)));
            Sets.Add(new Setting(Delete));
        }

        public readonly DrawingVisual IndicatorVisual = new DrawingVisual();

        private protected override void ChangeMethod(Vector? Changes) { }
        private protected override void NewCoordinates() { }
        private protected override void DrawShadow(DrawingVisual ElementsVisual, DrawingVisual PricesVisual, DrawingVisual TimesVisual) { }

        private protected async void ApplyDataChanges()
        {
            await Redraw();
            ApplyChangesToAll();
        }
        private protected void ApplyRenderChanges()
        {
            Rendering();
            ApplyChangesToAll();
        }

        public event Action<CenterIndicator, int> Moving;
        private protected abstract void CalculateData();
        public void Rendering() => DrawElement(null, IndicatorVisual, null, null);
        public void SetChart(IChart Chart) 
        {
            this.Chart = Chart;
            Chart.CandlesChanged += ac => Redraw();
        }
        private protected Task Redraw()
        {
            return Task.Run(() =>
            {
                if (Chart.StartTime == DateTime.FromBinary(0)) return;
                CalculateData();
                Rendering();
            });
        }

        private protected TimeSpan dT;
        private protected Point GetPoint(DateTime time, double val) => new Point(GetX(time), GetY(val));
        private protected double GetX(DateTime time) => (time - Chart.TimeA) / dT;
        private protected double GetY(double val) => Chart.ChHeight * (Chart.PricesMin + Chart.PricesDelta - val / Chart.TickSize) / Chart.PricesDelta;
    }
}
