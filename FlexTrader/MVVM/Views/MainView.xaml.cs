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

using System.Windows;
using System.Windows.Controls;

namespace FlexTrader.MVVM.Views
{
    public partial class MainView : ChartWindow
    {
        public MainView()
        {
            InitializeComponent();

            ((ViewModels.MainViewModel)DataContext).Initialize(this);
        }

        /// <summary>
        /// Базовый контейнер
        /// </summary>
        public override Grid BaseGrid => this.BaseGRD;
    }
}