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

using FlexTrader.MVVM.Views;
using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace FlexTrader
{
    public partial class App : Application
    {
        //статики приложения
        public static DateTime BaseDT { get; } =
            new DateTime(1970, 1, 1).
            AddHours(Convert.ToInt32((DateTime.Now - DateTime.UtcNow).TotalHours));

        public static DateTime MillisecondsToDateTime(long milliseconds) =>
            BaseDT.AddMilliseconds(milliseconds);
        public static long DateTimeToMilliseconds(DateTime dt) =>
            Convert.ToInt64((dt - BaseDT).TotalMilliseconds);

        protected override void OnStartup(StartupEventArgs e)
        {
            if (Process.GetProcesses().
                Where(p => p.ProcessName == "FlexTrader.exe")
                .Count() > 0) return;

            base.OnStartup(e);
            this.MainWindow = new MainView();
            MainWindow.Show();
        }
    }
}
