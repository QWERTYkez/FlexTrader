﻿/* 
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

namespace FlexTrader.MVVM.Resources
{
    public partial class DoublePicker : UserControl
    {
        public DoublePicker(double val, Action<object> act, double? min = null, double? max = null)
        {
            InitializeComponent();
            AcceptButton.Click += (s, e) => act.Invoke(((Button)s).Tag);
            AcceptButton.Tag = val;

            var dptb = new DoublePickerTextBox(val, min, max);

            this.contentPresenter.Content = dptb;

            dptb.Changed += d => 
            {
                if (d != null)
                {
                    AcceptButton.Tag = d.Value;
                    AcceptButton.IsEnabled = true;
                }
                else
                {
                    AcceptButton.IsEnabled = false;
                }
            };
        }
    }

    public class DoublePickerTextBox : TextBox
    {
        public event Action<double?> Changed;

        public DoublePickerTextBox(double val, double? min = null, double? max = null)
        {
            var dc = new DoublePickerContext(val, min, max);
            DataContext = dc;
            dc.Changed += d => this.Changed?.Invoke(d);

            var b = new Binding("Value")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                NotifyOnValidationError = true
            };
            b.ValidationRules.Add(new DataErrorValidationRule());
            SetBinding(TextBox.TextProperty, b);

            FontSize = 14;
            FontWeight = FontWeights.Bold;
            FontFamily = new System.Windows.Media.FontFamily("consolas");
        }
    }

    public class DoublePickerContext : IDataErrorInfo
    {
        public DoublePickerContext(double val, double? min = null, double? max = null)
        {
            this.Value = val.ToString();

            if (min != null)
                this.min = min.Value;
            else
                this.min = 0;

            if (max != null)
                this.max = max.Value;
            else
                this.max = Double.MaxValue;
        }

        public string Value { get; set; }
        private readonly double min;
        private readonly double max;
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
}