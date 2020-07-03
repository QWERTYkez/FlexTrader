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
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChartModules.PaintingModules
{
    public class TrendsModule : ChartModule
    {
        private readonly Action ResetInstrument;
        public TrendsModule(IChart chart, PriceMarksModule PMM, Action ResetInstrument) : base(chart)
        {
            this.ResetInstrument = ResetInstrument;
        }

        public override Task Redraw() => null;
        private protected override void Destroy() { }

        private protected override void SetsDefinition()
        {
        }

        public void PaintingTrend(MouseButtonEventArgs e) => ResetInstrument.Invoke();
    }
}
