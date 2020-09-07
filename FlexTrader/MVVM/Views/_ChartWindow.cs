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
using ChartModules.PaintingModule;
using ChartModules.StandardModules;
using FlexTrader.MVVM.Resources;
using System;
using System.Collections.Generic;
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
            KeyPressed += e => { if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) GetControl(); };
            KeyReleased += e => { if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) LoseControl(); };
            Initialized += (s, e) => { ((Grid)this.Content).Children.Add(ContextMenuPopup); };
            this.PreviewMouseLeftButtonDown += (s, e) =>
            { if (!OverMenu) { InvokeRemoveHook(); ContextMenuPopup.IsOpen = false; } };

            this.PreviewMouseLeftButtonDown += CW_PreviewMouseLeftButtonDown;

            //PreviewMouseLeftButtonDown
            Interacion = e => InstrumentsHandler?.Interacion?.Invoke(e);
            Moving = e => InstrumentsHandler?.Moving?.Invoke(e);
            PaintingLevel = e => InstrumentsHandler?.PaintingLevel.Invoke(e);
            PaintingTrend = e => InstrumentsHandler?.PaintingTrend.Invoke(e);

            //MouseMove
            HookElement = () => InstrumentsHandler?.HookElement?.Invoke();
            DrawPrototype = () => InstrumentsHandler?.DrawPrototype?.Invoke();
        }

        #region Инструменты
        private protected void ChartsGRD_PreviewMouseLeftButtonDown(object s, MouseButtonEventArgs e)
        {
            RemoveHooks?.Invoke();
            LBDInstrument.Invoke(e);
        }

        public IHaveInstruments InstrumentsHandler { get; set; }
        private protected abstract Grid ChartsGRD { get; }

        public event Action<PInstrument> PrepareInstrument;
        public event Action<CursorT> SetCursor; 
        public event Action RemoveHooks;
        public event Action<bool> ToggleInteraction;

        private Action<MouseButtonEventArgs> LBDInstrument { get; set; }
        private readonly Action<MouseButtonEventArgs> Interacion;
        private readonly Action<MouseButtonEventArgs> Moving;
        private readonly Action<MouseButtonEventArgs> PaintingLevel;
        private readonly Action<MouseButtonEventArgs> PaintingTrend;

        public Action MMInstrument { get; set; }
        private readonly Action HookElement;
        private readonly Action DrawPrototype;

        private bool MagnetInstrument = false;
        public abstract void ResetInstrument(string Name);
        private bool Interaction = false;
        private bool painting = false;
        private bool Painting
        {
            get => painting;
            set
            {
                painting = value;
                if (!painting) ClearPrototypes?.Invoke();
            }
        }
        public event Action ClearPrototypes;
        private protected void SetInsrument(string InstrumentName)
        {
            Task.Run(() =>
            {
                CursorT t = CursorT.None;
                switch (InstrumentName)
                {
                    case "PaintingLevels":
                        LBDInstrument = PaintingLevel; PrepareInstrument.Invoke(PInstrument.Level);
                        Painting = true; MagnetInstrument = true; t = CursorT.Paint;
                        MMInstrument = DrawPrototype;
                        break;

                    case "PaintingTrends":
                        LBDInstrument = PaintingTrend; PrepareInstrument.Invoke(PInstrument.Trend);
                        Painting = true; MagnetInstrument = true; t = CursorT.Paint;
                        MMInstrument = DrawPrototype;
                        break;

                    case "Interacion":
                        LBDInstrument = Interacion; Painting = false; Interaction = true;
                        MagnetInstrument = true; t = CursorT.Hook;
                        MMInstrument = HookElement; ToggleInteraction.Invoke(true);
                        break;

                    default:
                        LBDInstrument = Moving; Painting = false; //SetMenu(null, null, null, null, null);
                        MagnetInstrument = false; t = CursorT.Standart; 
                        MMInstrument = null; 
                        break; 
                }
                if (InstrumentName != "Interacion" && Interaction)
                {
                    ToggleInteraction.Invoke(false);
                    Interaction = false;
                }

                SetCursor.Invoke(t);
                SetMagnet();
            });
        }

        private protected abstract bool CurrentMagnetState { get; set; }
        public event Action<bool> ToggleMagnet;
        private protected void SetMagnet() => 
            ToggleMagnet.Invoke(MagnetInstrument && CurrentMagnetState);

        public bool Controlled { get; private set; } = false;
        public bool ControlUsed { get; set; }
        private void GetControl()
        {
            if (!Controlled)
            {
                Task.Run(() =>
                {
                    if (LBDInstrument == Moving)
                    {
                        ResetInstrument("Interacion");
                        Controlled = true;
                    }
                    if (Painting)
                    {
                        Controlled = true;
                        ControlUsed = false;
                    }
                });
            }

        }
        private void LoseControl()
        {
            if (Controlled) Task.Run(() =>
            {
                if (Interaction || ControlUsed) ResetInstrument(null);
                Controlled = false;
            });
        }
        #endregion

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
            Dispatcher.Invoke(() => 
            {
                SettingsWindow?.Close();
                SettingsWindow = new SettingsWindow(sb, sn, st);
                SettingsWindow.Closed += (s, e) => { SettingsWindow = null; };
            });
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
        private protected void CW_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetMenu(null, null, null, null);
        }

        private protected ScrollViewer TopPanel { get; set; }
        private protected ScrollViewer OverlayMenu { get; set; }
        private WrapPanel BWP { get; set; }
        private Action RemoveTopMenuHook;
        private List<Sliding> Slidings { get; set; }
        public void SetMenu(string SetsName, List<Setting> Sets, Action DrawHook, Action RemoveHook)
        {
            if (Sets != null)
            {
                DrawHook?.Invoke();
                RemoveTopMenuHook = RemoveHook;
                Dispatcher.Invoke(() => 
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

                        var WP = new WrapPanel { Margin = new Thickness(5), Height = 42, HorizontalAlignment = HorizontalAlignment.Left };
                        {
                            foreach (var set in Sets)
                            {
                                FrameworkElement fe = null;
                                switch (set.Type)
                                {
                                    case SetType.Lock:
                                        var L = new Lock
                                        {
                                            Foreground = Brushes.White,
                                            Locked = (bool)(set.Get())
                                        };
                                        var lpb = new PaletteButton()
                                        {
                                            VerticalAlignment = VerticalAlignment.Center,
                                            Margin = new Thickness(2.5, 0, 2.5, 0),
                                            Content = L,
                                            IsActive = L.Locked
                                        };
                                        lpb.Click += (s, e) =>
                                        {
                                            L.Locked = !L.Locked;
                                            set.Set(L.Locked);
                                            lpb.IsActive = L.Locked;
                                        };
                                        BWP.Children.Add(lpb);
                                        break;
                                    case SetType.Move:
                                        BWP.Children.Add(new Mover(set.Set));
                                        break;
                                    case SetType.Delete:
                                        var dpb = new PaletteButton()
                                        {
                                            VerticalAlignment = VerticalAlignment.Center,
                                            Margin = new Thickness(2.5, 0, 2.5, 0),
                                            Color = PaletteButtonColor.Red,
                                            Content = new CrossHair
                                            {
                                                Foreground = Brushes.White,
                                                Rotated = true
                                            }
                                        };
                                        dpb.Click += (s, e) =>
                                        {
                                            OverlayMenu.Visibility = Visibility.Hidden;
                                            TopPanel.Visibility = Visibility.Visible;
                                            set.Set(null);
                                            RemoveHook.Invoke();
                                        };
                                        BWP.Children.Add(dpb);
                                        break;
                                    case SetType.Brush:
                                        WP.Children.Add(fe = new Sliding
                                        {
                                            Background = Brushes.Teal,
                                            Foreground = Brushes.White,
                                            Title = set.Name,
                                            ContentWidth = 40,
                                            AlwaysOpen = true,
                                            Content = new ColorPicker(set.Get() as SolidColorBrush, set.Set)
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
                                            Title = set.Name,
                                            ContentWidth = 100,
                                            AlwaysOpen = true,
                                            Content = new DoublePicker((double)set.Get(), set.Set, (double?)set.Param1, (double?)set.Param2)
                                        });
                                        Slidings.Add(fe as Sliding);
                                        break;
                                    case SetType.DoubleSlider:
                                        WP.Children.Add(fe = new Sliding
                                        {
                                            Background = Brushes.Teal,
                                            Foreground = Brushes.White,
                                            Title = set.Name,
                                            ContentWidth = 100,
                                            AlwaysOpen = true,
                                            Content = new DoubleSlider((double)set.Get(), set.Set, (double)set.Param1, (double)set.Param2)
                                            { Foreground = Brushes.White }
                                        });
                                        Slidings.Add(fe as Sliding);
                                        break;
                                }
                                if (fe != null)
                                {
                                    fe.Height = 40;
                                    fe.VerticalAlignment = VerticalAlignment.Center;
                                }
                            }
                        }
                        BWP.Children.Add(WP);
                    }
                    OverlayMenu.Content = BWP;
                });
            }
            else
            {
                RemoveTopMenuHook?.Invoke(); RemoveTopMenuHook = null;
                Dispatcher.Invoke(() => 
                {
                    OverlayMenu.Visibility = Visibility.Hidden;
                    TopPanel.Visibility = Visibility.Visible;
                });
            }
        }
        #endregion

        #region Контекстное меню
        private bool OverMenu = false;
        private StackPanel ContextMenuSP { get; } = new StackPanel();
        private protected Popup ContextMenuPopup { get; }
        private static Style ContextMenuButtonStyle { get; } = (Style)(new Button()).FindResource("ContextMenuButton");
        private Action RemoveHook { get; set; }
        private void InvokeRemoveHook()
        {
            RemoveHook?.Invoke();
            RemoveHook = null;
        }
        public void ShowContextMenu((List<(string Name, Action Act)> Items, Action DrawHook, Action RemoveHook) Menu)
        {
            ContextMenuPopup.IsOpen = false;
            RemoveHook?.Invoke();
            Menu.DrawHook?.Invoke();
            RemoveHook = Menu.RemoveHook;
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
                            InvokeRemoveHook();
                            ContextMenuPopup.IsOpen = false;
                        };
                        btn.Style = ContextMenuButtonStyle;
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

        public event Action<KeyEventArgs> KeyPressed;
        public event Action<KeyEventArgs> KeyReleased;
    }
}
