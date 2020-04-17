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

using FlexTrader.MVVM.Resources;
using FlexTrader.MVVM.Views.ChartModules;
using System;
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
                if (bs.SetsName != null)
                {
                    var sp = AddLevel(BaseSP, bs.SetsName);
                    foreach (var s in bs.Sets)
                    {
                        switch (s.Type)
                        {
                            case SetType.GoDown: sp = AddLevel(sp, s.Name); break;
                            case SetType.GoUp: sp = (sp.Parent as Expander).Parent as StackPanel; break;
                            case SetType.Brush:
                                AddSetting(sp, s, (obj) => new ColorPicker(obj as SolidColorBrush, s.Set));
                                break;
                            case SetType.DoubleSlider:
                                AddSetting(sp, s, (obj) => new DoubleSlider((double)obj,
                                    (double)s.Param1, (double)s.Param2, s.Set)); break;
                        }
                    }
                }
            }
        }

        private StackPanel AddLevel(StackPanel sp, string header)
        {
            if (header == null || header == "") header = "---";

            var nsp = new StackPanel { Margin = new Thickness(20, 5, 0, 5) };
            var exp = new Expander 
            { 
                Header = header, 
                Content = nsp, 
                IsExpanded = true, 
                FontSize = 12, 
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold 
            };
            sp.Children.Add(exp);
            return nsp;
        }
        private void AddSetting(StackPanel sp, Setting s, Func<object, dynamic> GetEl = null)
        {
            var grd = new Grid { Margin = new Thickness(20, 2, 50, 2) };
            grd.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            grd.ColumnDefinitions.Add(new ColumnDefinition());
            grd.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            var lb = new Label 
            { 
                Content = s.Name, 
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold 
            };
            grd.Children.Add(lb);

            if (GetEl != null)
            {
                dynamic el = GetEl.Invoke(s.Obj);
                grd.Tag = el;
                el.HorizontalAlignment = HorizontalAlignment.Right;
                el.Width = 200;
                Grid.SetColumn(el, 1);
                grd.Children.Add(el);

                if (s.ResetObj != null)
                {
                    var btn = new Button
                    {
                        Content = new Reset { Foreground = Brushes.Black },
                        Width = 30,
                        Height = 30,
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0)
                    };
                    btn.Margin = new Thickness(5, 0, 15, 0);
                    btn.Click += (st, e) =>
                    {
                        s.Set.Invoke(s.ResetObj);
                        grd.Children.Remove(grd.Tag as UIElement);

                        dynamic el = GetEl.Invoke(s.ResetObj);
                        grd.Tag = el;
                        el.HorizontalAlignment = HorizontalAlignment.Right;
                        el.Width = 200;
                        Grid.SetColumn(el, 1);
                        grd.Children.Add(el);
                    };
                    Grid.SetColumn(btn, 2);
                    grd.Children.Add(btn);
                }
                else
                {
                    el.Margin = new Thickness(0, 0, 50, 0);
                }
            }

            sp.Children.Add(grd);
        }
    }
}