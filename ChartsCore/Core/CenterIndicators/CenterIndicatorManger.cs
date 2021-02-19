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

using ChartsCore.Core.CenterIndicators.Indicators;
using ChartsCore.Core.CenterIndicators.Paintings;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ChartsCore.Core.CenterIndicators
{
    public class CenterIndicatorManger
    {
        private View Chart;
        public CenterIndicatorManger(View Chart, DrawingCanvas BackgroundIndLayer, 
            DrawingCanvas ForegroundIndLayer, DrawingCanvas PricesCanvas, DrawingCanvas TimesCanvas)
        {
            this.Chart = Chart;
            this.BackgroundIndLayer = BackgroundIndLayer;
            this.ForegroundIndLayer = ForegroundIndLayer; 
            this.PricesCanvas = PricesCanvas;
            this.TimesCanvas = TimesCanvas;

            Chart.VerticalСhanges += Redraw;
            Chart.HorizontalСhanges += Redraw;
            Chart.VerticalСhanges += ResetHooks;
            Chart.HorizontalСhanges += ResetHooks;

            ///////////
            AddElement(new EMA(5, new Pen(Brushes.Gold, 3)));
            AddElement(new EMA(10, new Pen(Brushes.Gold, 3)));
            AddElement(new EMA(15, new Pen(Brushes.Gold, 3)));
        }
        private readonly DrawingCanvas BackgroundIndLayer;
        private readonly DrawingCanvas ForegroundIndLayer;
        private readonly DrawingCanvas PricesCanvas;
        private readonly DrawingCanvas TimesCanvas;

        private readonly List<HookElement> BackgroundIndicators = new List<HookElement>();
        private readonly List<HookElement> ForegroundIndicators = new List<HookElement>();
        public void AddElement(HookElement el)
        {
            el.SetChart(Chart);
            el.SetDeleteAction(DeleteElement);
            el.Moving += MoveIndicator;
            ForegroundIndicators.Add(el);
            ForegroundIndLayer.AddVisual(el.IndicatorVisual);
            
            if (el is Painting)
            {
                PricesCanvas.AddVisual(el.PriceVisual);
                TimesCanvas.AddVisual(el.TimeVisual);
            }

            ResetHooks();
            el.Rendering();
        }
        private void DeleteElement(HookElement el)
        {
            if (BackgroundIndicators.Contains(el))
            {
                BackgroundIndicators.Remove(el);
                BackgroundIndLayer.RemoveVisual(el.IndicatorVisual);
            }
            if (ForegroundIndicators.Contains(el))
            {
                ForegroundIndicators.Remove(el);
                ForegroundIndLayer.RemoveVisual(el.IndicatorVisual);
            }
            ResetPricesTimes();
            ResetHooks();
        }
        private void ResetVisualsBackground()
        {
            BackgroundIndLayer.ClearVisuals();
            foreach (var ind in BackgroundIndicators)
                BackgroundIndLayer.AddVisual(ind.IndicatorVisual);
            ResetPricesTimes();
        }
        private void ResetVisualsForeground()
        {
            ForegroundIndLayer.ClearVisuals();
            foreach (var ind in ForegroundIndicators)
                ForegroundIndLayer.AddVisual(ind.IndicatorVisual);
            ResetPricesTimes();
        }
        private void ResetVisuals()
        {
            BackgroundIndLayer.ClearVisuals();
            ForegroundIndLayer.ClearVisuals();
            foreach (var ind in BackgroundIndicators)
                BackgroundIndLayer.AddVisual(ind.IndicatorVisual);
            foreach (var ind in ForegroundIndicators)
                ForegroundIndLayer.AddVisual(ind.IndicatorVisual);
            ResetPricesTimes();
        }
        private void ResetPricesTimes()
        {
            PricesCanvas.ClearVisuals(); 
            TimesCanvas.ClearVisuals();
            foreach (var ind in BackgroundIndicators)
            {
                PricesCanvas.AddVisual(ind.PriceVisual);
                TimesCanvas.AddVisual(ind.TimeVisual);
            }
            foreach (var ind in ForegroundIndicators)
            {
                PricesCanvas.AddVisual(ind.PriceVisual);
                TimesCanvas.AddVisual(ind.TimeVisual);
            }
        }
        private void MoveIndicator(HookElement element, int i)
        {
            if (ForegroundIndicators.Contains(element))
            {
                switch (i)
                {
                    case 2:
                        {
                            i = ForegroundIndicators.IndexOf(element);
                            if (i == ForegroundIndicators.Count - 1) return;
                            ForegroundIndicators.Remove(element);
                            ForegroundIndicators.Add(element);
                            ResetVisualsForeground();
                        }
                        break;
                    case 1:
                        {
                            i = ForegroundIndicators.IndexOf(element);
                            if (i == ForegroundIndicators.Count - 1) return;
                            ForegroundIndicators.Remove(element);
                            ForegroundIndicators.Insert(i + 1, element);
                            ResetVisualsForeground();
                        }
                        break;
                    case -1:
                        {
                            i = ForegroundIndicators.IndexOf(element);
                            if (i == 0)
                            {
                                ForegroundIndicators.Remove(element);
                                BackgroundIndicators.Add(element);
                                ResetVisuals();
                            }
                            else
                            {
                                ForegroundIndicators.Remove(element);
                                ForegroundIndicators.Insert(i - 1, element);
                                ResetVisualsForeground();
                            }
                        }
                        break;
                    case -2:
                        {
                            ForegroundIndicators.Remove(element);
                            BackgroundIndicators.Insert(0, element);
                            ResetVisuals();
                        }
                        break;
                }
            }
            else
            {
                switch (i)
                {
                    case 2:
                        {
                            BackgroundIndicators.Remove(element);
                            ForegroundIndicators.Add(element);
                            ResetVisuals();
                        }
                        break;
                    case 1:
                        {
                            i = BackgroundIndicators.IndexOf(element);
                            if (i == BackgroundIndicators.Count - 1)
                            {
                                BackgroundIndicators.Remove(element);
                                ForegroundIndicators.Insert(0, element);
                                ResetVisuals();
                            }
                            else
                            {
                                BackgroundIndicators.Remove(element);
                                BackgroundIndicators.Insert(i + 1, element);
                                ResetVisualsBackground();
                            }
                        }
                        break;
                    case -1:
                        {
                            i = BackgroundIndicators.IndexOf(element);
                            if (i == 0) return;
                            BackgroundIndicators.Remove(element);
                            BackgroundIndicators.Insert(i - 1, element);
                            ResetVisualsBackground();
                        }
                        break;
                    case -2:
                        {
                            i = BackgroundIndicators.IndexOf(element);
                            if (i == 0) return;
                            BackgroundIndicators.Remove(element);
                            BackgroundIndicators.Insert(0, element);
                            ResetVisualsBackground();
                        }
                        break;
                }
            }
        }

        private int ChangesCounter1 = 0;
        private void ResetHooks()
        {
            Task.Run(() => 
            {
                ChangesCounter1 += 1;
                var x = ChangesCounter1;
                Thread.Sleep(50);
                if (x != ChangesCounter1) return;
                VisibleHooks = (from el in BackgroundIndicators.AsParallel() where el.VisibilityOnChart select el.Hook).ToList();
                VisibleHooks.AddRange(from el in ForegroundIndicators.AsParallel() where el.VisibilityOnChart select el.Hook);
            });
        }

        private int ChangesCounter = 0;
        private object CCkey = new object();
        private void Redraw()
        {
            Task.Run(() =>
            {
                ChangesCounter += 1;
                var x = ChangesCounter;
                lock (CCkey)
                {
                    if (x != ChangesCounter) return;

                    foreach (var ind in BackgroundIndicators) ind.Rendering();
                    foreach (var ind in ForegroundIndicators) ind.Rendering();
                }
            });
        }

        public List<Hook> VisibleHooks { get; private set; }
    }
}
