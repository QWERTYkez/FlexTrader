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

using ChartModules;
using FlexTrader.MVVM.Resources;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
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

        public abstract ScrollViewer TopPanel { get; }
        public abstract ScrollViewer OverlayMenu { get; }
        public void SetMenu(string SetsName, List<Setting> Sets)
        {
            if (Sets != null)
            {
                OverlayMenu.Visibility = Visibility.Visible;
                TopPanel.Visibility = Visibility.Hidden;

                var WP = new WrapPanel { Margin = new Thickness(5), Height = 42 };
                {
                    WP.Children.Add(new Viewbox
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

                    var x = new Label
                    {
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Height = 40,
                        Margin = new Thickness(2.5, 0, 2.5, 0),
                        Content = SetsName,

                        FontFamily = new FontFamily("Consolas"),
                        Foreground = Brushes.White,
                        FontSize = 18
                    };

                    foreach (var s in Sets)
                    {
                        FrameworkElement fe = null;
                        switch (s.Type)
                        {
                            case SetType.Brush:
                                WP.Children.Add(fe = new ColorPicker(s.Get() as SolidColorBrush, s.Set)
                                {
                                    Width = 40,
                                    CornerRadius = 10
                                });
                                break;
                            case SetType.DoublePicker:
                                WP.Children.Add(fe = new Sliding
                                {
                                    Background = Brushes.Teal,
                                    Foreground = Brushes.White,
                                    Title = s.Name,
                                    ContentWidth = 100,
                                    Content = new DoublePicker((double)s.Get(), s.Set, (double?)s.Param1, (double?)s.Param2)
                                    {
                                        Background = Brushes.Black, 
                                        Foreground = Brushes.White, 
                                        FontSize = 16
                                    }
                                });
                                break;
                        }
                        fe.Height = 40;
                        fe.Margin = new Thickness(2.5, 0, 2.5, 0);
                    }
                }
                OverlayMenu.Content = WP;
            }
            else
            {
                OverlayMenu.Visibility = Visibility.Hidden;
                TopPanel.Visibility = Visibility.Visible;
            }
        }

        public abstract event Action<string> SetInstrument;
        public abstract string CurrentInstrument { get; }

        public abstract event Action<bool> SetMagnet;
        public abstract bool CurrentMagnetState { get; }

        public event Action<KeyEventArgs> KeyPressed;
        public event Action<KeyEventArgs> KeyReleased;

        public abstract void ResetPB(string Name);
    }
}
