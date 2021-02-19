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

using ChartsCore.Core.Exchanges;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace ChartsCore.Core
{
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public void Inicialize()
        {
            ChartBackground = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            TickSize = 0.01;
            BaseFontSize = 18;
            FontBrush = Brushes.White;
            LinesThickness = 1;
            LinesBrush = Brushes.DarkGray;

            Binance ex = new Binance();
            //ex.GeneralInfo();

            NewCandles = ex.GetCandles("ETH", "USDT", CandleIntervalKey.m15).Select(c => c as ICandle).ToList();
        }

        public double TickSize { get; set; }
        public List<ICandle> NewCandles { get; set; }

        public SolidColorBrush ChartBackground { get; set; }
        public double BaseFontSize { get; set; }
        public Brush FontBrush { get; set; }

        public double LinesThickness { get; set; }
        public Brush LinesBrush { get; set; }
        
        public double CursorThickness { get; set; }
        public Brush CursorBrush { get; set; }
    }
}
