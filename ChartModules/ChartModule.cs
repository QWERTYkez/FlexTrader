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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ChartModules
{
    public abstract class ChartModule
    {
        private protected IChart Chart;
        private protected Dispatcher Dispatcher;
        public ChartModule(IChart chart)
        {
            Chart = chart;
            if (this is IHooksModule) Chart.HooksModules.Add((IHooksModule)this);
            Dispatcher = Chart.Dispatcher;
        }
        public abstract Task Redraw();
        public void Restruct()
        {
            Chart = null;
            Destroy();
        }
        private protected abstract void Destroy();
        public (string SetsName, List<Setting> Sets) GetSets() => (SetsName, Sets);
        private protected List<Setting> Sets { get; } = new List<Setting>();
        private protected string SetsName { get; set; }
    }

    public abstract class PaintingChartModule : ChartModule, IHooksModule
    {
        private readonly IDrawingCanvas ElementsCanvas;
        private protected readonly Action<string> ResetInstrument;
        private protected PaintingChartModule(IChart chart, IDrawingCanvas ElementsCanvas, string SetsName, string ElementName, Action<string> ResetInstrument) : base(chart) 
        {
            this.ElementName = ElementName;
            this.SetsName = SetsName;
            this.ElementsCanvas = ElementsCanvas;
            this.ResetInstrument = ResetInstrument;

            ElementsCanvas.AddVisual(ElementsVisual);
        }

        private protected readonly DrawingVisual ElementsVisual = new DrawingVisual();
        private protected override void Destroy()
        {
            ElementsCanvas.ClearVisuals();
        }

        private string ElementName { get; }

        private protected readonly List<ChangingElement> ElementsCollection = new List<ChangingElement>();
        private void CollectionChanged()
        {
            Sets.Clear();
            for (int i = 0; i < ElementsCollection.Count; i++)
                Setting.SetsLevel(Sets, $"{ElementName} {i + 1}", ElementsCollection[i].GetSettings().ToArray());

            Redraw();
        }

        private protected void AddElementToCollection(ChangingElement el)
        {
            el.ApplyChange = CollectionChanged;
            ElementsCollection.Add(el);
            CollectionChanged();
        }

        public abstract void PaintingElement(MouseButtonEventArgs e);

        //IHooksModule
        private protected abstract void ResetHooks();
        public List<Hook> Hooks { get; private set; } = new List<Hook>();
    }
}
