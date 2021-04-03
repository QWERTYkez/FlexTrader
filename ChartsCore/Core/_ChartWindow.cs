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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ChartsCore.Core
{
    public abstract class ChartWindow : Window
    {
        #region Обработка таскания мышью
        private Point StartPosition;
        private Action<Vector> ActA;
        private Action ActB;
        public void MoveElement(MouseButtonEventArgs e, Action<Vector> ActA, Action ActB = null)
        {
            e.Handled = true;
            StartPosition = e.GetPosition(this); this.ActA = ActA; this.ActB = ActB;

            this.MouseLeftButtonUp += ButtonUp;
            this.MouseMove += MovingAct;
        }
        private void ButtonUp(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            this.MouseLeftButtonUp -= ButtonUp;
            EndMoving();
        }
        private void MovingAct(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            if (e.LeftButton == MouseButtonState.Released) EndMoving();
            var vec = e.GetPosition(this) - StartPosition;
            ActA.Invoke(vec);
        }
        public void EndMoving(Dispatcher Dispatcher) => Dispatcher.Invoke(EndMoving);
        public void EndMoving()
        {
            this.MouseMove -= MovingAct;
            ActB?.Invoke();
        }
        private List<Func<Vector, Task>> ActsA;
        private List<Func<Task>> ActsB;
        public void MoveElements(MouseButtonEventArgs e, List<Func<Vector, Task>> ActsA, List<Func<Task>> ActsB = null)
        {
            e.Handled = true;
            StartPosition = e.GetPosition(this); this.ActsA = ActsA; this.ActsB = ActsB;

            this.MouseLeftButtonUp += ButtonUp2;
            this.MouseMove += MovingActs;
        }
        private void ButtonUp2(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            this.MouseLeftButtonUp -= ButtonUp2;
            EndMovings();
        }
        private void MovingActs(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            if (e.LeftButton == MouseButtonState.Released) EndMovings();
            var vec = e.GetPosition(this) - StartPosition;
            foreach (var act in ActsA) Task.Run(() => act.Invoke(vec));
        }
        public void EndMovings(Dispatcher Dispatcher) => Dispatcher.Invoke(EndMovings);
        public void EndMovings()
        {
            this.MouseMove -= MovingActs;

            if (ActsB != null) foreach (var act in ActsB) act.Invoke();
        }
        #endregion
    }
}
