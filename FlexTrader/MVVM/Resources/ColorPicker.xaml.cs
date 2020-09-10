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

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace FlexTrader.MVVM.Resources
{
    public partial class ColorPicker : UserControl
    {
        private readonly ColorPickerDataContext DC;
        public event Action<SolidColorBrush> BrushChanged;
        public ColorPicker() { new NotImplementedException(); }
        public ColorPicker(SolidColorBrush br, Action<object> set = null)
        {
            InitializeComponent();

            this.Width = Width;
            this.Height = Height;

            if (set != null) BrushChanged += b => 
                set.Invoke(b);

            DataContext = DC = new ColorPickerDataContext();
            DC.PropertyChanged += DC_PropertyChanged;
            DC.Initialize(br, () => Dispatcher.Invoke(() => Apply(null, null)));
            NewBrushChanged(br);

            AddChannelBinding(RedSlider, CChannels.Red);
            AddChannelBinding(GreenSlider, CChannels.Green);
            AddChannelBinding(BlueSlider, CChannels.Blue);
            AddChannelBinding(AlphaSlider, CChannels.Alpha);
            AddChannelBinding(RedTextBox, CChannels.Red, UpdateSourceTrigger.PropertyChanged);
            AddChannelBinding(GreenTextBox, CChannels.Green, UpdateSourceTrigger.PropertyChanged);
            AddChannelBinding(BlueTextBox, CChannels.Blue, UpdateSourceTrigger.PropertyChanged);
            AddChannelBinding(AlphaTextBox, CChannels.Alpha, UpdateSourceTrigger.PropertyChanged);

            #region AddingColors
            AddStandardColor(Standard, Brushes.White, "White");
            AddStandardColor(Standard, Brushes.Snow, "Snow");
            AddStandardColor(Standard, Brushes.Honeydew, "Honeydew");
            AddStandardColor(Standard, Brushes.MintCream, "MintCream");
            AddStandardColor(Standard, Brushes.Azure, "Azure");
            AddStandardColor(Standard, Brushes.AliceBlue, "AliceBlue");
            AddStandardColor(Standard, Brushes.GhostWhite, "GhostWhite");
            AddStandardColor(Standard, Brushes.WhiteSmoke, "WhiteSmoke");
            AddStandardColor(Standard, Brushes.SeaShell, "SeaShell");
            AddStandardColor(Standard, Brushes.Beige, "Beige");
            AddStandardColor(Standard, Brushes.OldLace, "OldLace");
            AddStandardColor(Standard, Brushes.FloralWhite, "FloralWhite");
            AddStandardColor(Standard, Brushes.Ivory, "Ivory");
            AddStandardColor(Standard, Brushes.AntiqueWhite, "AntiqueWhite");
            AddStandardColor(Standard, Brushes.Linen, "Linen");
            AddStandardColor(Standard, Brushes.LavenderBlush, "LavenderBlush");
            AddStandardColor(Standard, Brushes.MistyRose, "MistyRose");
            AddStandardColor(Standard, Brushes.Gainsboro, "Gainsboro");
            AddStandardColor(Standard, Brushes.LightGray, "LightGray");
            AddStandardColor(Standard, Brushes.Silver, "Silver");
            AddStandardColor(Standard, Brushes.DarkGray, "DarkGray");
            AddStandardColor(Standard, Brushes.Gray, "Gray");
            AddStandardColor(Standard, Brushes.DimGray, "DimGray");
            AddStandardColor(Standard, Brushes.LightSlateGray, "LightSlateGray");
            AddStandardColor(Standard, Brushes.SlateGray, "SlateGray");
            AddStandardColor(Standard, Brushes.DarkSlateGray, "DarkSlateGray");
            AddStandardColor(Standard, Brushes.Black, "Black", Brushes.White);
            AddStandardColor(Standard, Brushes.Cornsilk, "Cornsilk");
            AddStandardColor(Standard, Brushes.BlanchedAlmond, "BlanchedAlmond");
            AddStandardColor(Standard, Brushes.Bisque, "Bisque");
            AddStandardColor(Standard, Brushes.NavajoWhite, "NavajoWhite");
            AddStandardColor(Standard, Brushes.Wheat, "Wheat");
            AddStandardColor(Standard, Brushes.BurlyWood, "BurlyWood");
            AddStandardColor(Standard, Brushes.Tan, "Tan");
            AddStandardColor(Standard, Brushes.RosyBrown, "RosyBrown");
            AddStandardColor(Standard, Brushes.SandyBrown, "SandyBrown");
            AddStandardColor(Standard, Brushes.Goldenrod, "Goldenrod");
            AddStandardColor(Standard, Brushes.DarkGoldenrod, "DarkGoldenrod");
            AddStandardColor(Standard, Brushes.Peru, "Peru");
            AddStandardColor(Standard, Brushes.Chocolate, "Chocolate");
            AddStandardColor(Standard, Brushes.SaddleBrown, "SaddleBrown");
            AddStandardColor(Standard, Brushes.Sienna, "Sienna");
            AddStandardColor(Standard, Brushes.Brown, "Brown");
            AddStandardColor(Standard, Brushes.Maroon, "Maroon");
            AddStandardColor(Standard, Brushes.LightSalmon, "LightSalmon");
            AddStandardColor(Standard, Brushes.Salmon, "Salmon");
            AddStandardColor(Standard, Brushes.DarkSalmon, "DarkSalmon");
            AddStandardColor(Standard, Brushes.LightCoral, "LightCoral");
            AddStandardColor(Standard, Brushes.IndianRed, "IndianRed");
            AddStandardColor(Standard, Brushes.Crimson, "Crimson");
            AddStandardColor(Standard, Brushes.Red, "Red");
            AddStandardColor(Standard, Brushes.Firebrick, "Firebrick");
            AddStandardColor(Standard, Brushes.DarkRed, "DarkRed");
            AddStandardColor(Standard, Brushes.LightYellow, "LightYellow");
            AddStandardColor(Standard, Brushes.LemonChiffon, "LemonChiffon");
            AddStandardColor(Standard, Brushes.LightGoldenrodYellow, "LightGoldenrodYellow");
            AddStandardColor(Standard, Brushes.PapayaWhip, "PapayaWhip");
            AddStandardColor(Standard, Brushes.Moccasin, "Moccasin");
            AddStandardColor(Standard, Brushes.PeachPuff, "PeachPuff");
            AddStandardColor(Standard, Brushes.PaleGoldenrod, "PaleGoldenrod");
            AddStandardColor(Standard, Brushes.Khaki, "Khaki");
            AddStandardColor(Standard, Brushes.DarkKhaki, "DarkKhaki");
            AddStandardColor(Standard, Brushes.Gold, "Gold");
            AddStandardColor(Standard, Brushes.Yellow, "Yellow");
            AddStandardColor(Standard, Brushes.Orange, "Orange");
            AddStandardColor(Standard, Brushes.DarkOrange, "DarkOrange");
            AddStandardColor(Standard, Brushes.Coral, "Coral");
            AddStandardColor(Standard, Brushes.Tomato, "Tomato");
            AddStandardColor(Standard, Brushes.OrangeRed, "OrangeRed");
            AddStandardColor(Standard, Brushes.PaleGreen, "PaleGreen");
            AddStandardColor(Standard, Brushes.LightGreen, "LightGreen");
            AddStandardColor(Standard, Brushes.YellowGreen, "YellowGreen");
            AddStandardColor(Standard, Brushes.GreenYellow, "GreenYellow");
            AddStandardColor(Standard, Brushes.Chartreuse, "Chartreuse");
            AddStandardColor(Standard, Brushes.LawnGreen, "LawnGreen");
            AddStandardColor(Standard, Brushes.Lime, "Lime");
            AddStandardColor(Standard, Brushes.LimeGreen, "LimeGreen");
            AddStandardColor(Standard, Brushes.MediumSpringGreen, "MediumSpringGreen");
            AddStandardColor(Standard, Brushes.SpringGreen, "SpringGreen");
            AddStandardColor(Standard, Brushes.MediumAquamarine, "MediumAquamarine");
            AddStandardColor(Standard, Brushes.Aquamarine, "Aquamarine");
            AddStandardColor(Standard, Brushes.LightSeaGreen, "LightSeaGreen");
            AddStandardColor(Standard, Brushes.MediumSeaGreen, "MediumSeaGreen");
            AddStandardColor(Standard, Brushes.SeaGreen, "SeaGreen");
            AddStandardColor(Standard, Brushes.DarkSeaGreen, "DarkSeaGreen");
            AddStandardColor(Standard, Brushes.ForestGreen, "ForestGreen");
            AddStandardColor(Standard, Brushes.Green, "Green");
            AddStandardColor(Standard, Brushes.DarkGreen, "DarkGreen");
            AddStandardColor(Standard, Brushes.OliveDrab, "OliveDrab");
            AddStandardColor(Standard, Brushes.Olive, "Olive");
            AddStandardColor(Standard, Brushes.DarkOliveGreen, "DarkOliveGreen");
            AddStandardColor(Standard, Brushes.Teal, "Teal");
            AddStandardColor(Standard, Brushes.LightBlue, "LightBlue");
            AddStandardColor(Standard, Brushes.PowderBlue, "PowderBlue");
            AddStandardColor(Standard, Brushes.PaleTurquoise, "PaleTurquoise");
            AddStandardColor(Standard, Brushes.Turquoise, "Turquoise");
            AddStandardColor(Standard, Brushes.MediumTurquoise, "MediumTurquoise");
            AddStandardColor(Standard, Brushes.DarkTurquoise, "DarkTurquoise");
            AddStandardColor(Standard, Brushes.Cyan, "Aqua/Cyan");
            AddStandardColor(Standard, Brushes.DarkCyan, "DarkCyan");
            AddStandardColor(Standard, Brushes.CadetBlue, "CadetBlue");
            AddStandardColor(Standard, Brushes.LightSteelBlue, "LightSteelBlue");
            AddStandardColor(Standard, Brushes.SteelBlue, "SteelBlue");
            AddStandardColor(Standard, Brushes.LightSkyBlue, "LightSkyBlue");
            AddStandardColor(Standard, Brushes.SkyBlue, "SkyBlue");
            AddStandardColor(Standard, Brushes.DeepSkyBlue, "DeepSkyBlue");
            AddStandardColor(Standard, Brushes.DodgerBlue, "DodgerBlue");
            AddStandardColor(Standard, Brushes.CornflowerBlue, "CornflowerBlue");
            AddStandardColor(Standard, Brushes.RoyalBlue, "RoyalBlue");
            AddStandardColor(Standard, Brushes.Blue, "Blue", Brushes.White);
            AddStandardColor(Standard, Brushes.MediumBlue, "MediumBlue", Brushes.White);
            AddStandardColor(Standard, Brushes.DarkBlue, "DarkBlue", Brushes.White);
            AddStandardColor(Standard, Brushes.Navy, "Navy", Brushes.White);
            AddStandardColor(Standard, Brushes.MidnightBlue, "MidnightBlue", Brushes.White);
            AddStandardColor(Standard, Brushes.Lavender, "Lavender");
            AddStandardColor(Standard, Brushes.Thistle, "Thistle");
            AddStandardColor(Standard, Brushes.Plum, "Plum");
            AddStandardColor(Standard, Brushes.Violet, "Violet");
            AddStandardColor(Standard, Brushes.Orchid, "Orchid");
            AddStandardColor(Standard, Brushes.Magenta, "Fuchsia/Magenta");
            AddStandardColor(Standard, Brushes.MediumOrchid, "MediumOrchid");
            AddStandardColor(Standard, Brushes.MediumPurple, "MediumPurple");
            AddStandardColor(Standard, Brushes.BlueViolet, "BlueViolet");
            AddStandardColor(Standard, Brushes.DarkViolet, "DarkViolet");
            AddStandardColor(Standard, Brushes.DarkOrchid, "DarkOrchid");
            AddStandardColor(Standard, Brushes.Purple, "Purple", Brushes.White);
            AddStandardColor(Standard, Brushes.DarkMagenta, "DarkMagenta", Brushes.White);
            AddStandardColor(Standard, Brushes.SlateBlue, "SlateBlue");
            AddStandardColor(Standard, Brushes.DarkSlateBlue, "DarkSlateBlue");
            AddStandardColor(Standard, Brushes.MediumSlateBlue, "MediumSlateBlue");
            AddStandardColor(Standard, Brushes.Indigo, "Indigo", Brushes.White);
            AddStandardColor(Standard, Brushes.Pink, "Pink");
            AddStandardColor(Standard, Brushes.LightPink, "LightPink");
            AddStandardColor(Standard, Brushes.HotPink, "HotPink");
            AddStandardColor(Standard, Brushes.DeepPink, "DeepPink");
            AddStandardColor(Standard, Brushes.PaleVioletRed, "PaleVioletRed");
            AddStandardColor(Standard, Brushes.MediumVioletRed, "MediumVioletRed");
            AddStandardColor(Standard, Brushes.Transparent, "Transparent", Brushes.White);
            #endregion
        }
        public double CornerRadius { set => DC.CR = value; }

        public SolidColorBrush Brush => DC.LastBrush;
        public Color Color => DC.LastBrush.Color;

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            e.Handled = true;
            Picker.VerticalOffset += e.VerticalChange;
            Picker.HorizontalOffset += e.HorizontalChange;
        }

        private void AddChannelBinding(Control c, CChannels channel, 
            UpdateSourceTrigger st = UpdateSourceTrigger.Default)
        {
            var dc = DataContext as ColorPickerDataContext;
            (CChannels, Func<SolidColorBrush>) pm = (channel, () => dc.NewBrush);
            var b = new Binding("NewBrush")
            {
                Converter = new BrushToChannelConverter(),
                ConverterParameter = pm,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = st
            };

            if (c is Slider) c.SetBinding(Slider.ValueProperty, b);
            if (c is TextBox) c.SetBinding(TextBox.TextProperty, b);
        }

        private class ColorPickerDataContext : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            public void OnPropertyChanged([CallerMemberName]string prop = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            }

            public void Initialize(SolidColorBrush br, Action A)
            {
                LastBrush = br;
                nbr = br;

                this.A = A;
            }

            public double CR { get; set; }

            private Action A;

            public SolidColorBrush LastBrush { get; set; }

            private int ChangesCounter = 0;
            private SolidColorBrush nbr;
            public SolidColorBrush NewBrush 
            {
                get 
                { 
                    return nbr; 
                }
                set 
                {
                    nbr = value;
                    OnPropertyChanged();

                    Task.Run(() => 
                    {
                        ChangesCounter += 1;
                        var x = ChangesCounter;
                        Thread.Sleep(100);
                        if (x != ChangesCounter) return;
                        A?.Invoke();
                    });
                }
            }

            public SolidColorBrush MiddleSliderBrush { get; set; }
            public SolidColorBrush BottomSliderBrush { get; set; }
        }

        private void AddStandardColor(WrapPanel Wp, SolidColorBrush brush, string Brush, SolidColorBrush Foreground = null)
        {
            if (Foreground == null) Foreground = Brushes.Black;
            var lb = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Content = Brush,
                Width = 108,
                Height = 24,
                Background = brush,
                Foreground = Foreground,
                FontSize = 10
            };
            lb.MouseDown += GetBackground;
            Wp.Children.Add(lb);
        }

        #region BottomSlider
        private double BottomSliderLastPoint;
        private double BottomSliderLastMargin;
        private void BottomSliderMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            BottomSliderLastPoint = e.GetPosition(this).X;
            BottomSliderLastMargin = BottomSlider.Margin.Left;
            SlidersLock = true;
            this.MouseLeftButtonUp += (obj, e) =>
            {
                this.MouseMove -= MoveBottomSlider;
                SlidersLock = false;
            };
            this.MouseMove += MoveBottomSlider;
        }
        private void BottomGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            var x = e.GetPosition(BottomGrid).X - 10;
            if (x > 519) x = 519;
            if (x < 0) x = 0;
            SlidersLock = true;
            SetBottomSlider(x);

            BottomSliderLastPoint = e.GetPosition(this).X;
            BottomSliderLastMargin = BottomSlider.Margin.Left;
            
            this.MouseLeftButtonUp += (obj, e) =>
            {
                this.MouseMove -= MoveBottomSlider;
                SlidersLock = false;
            };
            this.MouseMove += MoveBottomSlider;
        }
        private void MoveBottomSlider(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            if (e.LeftButton == MouseButtonState.Released)
            {
                this.MouseMove -= MoveBottomSlider; return;
            }

            var D = e.GetPosition(this).X - BottomSliderLastPoint;

            var x = BottomSliderLastMargin + D;
            if (x > 519) x = 519;
            if (x < 0) x = 0;
            SetBottomSlider(x);
        }
        #endregion
        #region MiddleSlider
        private Point MiddleSliderLastPoint;
        private double MiddleSliderLastLeft;
        private double MiddleSliderLastTop;
        private void MiddleSliderMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            MiddleSliderLastPoint = e.GetPosition(this);
            MiddleSliderLastLeft = MiddleSlider.Margin.Left;
            MiddleSliderLastTop = MiddleSlider.Margin.Top;
            BottomLock = true;
            this.MouseLeftButtonUp += (obj, e) =>
            {
                this.MouseMove -= MoveMiddleSlider;
                BottomLock = false;
            };
            this.MouseMove += MoveMiddleSlider;
        }
        private void MiddleGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            var left = e.GetPosition(MiddleGrid).X;
            var top = e.GetPosition(MiddleGrid).Y;
            if (left < 0) left = 0;
            if (top < 0) top = 0;
            if (left > 539) left = 539;
            if (top > 388) top = 388;
            BottomLock = true;
            SetMiddleSlider(left, top);

            MiddleSliderLastPoint = e.GetPosition(this);
            MiddleSliderLastLeft = MiddleSlider.Margin.Left;
            MiddleSliderLastTop = MiddleSlider.Margin.Top;
            this.MouseLeftButtonUp += (obj, e) =>
            {
                this.MouseMove -= MoveMiddleSlider;
                BottomLock = false;
            };
            this.MouseMove += MoveMiddleSlider;
        }
        private void MoveMiddleSlider(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            if (e.LeftButton == MouseButtonState.Released)
            {
                this.MouseMove -= MoveMiddleSlider; return;
            }

            var Dleft = e.GetPosition(this).X - MiddleSliderLastPoint.X;
            var DTop = e.GetPosition(this).Y - MiddleSliderLastPoint.Y;

            var left = MiddleSliderLastLeft + Dleft;
            var top = MiddleSliderLastTop + DTop;
            if (left < 0) left = 0;
            if (top < 0) top = 0;
            if (left > 539) left = 539;
            if (top > 388) top = 388;
            SetMiddleSlider(left, top);
            
        }
        #endregion

        private void DC_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "NewBrush":
                    {
                        NewBrushChanged(DC.NewBrush);
                    }
                    break;
            }
        }
        private void NewBrushChanged(SolidColorBrush nbr)
        {
            if (!SlidersLock)
            {
                var r = (double)nbr.Color.R;
                var g = (double)nbr.Color.G;
                var b = (double)nbr.Color.B;
                double x = 0; double left = 0; double top = 0;

                if (r >= g && r >= b)
                {
                    top = (1 - r / 255) * 388;
                    if (g > b)
                    {
                        x = 86.5 * (g / r);
                        left = (1 - b / r) * 539;
                    }
                    else
                    {
                        x = 519 - 86.5 * (b / r);
                        left = (1 - g / r) * 539;
                    }
                }
                else if (g >= r && g >= b)
                {
                    top = (1 - g / 255) * 388;
                    if (r > b)
                    {
                        x = 173 - 86.5 * (r / g);
                        left = (1 - b / g) * 539;
                    }
                    else
                    {
                        x = 173 + 86.5 * (b / g);
                        left = (1 - r / g) * 539;
                    }
                }
                else if (b >= r && b >= g)
                {
                    top = (1 - b / 255) * 388;
                    if (r > g)
                    {
                        x = 346 + 86.5 * (r / b);
                        left = (1 - g / b) * 539;
                    }
                    else
                    {
                        x = 346 - 86.5 * (g / b);
                        left = (1 - r / b) * 539;
                    }
                }

                SetBottomSlider(x, true);
                SetMiddleSlider(left, top, true);

                var br = new SolidColorBrush(Color.FromRgb(
                    nbr.Color.R, nbr.Color.G, nbr.Color.B));
                br.Freeze();
                DC.MiddleSliderBrush = br;
            }
        }

        // left 539
        // top 388
        private bool BottomLock = false;
        private bool SlidersLock = false;
        private void SetBottomSlider(double x, bool NewBrush = false)
        {
            if (BottomLock) return;

            byte R = 0; byte G = 0; byte B = 0;
            if (x <= 86.5)
            {
                R = 255; 
                G = Convert.ToByte(255 * x / 86.5); 
                B = 0;
            }
            if (x > 86.5 && x <= 173)
            {
                var y = x - 86.5;
                R = Convert.ToByte(255 * (1 - y / 86.5));
                G = 255;
                B = 0;
            }

            if (x > 173 && x <= 259.5)
            {
                var y = x - 173;
                R = 0;
                G = 255;
                B = Convert.ToByte(255 * y / 86.5);
            }
            if (x > 259.5 && x <= 346)
            {
                var y = x - 259.5;
                R = 0;
                G = Convert.ToByte(255 * (1 - y / 86.5));
                B = 255;
            }
            if (x > 346 && x <= 432.5)
            {
                var y = x - 346;
                R = Convert.ToByte(255 * (y / 86.5));
                G = 0;
                B = 255;
            }
            if (x > 432.5 && x <= 519)
            {
                var y = x - 432.5;
                R = 255;
                G = 0;
                B = Convert.ToByte(255 * (1 - y / 86.5));
            }
            var br = new SolidColorBrush(Color.FromRgb(R, G, B)); br.Freeze();
            DC.BottomSliderBrush = br;

            try
            {
                BottomSlider.Margin = new Thickness(x, 0, 0, 0);
            }
            catch { }
            if (!NewBrush) SetBrush();
        }
        private void SetMiddleSlider(double left, double top, bool NewBrush = false)
        {
            try
            {
                MiddleSlider.Margin = new Thickness(left, top, 0, 0);
            }
            catch { }
            if (!NewBrush) SetBrush();
        }
        private void SetBrush()
        {
            var r = (float)DC.BottomSliderBrush.Color.R;
            var g = (float)DC.BottomSliderBrush.Color.G;
            var b = (float)DC.BottomSliderBrush.Color.B;
            var top = MiddleSlider.Margin.Top;
            var left = MiddleSlider.Margin.Left;

            byte A = DC.NewBrush.Color.A;

            var T = 1 - ((float)top / 388);
            var L = 1 - ((float)left / 539);

            if (r >= g && r >= b)
            {
                g += L * (r - g);
                b += L * (r - b);
            }
            else if (g >= r && g >= b)
            {
                r += L * (g - r);
                b += L * (g - b);
            }
            else if (b >= r && b >= g)
            {
                r += L * (b - r);
                g += L * (b - g);
            }
            byte R = Convert.ToByte(r * T);
            byte G = Convert.ToByte(g * T);
            byte B = Convert.ToByte(b * T);
            var br = new SolidColorBrush(Color.FromArgb(A, R, G, B)); br.Freeze();
            DC.NewBrush = br;
            br = new SolidColorBrush(Color.FromRgb(R, G, B)); br.Freeze();
            DC.MiddleSliderBrush = br;
        }

        private void GetBackground(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (sender is Grid)
            {
                var gr = (sender as Grid);
                DC.NewBrush = gr.Background as SolidColorBrush;
            }
            else
            {
                var gr = (sender as Label);
                DC.NewBrush = gr.Background as SolidColorBrush;
            }
        }

        private void ShowPicker(object sender, MouseButtonEventArgs e)
        {
            if(Parent is Sliding) (Parent as Sliding).Freeze = true;
            e.Handled = true;
            DC.NewBrush = DC.LastBrush;
            Picker.IsOpen = true;
        }
        private void Apply(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                if (Parent is Sliding) (Parent as Sliding).Freeze = false;
                Picker.IsOpen = false;
            }
            Task.Run(() => BrushChanged.Invoke(DC.NewBrush));
        }
        private void Cancel(object sender, RoutedEventArgs e)
        {
            if (Parent is Sliding) (Parent as Sliding).Freeze = false;
            DC.NewBrush = DC.LastBrush;
            e.Handled = true;
            Picker.IsOpen = false;
        }
    }
}