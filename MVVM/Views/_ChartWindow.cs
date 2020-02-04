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
using System.Windows;
using System.Windows.Input;

namespace FlexTrader.MVVM.Views
{
    public class ChartWindow : Window
    {
        public event Action<Vector> Moving;
        public event Action<double> ScalingY;
        public event Action<double> ScalingX;
        private Point StartPosition;

        internal void StartMoveChart(MouseButtonEventArgs e)
        {
            StartPosition = e.GetPosition(this);
            this.MouseLeftButtonUp += (obj, e) => this.MouseMove -= MovingAct;
            this.MouseMove += MovingAct;
        }
        private void MovingAct(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released) this.MouseMove -= MovingAct;
            Moving.Invoke(e.GetPosition(this) - StartPosition);
        }

        internal void StartYScaling(MouseButtonEventArgs e)
        {
            StartPosition = e.GetPosition(this);
            this.MouseLeftButtonUp += (obj, e) => this.MouseMove -= ScalingYAct;
            this.MouseMove += ScalingYAct;
        }
        private void ScalingYAct(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released) this.MouseMove -= ScalingYAct;
            ScalingY.Invoke((e.GetPosition(this) - StartPosition).Y);
        }

        internal void StartXScaling(MouseButtonEventArgs e)
        {
            StartPosition = e.GetPosition(this);
            this.MouseLeftButtonUp += (obj, e) => this.MouseMove -= ScalingXAct;
            this.MouseMove += ScalingXAct;
        }
        private void ScalingXAct(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released) this.MouseMove -= ScalingXAct;
            ScalingX.Invoke((e.GetPosition(this) - StartPosition).X);
        }
    }
}
