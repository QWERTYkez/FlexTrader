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
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace FlexUserConrols.Converters
{
    public class BrushToEffectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double d;
            try { d = System.Convert.ToDouble(parameter); } catch { d = 0; }
            return new DropShadowEffect { Color = (value as SolidColorBrush).Color, ShadowDepth = 0, BlurRadius = d };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            new SolidColorBrush((value as DropShadowEffect).Color);
    }
}
