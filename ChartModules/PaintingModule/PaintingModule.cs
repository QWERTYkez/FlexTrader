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

using ChartModules.PaintingModule.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ChartModules.PaintingModule
{
    public class PaintingModule : ChartModule
    {
        private readonly IDrawingCanvas ElementsCanvas;
        private readonly IDrawingCanvas PricesCanvas;
        private readonly IDrawingCanvas TimesCanvas;
        private readonly IDrawingCanvas PrototypeCanvas;
        private readonly IDrawingCanvas PrototypePriceCanvas;
        private readonly IDrawingCanvas PrototypeTimeCanvas;
        private readonly Action<string> ResetInstrument;
        public PaintingModule(IChart chart, IDrawingCanvas ElementsCanvas, IDrawingCanvas PricesCanvas,
            IDrawingCanvas TimesCanvas, IDrawingCanvas PrototypeCanvas, IDrawingCanvas PrototPCanvas,
            IDrawingCanvas PrototTCanvas) : base(chart)
        {
            this.ElementsCanvas = ElementsCanvas;   ElementsCanvas.AddVisual(ElementsVisual);
            this.PricesCanvas = PricesCanvas;       PricesCanvas.AddVisual(PricesVisual);
            this.TimesCanvas = TimesCanvas;         TimesCanvas.AddVisual(TimesVisual);
            this.PrototypeCanvas = PrototypeCanvas;     PrototypeCanvas.AddVisual(PrototypeVisual);
            this.PrototypePriceCanvas = PrototPCanvas;  PrototPCanvas.AddVisual(PrototypePriceVisual);
            this.PrototypeTimeCanvas = PrototTCanvas;   PrototTCanvas.AddVisual(PrototypeTimeVisual);

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
            Chart.ChartGrid.MouseMove += (s, e) => DrawPrototype?.Invoke(Chart,
                PrototypeVisual, PrototypePriceVisual, PrototypeTimeVisual);
            this.SetMenuAct = chart.MWindow.SetMenu;

            Chart.VerticalСhanges += () => Task.Run(() => ResetHooks());
            Chart.VerticalСhanges += () => Redraw();
            Chart.HorizontalСhanges += () => Task.Run(() => ResetHooks());
            Chart.HorizontalСhanges += () => Redraw();
            this.ResetInstrument = Chart.MWindow.ResetInstrument;

            SetsName = "Paintings";
            Chart.PaintingLevel = PaintingLevel;

            ////////////////////
            Task.Run(() => 
            {
                AddElement(new Level(208.99, Brushes.White, Brushes.Black, Brushes.Yellow, 5, 5, 2));
                AddElement(new Level(206.95, Brushes.Black, Brushes.Azure, Brushes.Azure, 4, 6, 3));
                AddElement(new Level(204.90, Brushes.Lime, Brushes.Black, Brushes.Lime, 3, 7, 4));
            });
        }

        private Action<IChart, DrawingVisual, DrawingVisual, DrawingVisual> DrawPrototype;
        private readonly Action<string, List<Setting>, IChart, Action, Action> SetMenuAct;
        private readonly DrawingVisual ElementsVisual = new DrawingVisual();
        private readonly DrawingVisual PricesVisual = new DrawingVisual();
        private readonly DrawingVisual TimesVisual = new DrawingVisual();
        private readonly DrawingVisual PrototypeVisual = new DrawingVisual();
        private readonly DrawingVisual PrototypePriceVisual = new DrawingVisual();
        private readonly DrawingVisual PrototypeTimeVisual = new DrawingVisual();
        private protected override void Destroy()
        {
            ElementsCanvas.ClearVisuals();
            PricesCanvas.ClearVisuals();
            TimesCanvas.ClearVisuals();
            PrototypeCanvas.ClearVisuals();
            PrototypePriceCanvas.ClearVisuals();
            PrototypeTimeCanvas.ClearVisuals();
        }

        private readonly List<ChangingElement> ElementsCollection = new List<ChangingElement>();
        private void CollectionChanged()
        {
            Sets.Clear();
            if (ElementsCollection.Count > 99)
            {
                for (int i = 0; i < ElementsCollection.Count; i++)
                    Setting.SetsLevel(Sets, $"{i + 1:000}. {ElementsCollection[i].ElementName}",
                        ElementsCollection[i].GetSettings().ToArray());
            }
            else if (ElementsCollection.Count > 9)
            {
                for (int i = 0; i < ElementsCollection.Count; i++)
                    Setting.SetsLevel(Sets, $"{i + 1:00}. {ElementsCollection[i].ElementName}",
                        ElementsCollection[i].GetSettings().ToArray());
            }
            else
            {
                for (int i = 0; i < ElementsCollection.Count; i++)
                    Setting.SetsLevel(Sets, $"{i + 1}. {ElementsCollection[i].ElementName}",
                        ElementsCollection[i].GetSettings().ToArray());
            }

            Redraw();
        }

        private void AddElement(ChangingElement el)
        {
            el.SetApplyChangeAction(CollectionChanged);
            el.Chart = Chart;
            el.SetDeleteAction(DeleteElement);
            ElementsCollection.Add(el);
            CollectionChanged();
            ResetHooks();
        }
        private void DeleteElement(ChangingElement el)
        {
            ElementsCollection.Remove(el);
            CollectionChanged();
            ResetHooks();
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

        public void PreparePaintingLevel()
        {
            DrawPrototype = Level.DrawPrototype;
            SetMenuAct.Invoke("New level", Level.StGetSets(), Chart, null, null);
        }
        public void PaintingLevel(MouseButtonEventArgs e) 
        {
            AddElement(new Level(Chart.HeightToPrice(Chart.CursorPosition.Magnet_Current.Y)));

            if (!Chart.MWindow.Controlled) ResetInstrument.Invoke(null);
            else Chart.MWindow.ControlUsed = true;
        }

        public void PreparePaintingTrend()
        {
            DrawPrototype = Trend.DrawFirstPoint;
            SetMenuAct.Invoke("New trend", Trend.StGetSets(), Chart, null, null);
            Chart.PaintingTrend = PaintingTrend;
        }
        public void PaintingTrend(MouseButtonEventArgs e)
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

        private void ResetHooks() 
        {
            VisibleHooks = (from el in ElementsCollection.AsParallel() where el.VisibilityOnChart select el.Hook).ToList();
        }

        private int ChangesCounter = 0;
        public override Task Redraw()
        {
            ChangesCounter += 1;
            var x = ChangesCounter;
            Thread.Sleep(50);
            if (x != ChangesCounter) return null;

            return Task.Run(() => 
            {
                var ppd = VisualTreeHelper.GetDpi(PricesVisual).PixelsPerDip;
                Action<DrawingContext>[][] lacts = new Action<DrawingContext>[ElementsCollection.Count][];

                try
                {
                    for (int i = 0; i < ElementsCollection.Count; i++)
                        lacts[i] = ElementsCollection[i].PrepareToDrawing(null, ppd);
                }
                catch (IndexOutOfRangeException) { return; }
                
                Dispatcher.Invoke(() =>
                {
                    using (var dc = ElementsVisual.RenderOpen())
                    {
                        foreach (var acts in lacts)
                        {
                            acts[0]?.Invoke(dc);
                        }
                    }
                    using (var dc = PricesVisual.RenderOpen())
                    {
                        foreach (var acts in lacts)
                        {
                            acts[1]?.Invoke(dc);
                        }
                    }
                    using (var dc = TimesVisual.RenderOpen())
                    {
                        foreach (var acts in lacts)
                        {
                            acts[2]?.Invoke(dc);
                        }
                    }
                });
            });
        }

        public List<Hook> VisibleHooks { get; private set; }
        
    }
}
