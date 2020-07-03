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
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace FlexTrader.MVVM.Resources
{
    public class CrossHair : UserControl { }
    public class InteractArrow : UserControl { }
    public class Gear : UserControl { }
    public class Reset : UserControl { }
    public class Magnet : UserControl { }

    public class PaintingLevel : UserControl, ICloneable
    {
        public object Clone()
        {
            return new PaintingLevel
            {
                Foreground = this.Foreground,
                Background = this.Background
            };
        }
    }
    public class PaintingTrend : UserControl, ICloneable
    {
        public object Clone()
        {
            return new PaintingTrend
            {
                Foreground = this.Foreground,
                Background = this.Background
            };
        }
    }

    public class Arrow : UserControl
    {
        public static readonly DependencyProperty ArrowDirectionProperty;

        public Direction ArrowDirection
        {
            get { return (Direction)GetValue(ArrowDirectionProperty); }
            set { SetValue(ArrowDirectionProperty, value); }
        }

        static Arrow()
        {
            ArrowDirectionProperty = DependencyProperty.Register("ArrowDirection", typeof(Direction), typeof(Arrow), new PropertyMetadata { DefaultValue = Direction.Right });
        }
    }

    public enum Direction
    {
        Right,
        Left,
        Up,
        Down
    }
}
