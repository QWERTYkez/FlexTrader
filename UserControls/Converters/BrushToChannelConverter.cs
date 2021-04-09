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
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace UserControls.Converters
{
    public class BrushToChannelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null) throw new Exception();
            var (ch, br) = ((CChannels ch, Func<SolidColorBrush> br))parameter;
            var c = (value as SolidColorBrush).Color;
            return ch switch
            {
                CChannels.Red => c.R,
                CChannels.Green => c.G,
                CChannels.Blue => c.B,
                CChannels.Alpha => c.A,
                _ => throw new Exception(),
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null) throw new Exception();
            var (ch, br) = ((CChannels ch, Func<SolidColorBrush> br))parameter;
            var c = (br.Invoke() as SolidColorBrush).Color;
            try
            {
                var val = System.Convert.ToByte(value);
                switch (ch)
                {
                    case CChannels.Red:
                        c.R = val;
                        return new SolidColorBrush(c);
                    case CChannels.Green:
                        c.G = val;
                        return new SolidColorBrush(c);
                    case CChannels.Blue:
                        c.B = val;
                        return new SolidColorBrush(c);
                    case CChannels.Alpha:
                        c.A = val;
                        return new SolidColorBrush(c);
                    default: throw new Exception();
                }
            }
            catch
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }
    public enum CChannels
    {
        Red,
        Green,
        Blue,
        Alpha
    }
}
