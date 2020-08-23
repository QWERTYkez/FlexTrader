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

using ChartModules;
using FlexTrader.MVVM.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace FlexTrader.MVVM.Views
{
    public abstract class ChartWindow : Window, IChartWindow
    {
        public ChartWindow()
        {
            this.Closing += (s, e) => SettingsWindow?.Close();

            this.PreviewKeyDown += (s, e) => { KeyPressed?.Invoke(e); };
            this.PreviewKeyUp += (s, e) => { KeyReleased?.Invoke(e); };

            ContextMenuPopup = new Popup
            {
                Placement = PlacementMode.Mouse,
                Child = new Border 
                {
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.White,
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    Child = ContextMenuSP
                }
            };
            ContextMenuPopup.MouseEnter += (s, e) => { OverMenu = true; };
            ContextMenuPopup.MouseLeave += (s, e) => { OverMenu = false; };
            Initialized += (s, e) => { ((Grid)this.Content).Children.Add(ContextMenuPopup); };
            this.PreviewMouseLeftButtonDown += (s, e) => 
            { if (!OverMenu) { InvokeRemoveMenu(); ContextMenuPopup.IsOpen = false; } };
        }

        #region Обработка таскания мышью
        private Point StartPosition;
        private Action<Vector?> ActA;
        private Action ActB;
        public void MoveCursor(MouseButtonEventArgs e, Action<Vector?> ActA, Action ActB = null)
        {
            StartPosition = e.GetPosition(this); this.ActA = ActA; this.ActB = ActB;

            this.MouseLeftButtonUp += (obj, e) => EndMoving();
            this.MouseMove += MovingAct;
        }
        private void MovingAct(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released) EndMoving();
            ActA?.Invoke(e.GetPosition(this) - StartPosition);
        }
        private void EndMoving()
        {
            ActA?.Invoke(null);
            this.MouseMove -= MovingAct;
            ActA = null;
            ActB?.Invoke();
            ActB = null;
        }
        #endregion

        #region Блок окна настроек
        private SettingsWindow SettingsWindow;
        public void ShowSettings(List<(string SetsName, List<Setting> Sets)> sb,
                                 List<(string SetsName, List<Setting> Sets)> sn,
                                 List<(string SetsName, List<Setting> Sets)> st)
        {
            SettingsWindow?.Close();
            SettingsWindow = new SettingsWindow(sb, sn, st);
            SettingsWindow.Show();
        }

        #endregion

        #region Блок TopMenu
        private bool Scrollable = false;
        private double LastZize;
        private protected void TopPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (OverlayMenu.Visibility == Visibility.Visible)
            {
                bool b;
                if (BWP.ActualWidth + 10 > OverlayMenu.ActualWidth && !Scrollable)
                {
                    LastZize = BWP.ActualWidth + 10;
                    b = false;
                    Scrollable = true;
                    goto ending;
                }
                else if (Scrollable && LastZize <= OverlayMenu.ActualWidth)
                {
                    b = true;
                    Scrollable = false;
                    goto ending;
                }
                return;
            ending:
                foreach (var s in Slidings)
                    s.AlwaysOpen = b;
            }
        }

        private protected ScrollViewer TopPanel { get; set; }
        private protected ScrollViewer OverlayMenu { get; set; }
        private WrapPanel BWP { get; set; }
        private List<Sliding> Slidings { get; set; }
        public void SetMenu(string SetsName, List<Setting> Sets)
        {
            if (Sets != null)
            {
                OverlayMenu.Visibility = Visibility.Visible;
                TopPanel.Visibility = Visibility.Hidden;

                BWP = new WrapPanel { Height = 52, HorizontalAlignment = HorizontalAlignment.Left };
                {
                    BWP.Children.Add(new Viewbox
                    {
                        Height = 40,
                        Width = 75,
                        Margin = new Thickness(2.5, 0, 2.5, 0),

                        Child = new Label
                        {
                            VerticalContentAlignment = VerticalAlignment.Center,
                            Content = SetsName,

                            FontFamily = new FontFamily("Consolas"),
                            Foreground = Brushes.White,
                            FontSize = 18
                        }
                    });

                    Slidings = new List<Sliding>();


                    {// Lock button

                        var L = new Lock
                        {
                            Foreground = Brushes.White,
                            Margin = new Thickness(2.5, 0, 2.5, 0),
                            Locked = (bool)(Sets[0].Get())
                        };
                        var lpb = new PaletteButton()
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Content = L,
                            IsActive = L.Locked
                        };
                        lpb.Click += (s,e) => 
                        { 
                            L.Locked = !L.Locked;
                            Sets[0].Set(L.Locked);
                            lpb.IsActive = L.Locked;
                        };


                        BWP.Children.Add(lpb);
                    }
                    

                    var WP = new WrapPanel { Margin = new Thickness(5), Height = 42, HorizontalAlignment = HorizontalAlignment.Left };
                    {
                        for (int i = 1; i < Sets.Count; i++)
                        {
                            FrameworkElement fe = null;
                            switch (Sets[i].Type)
                            {
                                case SetType.Brush:
                                    WP.Children.Add(fe = new Sliding
                                    {
                                        Background = Brushes.Teal,
                                        Foreground = Brushes.White,
                                        Title = Sets[i].Name,
                                        ContentWidth = 40,
                                        AlwaysOpen = true,
                                        Content = new ColorPicker(Sets[i].Get() as SolidColorBrush, Sets[i].Set)
                                        {
                                            CornerRadius = 10
                                        }
                                    });
                                    Slidings.Add(fe as Sliding);
                                    break;
                                case SetType.DoublePicker:
                                    WP.Children.Add(fe = new Sliding
                                    {
                                        Background = Brushes.Teal,
                                        Foreground = Brushes.White,
                                        Title = Sets[i].Name,
                                        ContentWidth = 100,
                                        AlwaysOpen = true,
                                        Content = new DoublePicker((double)Sets[i].Get(), Sets[i].Set, (double?)Sets[i].Param1, (double?)Sets[i].Param2)
                                    });
                                    Slidings.Add(fe as Sliding);
                                    break;
                                case SetType.DoubleSlider:
                                    WP.Children.Add(fe = new Sliding
                                    {
                                        Background = Brushes.Teal,
                                        Foreground = Brushes.White,
                                        Title = Sets[i].Name,
                                        ContentWidth = 100,
                                        AlwaysOpen = true,
                                        Content = new DoubleSlider((double)Sets[i].Get(), Sets[i].Set, (double)Sets[i].Param1, (double)Sets[i].Param2)
                                        { Foreground = Brushes.White }
                                    });
                                    Slidings.Add(fe as Sliding);
                                    break;
                            }
                            fe.Height = 40;
                            fe.VerticalAlignment = VerticalAlignment.Center;
                        }
                    }
                    BWP.Children.Add(WP);
                }
                OverlayMenu.Content = BWP;
            }
            else
            {
                OverlayMenu.Visibility = Visibility.Hidden;
                TopPanel.Visibility = Visibility.Visible;
            }
        }
        #endregion

        #region Контекстное меню
        private bool OverMenu = false;
        private StackPanel ContextMenuSP { get; } = new StackPanel();
        private protected Popup ContextMenuPopup { get; }
        private Action RemoveMenu { get; set; }
        private void InvokeRemoveMenu()
        {
            RemoveMenu?.Invoke(); 
            RemoveMenu = null;
        }
        public void ShowContextMenu((List<(string Name, Action Act)> Items, Action RemoveMenu) Menu)
        {
            InvokeRemoveMenu();
            ContextMenuPopup.IsOpen = false;
            RemoveMenu = Menu.RemoveMenu;
            ContextMenuSP.Children.Clear();
            {
                foreach (var item in Menu.Items)
                {
                    if (item.Name != "+++")
                    {
                        var btn = new Button
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            Content = item.Name
                        };
                        btn.Click += (s, e) =>
                        {
                            Task.Run(() => item.Act.Invoke());
                            InvokeRemoveMenu();
                            ContextMenuPopup.IsOpen = false;
                        };
                        btn.Style = (Style)btn.FindResource("ContextMenuButton");
                        ContextMenuSP.Children.Add(btn);
                    }
                    else
                    {
                        ContextMenuSP.Children.Add(new Border 
                        {
                            BorderBrush = new SolidColorBrush(Color.FromRgb(75, 75, 75)),
                            BorderThickness = new Thickness(1)
                        });
                    }
                }
            }
            ContextMenuPopup.IsOpen = true;
        }
        #endregion

        public abstract event Action<string> SetInstrument;
        public abstract string CurrentInstrument { get; }

        public abstract event Action<bool> SetMagnet;
        public abstract bool CurrentMagnetState { get; }

        public event Action<KeyEventArgs> KeyPressed;
        public event Action<KeyEventArgs> KeyReleased;

        public abstract void ResetPB(string Name);
    }
}
