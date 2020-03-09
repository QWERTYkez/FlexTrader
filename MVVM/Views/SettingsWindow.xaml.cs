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

using FlexTrader.MVVM.Views.ChartModules;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FlexTrader.MVVM.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(List<(string SetsName, List<Setting> Sets)> sb,
                              List<(string SetsName, List<Setting> Sets)> sn,
                              List<(string SetsName, List<Setting> Sets)> st)
        {
            InitializeComponent();

            foreach (var bs in sb)
            {
                var sp = AddLevel(BaseStack, bs.SetsName);
                foreach (var s in bs.Sets)
                {
                    switch (s.Type)
                    {
                        case SetType.GoDown: sp = AddLevel(sp, s.Name); break;
                        case SetType.GoUp: sp = sp.Parent as StackPanel; break;
                        case SetType.Brush:
                            {
                                sp.Children.Add(new Label { Content = s.Name });
                            }
                            break;
                        case SetType.DoubleSlider:
                            {
                                sp.Children.Add(new Label { Content = s.Name });
                            }
                            break;
                    }
                }
            }

            var xxx = new Resources.ColorPicker(sb[0].Sets[1].Get.Invoke() as SolidColorBrush, sb[0].Sets[1].Set);
            Other.Content = xxx;
            xxx.Width = 50; xxx.Height = 50;
        }

        private StackPanel AddLevel(StackPanel SP, string Header)
        {
            var nsp = new StackPanel { Margin = new Thickness(20,0,0,0) };

            SP.Children.Add(new Label { Content = Header });
            SP.Children.Add(nsp);
            return nsp;
        }
    }
}