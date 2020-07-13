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
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace FlexTrader.MVVM.Resources
{
    public class DoublePicker : TextBox
    {
        public DoublePicker(double val, double min, double max, Action<object> set)
        {
            DataContext = new DoublePickerContext(val, min, max, set);

            var b = new Binding("Value")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            b.ValidationRules.Add(new DataErrorValidationRule());
            SetBinding(TextBox.TextProperty, b);
        }
    }

    public class DoublePickerContext : IDataErrorInfo
    {
        public DoublePickerContext(double val, double min, double max, Action<object> set)
        {
            this.Value = val;
            this.min = min;
            this.max = max;
            this.set = set;
        }

        public double Value { get; set; }
        private readonly double min;
        private readonly double max;
        private readonly Action<object> set;

        public string this[string columnName]
        {
            get
            {
                string error = String.Empty;
                if ((Value < min) || (Value > max))
                    error = $"значение должно быть больше {min} и меньше {max}";
                else
                    set.Invoke(Value);
                return error;
            }
        }
        public string Error
        {
            get { throw new NotImplementedException(); }
        }
    }
}
