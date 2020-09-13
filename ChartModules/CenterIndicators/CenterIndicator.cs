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

namespace ChartModules.CenterIndicators
{
    public abstract class CenterIndicator : HookElement
    {
        private protected override void ChangeMethod(Vector? Changes) { }
        private protected override void NewCoordinates() { }

        public event Action<CenterIndicator, int> Moving;
        private protected override List<Setting> GetSets()
        {
            var sets = GetIndSets();
            sets.Add(new Setting((int i) => Moving.Invoke(this, i)));
            return sets;
        }
        private protected abstract List<Setting> GetIndSets();
    }
}
