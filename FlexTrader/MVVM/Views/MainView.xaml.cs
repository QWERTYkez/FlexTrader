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
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace FlexTrader.MVVM.Views
{
    public partial class MainView : ChartWindow
    {
        public MainView()
        {
            InitializeComponent();

            TopPanel = this.xTopPanel;
            TopPanel.SizeChanged += TopPanel_SizeChanged;
            OverlayMenu = this.xOverlayMenu;

            Palette.Tag = PaletteButtonNormal;
            PaletteButtonNormal.IsActive = true;

            ((ViewModels.MainViewModel)DataContext).Initialize(this);

            SetInsrument(CurrentInstrument); SetMagnet();
        }

        private Popup Pop1;
        private Popup Pop2;

        private string CurrentInstrument => (string)(((PaletteButton)(Palette.Tag)).Tag);

        private void PaletteButton_MouseEnter(object sender, MouseEventArgs e)
        {
            e.Handled = true;

            if (Pop1 != null) Pop1.IsOpen = false;
            if (Pop2 != null) Pop2.IsOpen = false;

            var grd = (Grid)sender;
            grd.Tag = "Focused";
            Pop1 = (Popup)grd.Children[1]; Pop1.IsOpen = true;
            Pop2 = (Popup)grd.Children[2]; Pop2.IsOpen = true;
        }
        private void PaletteButton_MouseLeave(object sender, MouseEventArgs e)
        {
            var grd = (Grid)sender;
            grd.Tag = "MouseLeave";
            var pop1 = (Popup)grd.Children[1];
            var pop2 = (Popup)grd.Children[2];
            Task.Run(() => 
            {
                Thread.Sleep(1000);
                Dispatcher.Invoke(() => 
                {
                    if ((string)grd.Tag == "MouseLeave")
                    {
                        grd.Tag = "Closed";
                        pop1.IsOpen = false;
                        pop2.IsOpen = false;
                    }
                });
            });
        }
        private void PopSP_MouseEnter(object sender, MouseEventArgs e)
        {
            var grd = (Grid)((Popup)sender).Parent;
            grd.Tag = "Focused";
        }
        private void PopSP_MouseLeave(object sender, MouseEventArgs e)
        {
            var grd = (Grid)((Popup)sender).Parent;
            grd.Tag = "MouseLeave";
            var pop1 = (Popup)grd.Children[1];
            var pop2 = (Popup)grd.Children[2];
            Task.Run(() =>
            {
                Thread.Sleep(1000);
                Dispatcher.Invoke(() =>
                {
                    if ((string)grd.Tag == "MouseLeave")
                    {
                        grd.Tag = "Closed";
                        pop1.IsOpen = false;
                        pop2.IsOpen = false;
                    }
                });
            });
        }
        private void PBC(object sender, RoutedEventArgs e)
        {
            if (Palette.Tag is PaletteButton lbtn) lbtn.IsActive = false;
            var btn = sender as PaletteButton;
            btn.IsActive = true;
            Palette.Tag = btn;
            SetInsrument((string)btn.Tag);
        }
        public override void ResetInstrument(string Name)
        {
            Dispatcher.Invoke(() => 
            {
                if (Palette.Tag is PaletteButton lbtn) lbtn.IsActive = false;
                var btn = Name switch
                {
                    "Interacion" => PaletteButtonInteracion,
                    _ => PaletteButtonNormal,
                };
                btn.IsActive = true;
                Palette.Tag = btn;
                SetInsrument((string)btn.Tag);
            });
        }
        private void PBCmenu(object sender, RoutedEventArgs e)
        {
            var newbtn = sender as PaletteButton;
            var lastbtn = ((Grid)((Popup)((Border)((Grid)newbtn.Parent).Parent).Parent).Parent).Children[0] as PaletteButton;
            if(CurrentInstrument == (string)lastbtn.Tag)
            {
                SetInsrument((string)newbtn.Tag);
            }  
            lastbtn.Tag = newbtn.Tag;
            lastbtn.Content = ((ICloneable)newbtn.Content).Clone();
            PBC(lastbtn, null);
        }

        private protected override bool CurrentMagnetState { get; set; }
        private protected override Grid ChartsGRD { get => ChartsGrid;  }

        private void BTN_SetMagnetState(object sender, RoutedEventArgs e)
        {
            MagnetBtn.IsActive = !MagnetBtn.IsActive;
            CurrentMagnetState = MagnetBtn.IsActive;
            SetMagnet();
        }

        private void HidePalette(object sender, RoutedEventArgs e)
        {
            if (Palette.Visibility == Visibility.Visible)
            {
                Palette.Visibility = Visibility.Collapsed;
                LeftArrow.ArrowDirection = Direction.Right;
            }
            else
            {
                Palette.Visibility = Visibility.Visible;
                LeftArrow.ArrowDirection = Direction.Left;
            }
        }

        private void ScrollingBar(object sender, MouseWheelEventArgs e)
        {
            var sv = sender as ScrollViewer;
            if (e.Delta < 0)
            {
                sv.LineRight();
                sv.LineRight();
            }
            else
            {
                sv.LineLeft();
                sv.LineLeft();
            }
        }
    }
}