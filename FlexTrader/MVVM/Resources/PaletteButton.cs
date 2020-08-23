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

using System.Windows;
using System.Windows.Controls;

namespace FlexTrader.MVVM.Resources
{
    public class PaletteButton : Button
    {
        public static readonly DependencyProperty IsActiveProperty;
        public static readonly DependencyProperty ColorProperty;

        static PaletteButton()
        {
            IsActiveProperty = DependencyProperty.Register("IsActive", typeof(bool), typeof(PaletteButton), new PropertyMetadata { DefaultValue = false });
            ColorProperty = DependencyProperty.Register("Color", typeof(PaletteButtonColor), typeof(PaletteButton), new PropertyMetadata { DefaultValue = PaletteButtonColor.Blue });
        }

        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }
        public PaletteButtonColor Color
        {
            get { return (PaletteButtonColor)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }
    }

    public enum PaletteButtonColor 
    { 
        Blue,
        Red
    }
}
