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

using ChartsCore.Core.CenterIndicators;
using ChartsCore.Core.StandardModules;
using FlexUserConrols.Buttons;
using FlexUserConrols.ContentControls;
using FlexUserConrols.Figures;
using FlexUserConrols.Pickers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ChartsCore.Core
{
    public abstract class ChartShell : UserControl
    {
        public ChartShell(ChartWindow Parent)
        {
            this.Unloaded += (s, e) => SettingsWindow?.Close();

            Parent.PreviewKeyDown += (s, e) => { if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) GetControl(); };
            Parent.PreviewKeyUp += (s, e) => { if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) LoseControl(); };

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
            { if (!OverMenu) { InvokeRemoveHook(); ContextMenuPopup.IsOpen = false; } };

            //PreviewMouseLeftButtonDown
            Interaction = e => InstrumentsHandler?.Interaction?.Invoke(e);
            Moving = e => InstrumentsHandler?.Moving?.Invoke(e);
            PaintingLevel = e => InstrumentsHandler?.PaintingLevel(e);
            PaintingTrend = e => InstrumentsHandler?.PaintingTrend(e);

            //MouseMove
            HookElement = () => InstrumentsHandler?.HookElement?.Invoke();
            DrawPrototype = () => InstrumentsHandler?.DrawPrototype?.Invoke();
        }

        #region Инструменты
        private protected void ChartInstrumentAction(object s, MouseButtonEventArgs e)
        {
            RemoveHooks?.Invoke();
            LBDInstrument(e);

            ChartGotFocus(InstrumentsHandler);
        }

        public View InstrumentsHandler { get; set; }
        public List<CandlesModule> Candles { get; private set; } = new List<CandlesModule>();
        public ILookup<TimeSpan, CandlesModule> ClipsCandles { get; private set; }
        public void ResetClips() => ClipsCandles = Candles.ToLookup(c => c.DeltaTime);

        private protected abstract Grid ChartsGRD { get; }

        public event Action<PInstrument> PrepareInstrument;
        public event Action<CursorT> SetCursor; 
        public event Action RemoveHooks;
        public event Action<bool> ToggleInteraction;

        private Action<MouseButtonEventArgs> LBDInstrument { get; set; }
        private readonly Action<MouseButtonEventArgs> Interaction;
        private readonly Action<MouseButtonEventArgs> Moving;
        private readonly Action<MouseButtonEventArgs> PaintingLevel;
        private readonly Action<MouseButtonEventArgs> PaintingTrend;

        public Action MMInstrument { get; set; }
        private readonly Action HookElement;
        private readonly Action DrawPrototype;

        private bool MagnetInstrument = false;
        public abstract void ResetInstrument(string Name);
        private bool InteractionF = false;
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
                        LBDInstrument = PaintingLevel; PrepareInstrument(PInstrument.Level);
                        Painting = true; MagnetInstrument = true; t = CursorT.Paint;
                        MMInstrument = DrawPrototype;
                        break;

                    case "PaintingTrends":
                        LBDInstrument = PaintingTrend; PrepareInstrument(PInstrument.Trend);
                        Painting = true; MagnetInstrument = true; t = CursorT.Paint;
                        MMInstrument = DrawPrototype;
                        break;

                    case "Interacion":
                        LBDInstrument = Interaction; Painting = false; InteractionF = true;
                        MagnetInstrument = true; t = CursorT.Hook;
                        MMInstrument = HookElement; ToggleInteraction(true);
                        break;

                    default:
                        LBDInstrument = Moving; Painting = false;
                        MagnetInstrument = false; t = CursorT.Standart; 
                        MMInstrument = null; 
                        break; 
                }
                if (InstrumentName != "Interacion" && InteractionF)
                { ToggleInteraction(false); InteractionF = false; }

                SetCursor(t);
                SetMagnet();
            });
        }

        private protected abstract bool CurrentMagnetState { get; set; }
        public event Action<bool> ToggleMagnet;
        private protected void SetMagnet() => 
            ToggleMagnet(MagnetInstrument && CurrentMagnetState);

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
                if (InteractionF || ControlUsed) ResetInstrument(null);
                Controlled = false;
            });
        }

        public event Action<bool> ToggleClipTime;
        private protected void SetClipTime(bool b) => ToggleClipTime(b);
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

        private protected ScrollViewer TopPanel { get; set; }
        private protected ScrollViewer OverlayMenu { get; set; }
        private WrapPanel BWP { get; set; }
        private Action RemoveTopMenuHook;
        private SolidColorBrush PanelBackground = new SolidColorBrush(Color.FromRgb(20, 43, 48));
        private List<Sliding> Slidings { get; set; }
        public void SetMenu(object sender, MouseButtonEventArgs e) => SetMenu(null, null, null, null);
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

                    BWP = new WrapPanel { Height = 50, HorizontalAlignment = HorizontalAlignment.Left };
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

                        var BaseButtonsGrd = new Grid();
                        BaseButtonsGrd.ColumnDefinitions.Add(new ColumnDefinition());
                        BaseButtonsGrd.ColumnDefinitions.Add(new ColumnDefinition());
                        BaseButtonsGrd.ColumnDefinitions.Add(new ColumnDefinition());
                        BWP.Children.Add(BaseButtonsGrd);

                        Slidings = new List<Sliding>();

                        WrapPanel wp;
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
                                            Locked = set.GetBool()
                                        };
                                        var lpb = new PaletteButton(PaletteType.Vertical)
                                        {
                                            VerticalAlignment = VerticalAlignment.Center,
                                            Margin = new Thickness(2.5, 0, 2.5, 0),
                                            Content = L,
                                            IsActive = L.Locked
                                        };
                                        Grid.SetColumn(lpb, 0);
                                        lpb.Click += (s, e) =>
                                        {
                                            L.Locked = !L.Locked;
                                            set.SetBool(L.Locked);
                                            lpb.IsActive = L.Locked;
                                        };
                                        BaseButtonsGrd.Children.Add(lpb);
                                        break;
                                    case SetType.Move:
                                        var mv = new Mover(set.SetInt);
                                        Grid.SetColumn(mv, 1);
                                        BaseButtonsGrd.Children.Add(mv);
                                        break;
                                    case SetType.Delete:
                                        var dpb = new PaletteButton(PaletteType.Vertical)
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
                                        Grid.SetColumn(dpb, 2);
                                        dpb.Click += (s, e) =>
                                        {
                                            OverlayMenu.Visibility = Visibility.Hidden;
                                            TopPanel.Visibility = Visibility.Visible;
                                            set.Delete();
                                            RemoveHook();
                                        };
                                        BaseButtonsGrd.Children.Add(dpb);
                                        break;
                                    case SetType.Brush:
                                        var cp = new ColorPicker(set.GetBrush(), set.SetBrush) { CornerRadius = 10 };
                                        cp.PickerMouseEnter += () => this.PreviewMouseLeftButtonDown -= SetMenu;
                                        cp.PickerMouseLeave += () => this.PreviewMouseLeftButtonDown += SetMenu;
                                        WP.Children.Add(fe = new Sliding
                                        {
                                            Background = Brushes.Teal,
                                            Foreground = Brushes.White,
                                            Title = set.Name,
                                            ContentWidth = 40,
                                            AlwaysOpen = true,
                                            Content = cp
                                        });
                                        Slidings.Add(fe as Sliding);
                                        break;
                                    case SetType.Double:
                                        WP.Children.Add(fe = new Sliding
                                        {
                                            Background = Brushes.Teal,
                                            Foreground = Brushes.White,
                                            Title = set.Name,
                                            ContentWidth = 100,
                                            AlwaysOpen = true,
                                            Content = new NumericPicker(set.GetDouble(), set.SetDouble, (double?)set.Param1, (double?)set.Param2, set.GetSetMinDouble, set.GetSetMaxDouble)
                                        });
                                        Slidings.Add(fe as Sliding);
                                        break;
                                    case SetType.IntPicker:
                                        WP.Children.Add(fe = new Sliding
                                        {
                                            Background = Brushes.Teal,
                                            Foreground = Brushes.White,
                                            Title = set.Name,
                                            ContentWidth = 60,
                                            AlwaysOpen = true,
                                            Content = new NumericPicker(set.GetInt(), set.SetInt, (int?)set.Param1, (int?)set.Param2, set.GetSetMinInt, set.GetSetMaxInt)
                                        });
                                        Slidings.Add(fe as Sliding);
                                        break;
                                    case SetType.IntSlider:
                                        WP.Children.Add(fe = new Sliding
                                        {
                                            Background = Brushes.Teal,
                                            Foreground = Brushes.White,
                                            Title = set.Name,
                                            ContentWidth = 100,
                                            AlwaysOpen = true,
                                            Content = new IntSlider(set.GetInt(), set.SetInt, (int)set.Param1, (int)set.Param2, set.GetSetMinInt, set.GetSetMaxInt)
                                            { Foreground = Brushes.White }
                                        });
                                        Slidings.Add(fe as Sliding);
                                        break;
                                    case SetType.GoDown:
                                        wp = new WrapPanel { Tag = WP };
                                        wp.Children.Add(new Border 
                                        {
                                            Margin = new Thickness(3, 0, 0, 0),
                                            VerticalAlignment = VerticalAlignment.Center,
                                            Background = PanelBackground,
                                            CornerRadius = new CornerRadius(5),
                                            Child = new Label
                                            {
                                                Content = set.Name,
                                                Padding = new Thickness(2),
                                                FontSize = 18,
                                                Foreground = Brushes.White,
                                                FontWeight = FontWeights.Bold,
                                                FontFamily = new FontFamily("Consolas")
                                            }
                                        });
                                        WP.Children.Add(new Border
                                        {
                                            Margin = new Thickness(2.5, 0, 2.5, 0),
                                            Background = Brushes.Teal,
                                            CornerRadius = new CornerRadius(5),
                                            Child = wp
                                        });
                                        WP = wp;
                                        break;
                                    case SetType.GoUp:
                                        foreach (var el in WP.Children)
                                            if (el is Sliding)
                                            {
                                                ((Sliding)el).BorderThickness = new Thickness(0);
                                                ((Sliding)el).Margin = new Thickness(0);

                                            }
                                        WP = (WrapPanel)WP.Tag;
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
                        var btn = new ContextMenuButton
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            Content = item.Name
                        };
                        btn.Click += (s, e) =>
                        {
                            Task.Run(() => item.Act());
                            InvokeRemoveHook();
                            ContextMenuPopup.IsOpen = false;
                        };
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

        #region ВыборЧарта
        public View SelectedChart { get; private set; }
        public void ChartGotFocus(View sender)
        {
            if (SelectedChart != null) SelectedChart.Selected = false;
            sender.Selected = true;
            SelectedChart = sender;
        }
        #endregion
    }
}
