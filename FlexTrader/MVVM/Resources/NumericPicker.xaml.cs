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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace FlexTrader.MVVM.Resources
{
    public partial class NumericPicker : UserControl
    {
        public NumericPicker(double val, Action<double> act, double? min = null, double? max = null,
            Action<Action<double>> GetSetMin = null, Action<Action<double>> GetSetMax = null)
        {
            InitializeComponent();
            AcceptButton.Click += (s, e) => act.Invoke((double)((Button)s).Tag);
            AcceptButton.Tag = val;

            var dptb = new NumericPickerTextBox(val, min, max, GetSetMin, GetSetMax);
            dptb.Enter += () => { if (AcceptButton.IsEnabled) act.Invoke((double)AcceptButton.Tag); };

            this.contentPresenter.Content = dptb;

            dptb.Changed += d => 
            {
                if (d != null)
                {
                    AcceptButton.Tag = (double)d;
                    AcceptButton.IsEnabled = true;
                }
                else
                {
                    AcceptButton.IsEnabled = false;
                }
            };
        }

        public NumericPicker(int val, Action<int> act, int? min = null, int? max = null,
            Action<Action<int>> GetSetMin = null, Action<Action<int>> GetSetMax = null)
        {
            InitializeComponent();
            AcceptButton.Click += (s, e) => act.Invoke((int)((Button)s).Tag);
            AcceptButton.Tag = val;

            var dptb = new NumericPickerTextBox(val, min, max, GetSetMin, GetSetMax);
            dptb.Enter += () => { if (AcceptButton.IsEnabled) act.Invoke((int)AcceptButton.Tag); };

            this.contentPresenter.Content = dptb;

            dptb.Changed += d =>
            {
                if (d != null)
                {
                    AcceptButton.Tag = (int)d;
                    AcceptButton.IsEnabled = true;
                }
                else
                {
                    AcceptButton.IsEnabled = false;
                }
            };
        }
    }

    public class NumericPickerTextBox : TextBox
    {
        public event Action Enter;
        public event Action<object> Changed;

        public NumericPickerTextBox(double val, double? min = null, double? max = null,
            Action<Action<double>> GetSetMin = null, Action<Action<double>> GetSetMax = null)
        {
            var dc = new DoublePickerContext(val, min, max, GetSetMin, GetSetMax);
            DataContext = dc;
            dc.Changed += d => this.Changed?.Invoke(d);

            this.KeyDown += EKeyDown;

            var b = new Binding("Value")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                NotifyOnValidationError = true
            };
            b.ValidationRules.Add(new DataErrorValidationRule());
            SetBinding(TextBox.TextProperty, b);

            FontSize = 18;
            FontWeight = FontWeights.Bold;
            FontFamily = new FontFamily("consolas");
        }

        private void EKeyDown(object s, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                this.KeyDown -= EKeyDown;
                this.KeyUp += EKeyUP;
                Enter.Invoke();
            }
        }
        private void EKeyUP(object s, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                this.KeyUp -= EKeyUP;
                this.KeyDown += EKeyDown;
            }
        }

        public NumericPickerTextBox(int val, int? min = null, int? max = null,
            Action<Action<int>> GetSetMin = null, Action<Action<int>> GetSetMax = null)
        {
            var dc = new IntPickerContext(val, min, max, GetSetMin, GetSetMax);
            DataContext = dc;
            dc.Changed += d => this.Changed?.Invoke(d);

            this.KeyDown += EKeyDown;

            var b = new Binding("Value")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                NotifyOnValidationError = true
            };
            b.ValidationRules.Add(new DataErrorValidationRule());
            SetBinding(TextBox.TextProperty, b);

            FontSize = 18;
            FontWeight = FontWeights.Bold;
            FontFamily = new FontFamily("consolas");
        }
    }

    public class DoublePickerContext : IDataErrorInfo
    {
        public DoublePickerContext(double val, double? min = null, double? max = null,
            Action<Action<double>> GetSetMin = null, Action<Action<double>> GetSetMax = null)
        {
            this.Value = val.ToString();

            GetSetMin?.Invoke(this.GetSetMin);
            GetSetMax?.Invoke(this.GetSetMax);

            if (min != null)
                this.min = min.Value;
            else
                this.min = 0;

            if (max != null)
                this.max = max.Value;
            else
                this.max = Double.MaxValue;
        }

        private void GetSetMin(double m)
        {
            try
            {
                var val = Convert.ToDouble(Value);
                if (val > m)
                {
                    Value = m.ToString();
                    min = m;
                }
                else min = m;
            }
            catch { min = m; }
        }

        private void GetSetMax(double m)
        {
            try
            {
                var val = Convert.ToDouble(Value);
                if (val < m)
                {
                    Value = m.ToString();
                    max = m;
                }
                else max = m;
            }
            catch { max = m; }
        }

        public string Value { get; set; }
        private double min;
        private double max;
        public event Action<double?> Changed;

        public string this[string columnName]
        {
            get
            {
                string error = String.Empty;
                double? val = null;
                try
                {
                    val = Convert.ToDouble(Value);
                    if ((val < min) || (val > max))
                    {
                        Changed.Invoke(null);
                        error = $"значение должно быть больше {min} и меньше {max}";
                    }
                }
                catch { }
                Changed.Invoke(val);
                return error;
            }
        }
        public string Error
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class IntPickerContext : IDataErrorInfo
    {
        public IntPickerContext(int val, int? min = null, int? max = null,
            Action<Action<int>> GetSetMin = null, Action<Action<int>> GetSetMax = null)
        {
            this.Value = val.ToString();

            GetSetMin?.Invoke(this.GetSetMin);
            GetSetMax?.Invoke(this.GetSetMax);

            if (min != null)
                this.min = min.Value;
            else
                this.min = 0;

            if (max != null)
                this.max = max.Value;
            else
                this.max = int.MaxValue;
        }

        private void GetSetMin(int m)
        {
            try
            {
                var val = Convert.ToDouble(Value);
                if (val > m)
                {
                    Value = m.ToString();
                    min = m;
                }
                else min = m;
            }
            catch { min = m; }
        }

        private void GetSetMax(int m)
        {
            try
            {
                var val = Convert.ToDouble(Value);
                if (val < m)
                {
                    Value = m.ToString();
                    max = m;
                }
                else max = m;
            }
            catch { max = m; }
        }

        public string Value { get; set; }
        private int min;
        private int max;
        public event Action<int?> Changed;

        public string this[string columnName]
        {
            get
            {
                string error = String.Empty;
                int? val = null;
                try
                {
                    val = Convert.ToInt32(Value);
                    if ((val < min) || (val > max))
                    {
                        Changed.Invoke(null);
                        error = $"значение должно быть больше {min} и меньше {max}";
                    }
                }
                catch { }
                Changed.Invoke(val);
                return error;
            }
        }
        public string Error
        {
            get { throw new NotImplementedException(); }
        }
    }
}
