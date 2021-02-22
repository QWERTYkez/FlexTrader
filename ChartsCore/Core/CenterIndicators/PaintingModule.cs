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

using ChartsCore.Core.CenterIndicators.Paintings;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ChartsCore.Core.CenterIndicators
{
    public class PaintingModule : ChartModule
    {
        private readonly DrawingCanvas PrototypeCanvas;
        private readonly DrawingCanvas PrototypePriceCanvas;
        private readonly DrawingCanvas PrototypeTimeCanvas;
        private readonly Action<string> ResetInstrument;
        public PaintingModule(View chart, DrawingCanvas PrototypeCanvas, DrawingCanvas PrototPCanvas,
            DrawingCanvas PrototTCanvas, Action<HookElement> AddElement) : base(chart)
        {
            this.PrototypeCanvas = PrototypeCanvas;     PrototypeCanvas.AddVisual(PrototypeVisual);
            this.PrototypePriceCanvas = PrototPCanvas;  PrototPCanvas.AddVisual(PrototypePriceVisual);
            this.PrototypeTimeCanvas = PrototTCanvas;   PrototTCanvas.AddVisual(PrototypeTimeVisual);

            this.AddElement = AddElement;

            Chart.ChartGrid.MouseEnter += (s, e) => 
            {
                PrototypeCanvas.Visibility = Visibility.Visible;
                PrototypePriceCanvas.Visibility = Visibility.Visible;
                PrototypeTimeCanvas.Visibility = Visibility.Visible;
            };
            Chart.ChartGrid.MouseLeave += (s, e) => 
            {
                PrototypeCanvas.Visibility = Visibility.Hidden;
                PrototypePriceCanvas.Visibility = Visibility.Hidden;
                PrototypeTimeCanvas.Visibility = Visibility.Hidden;
            };
            Chart.Shell.PrepareInstrument += PrepareInstrument;
            Chart.DrawPrototype = () => DrawPrototype?.Invoke(Chart,
                PrototypeVisual, PrototypePriceVisual, PrototypeTimeVisual);
            this.SetMenuAct = chart.Shell.SetMenu;

            this.ResetInstrument = Chart.Shell.ResetInstrument;

            Chart.PaintingLevel = PaintingLevel;

            ////////////////////
            Task.Run(() => 
            {
                Dispatcher.Invoke(() => 
                {
                    AddElement(new Level(208.99, Brushes.White, Brushes.Black, Brushes.Yellow, 5, 5, 2));
                    AddElement(new Level(206.95, Brushes.Black, Brushes.Azure, Brushes.Azure, 4, 6, 3));
                    AddElement(new Level(204.90, Brushes.Lime, Brushes.Black, Brushes.Lime, 3, 7, 4));
                });
            });
        }

        private Action<HookElement> AddElement;

        private protected override string SetsName => "Paintings";

        private Action<View, DrawingVisual, DrawingVisual, DrawingVisual> DrawPrototype;
        private readonly Action<string, List<Setting>, Action, Action> SetMenuAct;
        private readonly DrawingVisual PrototypeVisual = new DrawingVisual();
        private readonly DrawingVisual PrototypePriceVisual = new DrawingVisual();
        private readonly DrawingVisual PrototypeTimeVisual = new DrawingVisual();
        private protected override void Destroy()
        {
            PrototypeCanvas.ClearVisuals();
            PrototypePriceCanvas.ClearVisuals();
            PrototypeTimeCanvas.ClearVisuals();
        }

        public void ClearPrototype()
        {
            Task.Run(() => 
            {
                DrawPrototype = null;
                Dispatcher.Invoke(() =>
                {
                    PrototypeVisual.RenderOpen().Close();
                    PrototypePriceVisual.RenderOpen().Close();
                    PrototypeTimeVisual.RenderOpen().Close();
                });
            });
        }

        private void PrepareInstrument(PInstrument type)
        {
            Task.Run(() => 
            {
                switch (type)
                {
                    case PInstrument.Level:
                        {
                            DrawPrototype = Level.DrawPrototype;
                            SetMenuAct.Invoke("New level", Level.StGetSets(), null, null);
                        }
                        return;
                    case PInstrument.Trend:
                        {
                            DrawPrototype = Trend.DrawFirstPoint;
                            SetMenuAct.Invoke("New trend", Trend.StGetSets(), null, null);
                            Chart.PaintingTrend = PaintingTrend;
                        }
                        return;
                }
            });
        }
        private void PaintingLevel(MouseButtonEventArgs e) 
        {
            AddElement(new Level(Chart.HeightToPrice(Chart.CursorPosition.Magnet_Current.Y)));

            if (!Chart.Shell.Controlled) ResetInstrument.Invoke(null);
            else Chart.Shell.ControlUsed = true;
        }
        private void PaintingTrend(MouseButtonEventArgs e)
        {
            Chart.PaintingPoints = new List<Point> { Chart.CursorPosition.Magnet_Current };
            DrawPrototype = Trend.DrawSecondPoint;

            Chart.PaintingTrend = e => 
            {
                AddElement(new Trend(Chart.PaintingPoints[0].ToChartPoint(Chart), 
                    Chart.CursorPosition.Magnet_Current.ToChartPoint(Chart)));

                if (!Chart.Shell.Controlled) ResetInstrument.Invoke(null);
                else Chart.Shell.ControlUsed = true;
            };
        }

        public List<Hook> VisibleHooks { get; private set; }
    }

    public enum PInstrument
    {
        Level,
        Trend
    }
}
