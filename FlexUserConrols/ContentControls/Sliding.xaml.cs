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

namespace FlexUserConrols.ContentControls
{
    public class Sliding : ContentControl 
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(Sliding));
        public static readonly DependencyProperty ContentWidthProperty = DependencyProperty.Register("ContentWidth", typeof(double), typeof(Sliding));
        public static readonly DependencyProperty FreezeProperty = DependencyProperty.Register("Freeze", typeof(bool), typeof(Sliding));
        public static readonly DependencyProperty AlwaysOpenProperty = DependencyProperty.Register("AlwaysOpen", typeof(bool), typeof(Sliding));

        public double ContentWidth
        {
            get { return (double)GetValue(ContentWidthProperty); }
            set { SetValue(ContentWidthProperty, value + 10); }
        }
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        public bool Freeze
        {
            get { return (bool)GetValue(FreezeProperty); }
            set { SetValue(FreezeProperty, value); }
        }
        public bool AlwaysOpen
        {
            get { return (bool)GetValue(AlwaysOpenProperty); }
            set { SetValue(AlwaysOpenProperty, value); }
        }

        static Sliding()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Sliding),
                new FrameworkPropertyMetadata(typeof(Sliding)));
        }
    }
}