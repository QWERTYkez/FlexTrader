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

using ChartModules.CenterIndicators.Paintings;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ChartModules.CenterIndicators
{
    public class PaintingModule : ChartModule
    {
        private readonly DrawingCanvas PrototypeCanvas;
        private readonly DrawingCanvas PrototypePriceCanvas;
        private readonly DrawingCanvas PrototypeTimeCanvas;
        private readonly Action<string> ResetInstrument;
        public PaintingModule(IChart chart, DrawingCanvas PrototypeCanvas, DrawingCanvas PrototPCanvas,
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
            Chart.MWindow.PrepareInstrument += PrepareInstrument;
            Chart.DrawPrototype = () => DrawPrototype?.Invoke(Chart,
                PrototypeVisual, PrototypePriceVisual, PrototypeTimeVisual);
            this.SetMenuAct = chart.MWindow.SetMenu;

            this.ResetInstrument = Chart.MWindow.ResetInstrument;

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

        private Action<IChart, DrawingVisual, DrawingVisual, DrawingVisual> DrawPrototype;
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

        private void CollectionChanged()
        {
            //Sets.Clear();
            //if (ElementsCollection.Count > 99)
            //{
            //    for (int i = 0; i < ElementsCollection.Count; i++)
            //        Sets.AddLevel($"{i + 1:000}. {ElementsCollection[i].ElementName}",
            //            ElementsCollection[i].Sets.ToArray());
            //}
            //else if (ElementsCollection.Count > 9)
            //{
            //    for (int i = 0; i < ElementsCollection.Count; i++)
            //        Sets.AddLevel($"{i + 1:00}. {ElementsCollection[i].ElementName}",
            //            ElementsCollection[i].Sets.ToArray());
            //}
            //else
            //{
            //    for (int i = 0; i < ElementsCollection.Count; i++)
            //        Sets.AddLevel($"{i + 1}. {ElementsCollection[i].ElementName}",
            //            ElementsCollection[i].Sets.ToArray());
            //}

            //Redraw();
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

            if (!Chart.MWindow.Controlled) ResetInstrument.Invoke(null);
            else Chart.MWindow.ControlUsed = true;
        }
        private void PaintingTrend(MouseButtonEventArgs e)
        {
            Chart.PaintingPoints = new List<Point> { Chart.CursorPosition.Magnet_Current };
            DrawPrototype = Trend.DrawSecondPoint;

            Chart.PaintingTrend = e => 
            {
                AddElement(new Trend(Chart.PaintingPoints[0].ToChartPoint(Chart), 
                    Chart.CursorPosition.Magnet_Current.ToChartPoint(Chart)));

                if (!Chart.MWindow.Controlled) ResetInstrument.Invoke(null);
                else Chart.MWindow.ControlUsed = true;
            };
        }

        public List<Hook> VisibleHooks { get; private set; }
    }

    //public abstract class Painting : HookElement 
    //{
    //    public Painting()
    //    {
    //        Sets.Add(new Setting(() => this.Locked, b => this.Locked = b));
    //    }

    //    public override List<(string Name, Action Act)> GetContextMenu()
    //    {
    //        (string Name, Action Act) Lock;
    //        if (Locked) Lock = ("Unlock", () => Locked = !Locked);
    //        else Lock = ("Lock", () => Locked = !Locked);

    //        return new List<(string Name, Action Act)>
    //        {
    //            ("+++", null),
    //            Lock,
    //            ("Delete", Delete)
    //        };
    //    }
    //} 

    public enum PInstrument
    {
        Level,
        Trend
    }
}
