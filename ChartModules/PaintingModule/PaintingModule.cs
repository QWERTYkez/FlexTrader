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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace ChartModules.PaintingModule
{
    public class PaintingModule : ChartModule
    {
        private readonly IDrawingCanvas ElementsCanvas;
        private readonly IDrawingCanvas PricesCanvas;
        private readonly IDrawingCanvas TimesCanvas;
        private readonly Action<string> ResetInstrument;
        public PaintingModule(IChart chart, IDrawingCanvas ElementsCanvas, IDrawingCanvas PricesCanvas, 
            IDrawingCanvas TimesCanvas, Action<string> ResetInstrument) : base(chart)
        {
            this.ElementsCanvas = ElementsCanvas;   ElementsCanvas.AddVisual(ElementsVisual);
            this.PricesCanvas = PricesCanvas;       PricesCanvas.AddVisual(PricesVisual);
            this.TimesCanvas = TimesCanvas;         TimesCanvas.AddVisual(TimesVisual);

            Chart.VerticalСhanges += () => Task.Run(() => ResetHooks());
            Chart.VerticalСhanges += () => Redraw();
            Chart.HorizontalСhanges += () => Redraw();
            this.ResetInstrument = ResetInstrument;

            SetsName = "Paintings";

            ////////////////////
            AddElement(new Level(208.99, Brushes.White, Brushes.Black, Brushes.Yellow, 5, 5, 2));
            AddElement(new Level(206.95, Brushes.Black, Brushes.Azure, Brushes.Azure, 4, 6, 3));
            AddElement(new Level(204.90, Brushes.Lime, Brushes.Black, Brushes.Lime, 3, 7, 4));
        }

        private readonly DrawingVisual ElementsVisual = new DrawingVisual();
        private readonly DrawingVisual PricesVisual = new DrawingVisual();
        private readonly DrawingVisual TimesVisual = new DrawingVisual();
        private protected override void Destroy()
        {
            ElementsCanvas.ClearVisuals();
            PricesCanvas.ClearVisuals();
            TimesCanvas.ClearVisuals();
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
            el.ApplyChange = CollectionChanged;
            el.Chart = Chart;
            el.Delete = DeleteElement;
            ElementsCollection.Add(el);
            CollectionChanged();
        }
        private void DeleteElement(ChangingElement el)
        {
            ElementsCollection.Remove(el);
            CollectionChanged();
        }

        public void PaintingLevel(MouseButtonEventArgs e) 
        {
            AddElement(new Level(Chart.HeightToPrice(Chart.CurrentCursorPosition.Y),
                Brushes.White, Brushes.Black, Brushes.Lime, 3));

            if (!Chart.Controlled)
                ResetInstrument.Invoke(null);
            else
                Chart.ControlUsed = true;
            ResetHooks();
        }
        public void PaintingTrend(MouseButtonEventArgs e)
        {


            
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
                List<Action<DrawingContext>[]> lacts = new List<Action<DrawingContext>[]>();

                try
                {
                    foreach (var el in ElementsCollection)
                        lacts.Add(el.PrepareToDrawing(null, ppd));
                }
                catch (InvalidOperationException e) { return; }
                
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
