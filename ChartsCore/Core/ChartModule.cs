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

using System.Collections.Generic;
using System.Windows.Threading;

namespace ChartsCore.Core
{
    public abstract class ChartModule
    {
        private protected View Chart;
        private protected Dispatcher Dispatcher;
        public ChartModule(View chart)
        {
            Chart = chart;
            Dispatcher = Chart.Dispatcher;
        }
        public void Restruct()
        {
            Chart = null;
            Destroy();
        }
        private protected abstract void Destroy();
        public (string SetsName, List<Setting> Sets) GetSets() => (SetsName, Sets);
        private protected List<Setting> Sets { get; } = new List<Setting>();
        private protected abstract string SetsName { get; }
    }
}
