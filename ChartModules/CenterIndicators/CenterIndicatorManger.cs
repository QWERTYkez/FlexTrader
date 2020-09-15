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

using ChartModules.CenterIndicators.Indicators;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ChartModules.CenterIndicators
{
    public class CenterIndicatorManger
    {
        private IChart Chart;
        public CenterIndicatorManger(IChart Chart, DrawingCanvas BackgroundIndLayer, DrawingCanvas ForegroundIndLayer)
        {
            this.Chart = Chart;
            this.BackgroundIndLayer = BackgroundIndLayer;
            this.ForegroundIndLayer = ForegroundIndLayer;

            Chart.VerticalСhanges += Redraw;
            Chart.HorizontalСhanges += Redraw;
            Chart.VerticalСhanges += ResetHooks;
            Chart.HorizontalСhanges += ResetHooks;

            ///////////
            AddElement(new MA());
        }
        private DrawingCanvas BackgroundIndLayer;
        private DrawingCanvas ForegroundIndLayer;


        private readonly List<CenterIndicator> BackgroundIndicators = new List<CenterIndicator>();
        private readonly List<CenterIndicator> ForegroundIndicators = new List<CenterIndicator>();
        private void CollectionChanged()
        {
            //Sets.Clear();
            //if (ElementsCollection.Count > 99)
            //{
            //    for (int i = 0; i < ElementsCollection.Count; i++)
            //        Sets.AddLevel($"{i + 1:000}. {ElementsCollection[i].ElementName}",
            //            ElementsCollection[i].GetSettings().ToArray());
            //}
            //else if (ElementsCollection.Count > 9)
            //{
            //    for (int i = 0; i < ElementsCollection.Count; i++)
            //        Sets.AddLevel($"{i + 1:00}. {ElementsCollection[i].ElementName}",
            //            ElementsCollection[i].GetSettings().ToArray());
            //}
            //else
            //{
            //    for (int i = 0; i < ElementsCollection.Count; i++)
            //        Sets.AddLevel($"{i + 1}. {ElementsCollection[i].ElementName}",
            //            ElementsCollection[i].GetSettings().ToArray());
            //}

            //Redraw();
        }
        private void AddElement(CenterIndicator el)
        {
            el.SetChart(Chart);
            el.SetDeleteAction(DeleteElement);
            el.Moving += MoveIndicator;
            ForegroundIndicators.Add(el);
            ForegroundIndLayer.AddVisual(el.IndicatorVisual);
            CollectionChanged();
            ResetHooks();
        }
        private void DeleteElement(HookElement e)
        {
            var el = e as CenterIndicator;
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
            CollectionChanged();
            ResetHooks();
        }
        private void ResetVisualsBackground()
        {
            BackgroundIndLayer.ClearVisuals();
            foreach (var ind in BackgroundIndicators) 
                BackgroundIndLayer.AddVisual(ind.IndicatorVisual);
        }
        private void ResetVisualsForeground()
        {
            BackgroundIndLayer.ClearVisuals();
            foreach (var ind in BackgroundIndicators)
                BackgroundIndLayer.AddVisual(ind.IndicatorVisual);
        }
        private void MoveIndicator(CenterIndicator indicator, int i)
        {
            if (ForegroundIndicators.Contains(indicator))
            {
                if (i > 0)
                {
                    i = ForegroundIndicators.IndexOf(indicator);
                    if (i == ForegroundIndicators.Count - 1) return;
                    ForegroundIndicators.Remove(indicator);
                    ForegroundIndicators.Insert(i + 1, indicator);
                    ResetVisualsForeground();
                }
                else
                {
                    i = ForegroundIndicators.IndexOf(indicator); 
                    if (i == 0)
                    {
                        ForegroundIndicators.Remove(indicator);
                        BackgroundIndicators.Add(indicator);
                        ResetVisualsBackground();
                        ResetVisualsForeground();
                    }
                    else
                    {
                        ForegroundIndicators.Remove(indicator);
                        ForegroundIndicators.Insert(i - 1, indicator);
                        ResetVisualsForeground();
                    }
                }
            }
            else
            {
                if (i > 0)
                {
                    i = BackgroundIndicators.IndexOf(indicator);
                    if (i == BackgroundIndicators.Count - 1)
                    {
                        BackgroundIndicators.Add(indicator);
                        ForegroundIndicators.Insert(0, indicator);
                        ResetVisualsBackground();
                        ResetVisualsForeground();
                    }
                    else
                    {
                        BackgroundIndicators.Remove(indicator);
                        BackgroundIndicators.Insert(i + 1, indicator);
                        ResetVisualsBackground();
                    }
                }
                else
                {
                    i = BackgroundIndicators.IndexOf(indicator);
                    if (i == 0) return;
                    BackgroundIndicators.Remove(indicator);
                    BackgroundIndicators.Insert(i - 1, indicator);
                    ResetVisualsBackground();
                }
            }
        }

        private int ChangesCounter = 0;
        private void ResetHooks()
        {
            Task.Run(() => 
            {
                ChangesCounter += 1;
                var x = ChangesCounter;
                Thread.Sleep(50);
                if (x != ChangesCounter) return;
                VisibleHooks = (from el in BackgroundIndicators.AsParallel() where el.VisibilityOnChart select el.Hook).ToList();
                VisibleHooks.AddRange(from el in BackgroundIndicators.AsParallel() where el.VisibilityOnChart select el.Hook);
            });
        }

        private void Redraw()
        {
            Task.Run(() =>
            {
                
                foreach (var ind in BackgroundIndicators) ind.Rendering();
                foreach (var ind in ForegroundIndicators) ind.Rendering();
            });
        }

        public List<Hook> VisibleHooks { get; private set; }
    }
}
