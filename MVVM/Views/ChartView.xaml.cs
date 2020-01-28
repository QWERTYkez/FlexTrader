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

using FlexTrader.Exchanges;
using FlexTrader.MVVM.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlexTrader.MVVM.Views
{
    public partial class ChartView : UserControl
    {
        public ChartView()
        {
            InitializeComponent();
            var DContext = DataContext as ChartViewModel;
            DContext.PropertyChanged += DContext_PropertyChanged;
            {
                var Layers = new List<DrawingCanvas>();
                CandlesLayer = new DrawingCanvas(); Layers.Add(CandlesLayer);
                // add layer
                LayersControl.ItemsSource = Layers;
            }
            DContext.Inicialize();
        }

        private void DContext_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var DContext = sender as ChartViewModel;

            switch (e.PropertyName)
            {
                case "NewCandles":
                    {
                        if (DContext.NewCandles != null && DContext.NewCandles.Count > 0)
                            DrawNewCandles(DContext.NewCandles);
                    }
                    break;
            }
        }

        private double ChHeight => ChartGRD.ActualHeight;
        private double ChWidth => ChartGRD.ActualWidth;
        private double Delta;
        private double Min;
        private void ChartGRD_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Translate.Y = Min + Delta * 0.5 - ChHeight * 0.5;
            Scale.ScaleY = ChHeight / Delta;
        }
        private DateTime? StartTime;
        private void DrawNewCandles(IEnumerable<Candle> newCandles)
        {
            if (StartTime == null)
                StartTime = newCandles.Last().TimeStamp;

            Min = 145;
            double max = 345;
            double delta = max - Min;
            max += delta * 0.05;
            Min -= delta * 0.05;
            Delta = max - Min;

            Dispatcher.Invoke(() => 
            {
                var dvisual = new DrawingVisual();
                CandlesLayer.AddVisual(dvisual);
                CandlesLayer.Background = Brushes.Black;
                using (var dc = dvisual.RenderOpen())
                {
                    dc.DrawRectangle(Brushes.Lime, null, new Rect(new Point(5, 145), new Point(15, 345)));
                    dc.DrawRectangle(Brushes.Red, null, new Rect(new Point(5, 240), new Point(15, 250)));
                }
            });

            ChartGRD_SizeChanged(null, null);
        }
        
        private readonly DrawingCanvas CandlesLayer;

        
    }
}
