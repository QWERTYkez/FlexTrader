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
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace FlexTrader.MVVM.Views
{
    public class ChartWindow : Window
    {
        public event Action<Vector> Moving;
        public ChartWindow()
        {
            this.MouseLeave += (obj, e) => CanMoving = false;
            this.MouseLeftButtonUp += (obj, e) => CanMoving = false;
            this.MouseMove += (obj, e) =>
            {
                if (CanMoving)
                {
                    Moving.Invoke(e.GetPosition(this) - StartPosition);
                }
            };
        }

        private Point StartPosition;
        private bool CanMoving;
        internal void StartMoveChart(ChartView CV, MouseButtonEventArgs e)
        {
            StartPosition = e.GetPosition(this);
            CanMoving = true;
        }
    }
}
