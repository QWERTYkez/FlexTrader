﻿/* 
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

using ChartsCore.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace ChartsCore
{
    class ChartVievModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public Dispatcher Dispatcher { get; internal set; }
        internal void Initialize(ChartWindow Window, ChartViev mainView)
        {
            Dispatcher = mainView.Dispatcher;
            Chart = new View(Window, mainView);
            Chart2 = new View(Window, mainView);
        }

        public View Chart { get; set; }
        public View Chart2 { get; set; }
    }
}
