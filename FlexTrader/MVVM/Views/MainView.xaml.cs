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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace FlexTrader.MVVM.Views
{
    public partial class MainView : ChartWindow
    {
        public MainView()
        {
            InitializeComponent();

            Palette.Tag = PaletteButtonNormal;
            PaletteButtonNormal.IsActive = true;

            ((ViewModels.MainViewModel)DataContext).Initialize(this);
        }

        private Popup Pop1;
        private Popup Pop2;

        public override event Action<string> SetInstrument;
        
        public override string CurrentInstrument { get => (string)(((PaletteButton)(Palette.Tag)).Tag); }

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
            SetInstrument?.Invoke((string)btn.Tag);
        }
        private void PBCmenu(object sender, RoutedEventArgs e)
        {
            var newbtn = sender as PaletteButton;
            var lastbtn = ((Grid)((Popup)((Border)((Grid)newbtn.Parent).Parent).Parent).Parent).Children[0] as PaletteButton;
            if(CurrentInstrument == (string)lastbtn.Tag) SetInstrument?.Invoke((string)newbtn.Tag);
            lastbtn.Tag = newbtn.Tag;
            switch ((string)lastbtn.Tag)
            {
                case "PaintingLeves": lastbtn.Content = new PaintingLevel { Foreground = Brushes.White }; break;
                case "PaintingTrends": lastbtn.Content = new PaintingTrend { Foreground = Brushes.White }; break;
            }
        }

        public override event Action<bool> SetMagnet;
        public override bool CurrentMagnetState => MagnetBtn.IsActive;

        private void SetMagnetState(object sender, RoutedEventArgs e)
        {
            MagnetBtn.IsActive = !MagnetBtn.IsActive;
            SetMagnet?.Invoke(MagnetBtn.IsActive);
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
    }
}