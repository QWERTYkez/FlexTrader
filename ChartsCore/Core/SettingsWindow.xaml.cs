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

using ChartsCore.Core;
using ChartsCore.Resources;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChartsCore.Core
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(List<(string SetsName, List<Setting> Sets)> sb,
                              List<(string SetsName, List<Setting> Sets)> sn,
                              List<(string SetsName, List<Setting> Sets)> st)
        {
            InitializeComponent();

            AddSettingsToContainer(BaseSP, sb);
            //AddSettingsToContainer(SP1, sn);
            //AddSettingsToContainer(SP2, st);

            Show();
        }

        private void AddSettingsToContainer(StackPanel SP, List<(string SetsName, List<Setting> Sets)> Sets)
        {
            foreach (var bs in Sets)
            {
                if (bs.SetsName != null)
                {
                    StackPanel sp;
                    sp = AddLevel(SP, bs.SetsName);
                    for (int i = 0; i < bs.Sets.Count; i++)
                    {
                        switch (bs.Sets[i].Type)
                        {
                            case SetType.GoDown:

                                if (bs.Sets[i + 1].Type == SetType.Lock)
                                    sp = AddLevel(sp, bs.Sets[i].Name, bs.Sets[i + 1], bs.Sets[i + 2]);
                                else sp = AddLevel(sp, bs.Sets[i].Name);

                                break;
                            case SetType.GoUp:

                                sp = (sp.Parent as Expander).Parent as StackPanel;

                                break;
                            case SetType.Brush:

                                AddSetting(sp, bs.Sets[i], i, i => new ColorPicker(bs.Sets[i].GetBrush() as SolidColorBrush, bs.Sets[i].SetBrush));

                                break;
                            case SetType.Double:

                                AddSetting(sp, bs.Sets[i], i, i =>
                                        new NumericPicker(
                                            bs.Sets[i].GetDouble(),
                                            bs.Sets[i].SetDouble,
                                            (double?)bs.Sets[i].Param1,
                                            (double?)bs.Sets[i].Param2,
                                            bs.Sets[i].GetSetMinDouble,
                                            bs.Sets[i].GetSetMaxDouble));
                                break;
                            case SetType.IntPicker:

                                AddSetting(sp, bs.Sets[i], i, i =>
                                        new NumericPicker(
                                            bs.Sets[i].GetInt(),
                                            bs.Sets[i].SetInt,
                                            (int?)bs.Sets[i].Param1,
                                            (int?)bs.Sets[i].Param2,
                                            bs.Sets[i].GetSetMinInt,
                                            bs.Sets[i].GetSetMaxInt));
                                break;
                            case SetType.IntSlider:

                                AddSetting(sp, bs.Sets[i], i, i => 
                                        new IntSlider(
                                            bs.Sets[i].GetInt(),
                                            bs.Sets[i].SetInt,
                                            (int)bs.Sets[i].Param1,
                                            (int)bs.Sets[i].Param2,
                                            bs.Sets[i].GetSetMinInt,
                                            bs.Sets[i].GetSetMaxInt));
                                break;
                        }
                    }
                }
            }
        }

        private StackPanel AddLevel(StackPanel sp, string header, 
            Setting LockSetting = null, Setting DeleteSetting = null)
        {
            if (header == null || header == "") header = "---";

            var nsp = new StackPanel { Margin = new Thickness(20, 5, 0, 5) };
            var hr = new Grid { Height = 19, Width = 250 };

            nsp.SizeChanged += (s, e) => { hr.Width = nsp.ActualWidth - 105; };

            hr.Children.Add(new Label 
            {
                Content = header,
                Padding = new Thickness(0),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            });
            Expander exp = new Expander
            {
                Header = hr,
                Content = nsp,
                IsExpanded = true,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold
            };
            if (LockSetting != null && DeleteSetting != null)
            {
                var vb = new Viewbox { Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Right };
                {
                    var wp = new WrapPanel();

                    {// Lock button
                        var L = new Lock
                        {
                            Foreground = Brushes.White,
                            Locked = LockSetting.GetBool()
                        };
                        var lpb = new PaletteButtonLeft()
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 2.5, 0),
                            Content = L,
                            IsActive = L.Locked
                        };
                        lpb.Click += (s, e) =>
                        {
                            L.Locked = !L.Locked;
                            LockSetting.SetBool(L.Locked);
                            lpb.IsActive = L.Locked;
                        };
                        wp.Children.Add(lpb);
                    }

                    {// Delete button

                        var lpb = new PaletteButtonLeft()
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(2.5, 0, 0, 0),
                            Color = PaletteButtonColor.Red,
                            Content = new CrossHair
                            {
                                Foreground = Brushes.White,
                                Rotated = true
                            }
                        };
                        lpb.Click += (s, e) =>
                        {
                            sp.Children.Remove(exp);
                            DeleteSetting.Delete();
                        };
                        wp.Children.Add(lpb);
                    }

                    vb.Child = wp;
                }
                hr.Children.Add(vb);
            }
            sp.Children.Add(exp);
            return nsp;
        }
        private void AddSetting(StackPanel sp, Setting s, int i, Func<int, Control> GetEl = null)
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
                var El = GetEl(i);
                El.HorizontalAlignment = HorizontalAlignment.Right;
                El.Width = 200;
                El.Height = 30;
                Grid.SetColumn(El, 1);
                grd.Children.Add(El);

                if (s.Reset != null)
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
                        s.Reset();
                        grd.Children.Remove(El);

                        El = GetEl(i);
                        El.HorizontalAlignment = HorizontalAlignment.Right;
                        El.Width = 200;
                        El.Height = 30;
                        Grid.SetColumn(El, 1);
                        grd.Children.Add(El);
                    };
                    Grid.SetColumn(btn, 2);
                    grd.Children.Add(btn);
                }
                else
                {
                    El.Margin = new Thickness(0, 0, 50, 0);
                }
            }

            sp.Children.Add(grd);
        }
    }
}