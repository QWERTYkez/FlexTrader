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
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FlexTrader.MVVM.Resources
{
    public partial class IntSlider : UserControl
    {
        public event Action<int> ValueChanged;
        public IntSlider(int val, Action<int> set, int min, int max, 
            Action<Action<int>> GetSetMin = null, Action<Action<int>> GetSetMax = null)
        {
            InitializeComponent();

            if (set != null) ValueChanged += v => set.Invoke(v);

            GetSetMin?.Invoke(this.GetSetMin);
            GetSetMax?.Invoke(this.GetSetMax);

            slider.Minimum = min;
            slider.Maximum = max;
            slider.Value = val;

            slider.ValueChanged += (s, e) => Task.Run(() => ValueChanged.Invoke((int)e.NewValue));
        }

        private void GetSetMin(int min)
        {
            if (slider.Value < min)
            {
                slider.Value = min;
                slider.Minimum = min;
            }
            else
            {
                slider.Minimum = min;
            }
        }

        private void GetSetMax(int max)
        {
            if (slider.Value > max)
            {
                slider.Value = max;
                slider.Maximum = max;
            }
            else
            {
                slider.Maximum = max;
            }
        }
    }
}
