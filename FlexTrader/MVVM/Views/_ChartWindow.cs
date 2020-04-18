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
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FlexTrader.MVVM.Views
{
    public abstract class ChartWindow : Window, IChartWindow
    {
        public ChartWindow()
        {
            this.Closing += (s, e) => SettingsWindow?.Close();
        }

        #region Обработка таскания мышью
        public event Action<Vector, int> Moving;
        private Point StartPosition;

        public void StartMoveCursor(MouseButtonEventArgs e, int t)
        {
            EventType = t;
            StartPosition = e.GetPosition(this);
            this.MouseLeftButtonUp += (obj, e) => this.MouseMove -= MovingAct;
            this.MouseMove += MovingAct;
        }
        private int EventType;
        private void MovingAct(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released) this.MouseMove -= MovingAct;
            Moving.Invoke(e.GetPosition(this) - StartPosition, EventType);
        }
        #endregion

        #region Блок окна настроек
        public abstract Grid BaseGrid { get; }

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
    }
}
