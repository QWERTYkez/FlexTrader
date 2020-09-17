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
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ChartModules.CenterIndicators
{
    public abstract class HookElement
    {
        public HookElement()
        {
            Sets.Add(new Setting((int i) => Moving.Invoke(this, i)));
            Sets.Add(new Setting(Delete));

            Subhooks.AddRange(CreateSubhooks());
            Hook = new Hook(this, GetDistance, GetHookPoint, GetMagnetRadius, ChangeMethod,
                DrawElement, DrawShadow, AcceptNewCoordinates, Subhooks);
        }

        public virtual void SetChart(IChart Chart)
        {
            this.Chart = Chart;
        }

        private IChart chart;
        public IChart Chart { get => chart; set { chart = value; Dispatcher = value.Dispatcher; } }
        private protected Dispatcher Dispatcher { get; set; }
        public abstract string ElementName { get; }
        public abstract bool VisibilityOnChart { get; }

        public readonly DrawingVisual IndicatorVisual = new DrawingVisual();
        public DrawingVisual PriceVisual { get; set; }
        public DrawingVisual TimeVisual { get; set; }

        public void Rendering() => DrawElement(null, IndicatorVisual, PriceVisual, TimeVisual);
        private protected virtual void CalculateData() { }

        public bool Locked = false;
        public readonly List<Setting> Sets = new List<Setting>();

        public abstract double GetMagnetRadius();

        public event Action<(ChangesElementType type, object element)?> Changed;
        private Action ApplyChangeAct;
        public void SetApplyChangeAction(Action ApplyChangeAct)
        {
            this.ApplyChangeAct = ApplyChangeAct;
        }
        public void ApplyChanges() => ApplyChangeAct?.Invoke();
        private Action<HookElement> DeleteAct;
        public void SetDeleteAction(Action<HookElement> DeleteAct)
        {
            this.DeleteAct = DeleteAct;
        }
        public void Delete() => Dispatcher.Invoke(() => DeleteAct.Invoke(this));
        public Action ChangeHook { get; set; }
        
        private protected void ApplyChangesToAll()
        {
            ApplyChanges();
            Changed.Invoke(null);
        }
        private protected void ApplyChangesToAll(double Price)
        {
            ApplyChanges();
            Changed.Invoke((ChangesElementType.Price, Price));
        }
        private protected void ApplyChangesToAll(Point Point)
        {
            ApplyChanges();
            Changed.Invoke((ChangesElementType.Point, Point));
        }

        public Hook Hook { get; }

        public void AcceptNewCoordinates()
        {
            this.NewCoordinates();
            this.ApplyChanges();
        }
        private protected abstract double GetDistance(Point P);
        private protected abstract Point GetHookPoint(Point P);
        public abstract Action<DrawingContext>[] PrepareToDrawing(Vector? vec, double PixelsPerDip, bool DrawOver = false);
        private protected abstract void DrawShadow(DrawingVisual ElementsVisual, DrawingVisual PricesVisual, DrawingVisual TimesVisual);
        private protected abstract void NewCoordinates();
        private protected abstract void ChangeMethod(Vector? Changes);

        private protected void DrawElement(Vector? vec, DrawingVisual ElementVisual, DrawingVisual PriceVisual, DrawingVisual TimeVisual, bool DrawOver = false)
        {
            Task.Run(() =>
            {
                Action<DrawingContext>[] acts;
                if (PriceVisual == null) acts = PrepareToDrawing(vec, 0, DrawOver);
                else 
                    acts = PrepareToDrawing(vec, VisualTreeHelper.GetDpi(PriceVisual).PixelsPerDip, DrawOver);

                Dispatcher.Invoke(() =>
                {
                    using (var dc = ElementVisual.RenderOpen())
                        acts[0]?.Invoke(dc);
                    using (var dc = PriceVisual?.RenderOpen())
                        acts[1]?.Invoke(dc);
                    using (var dc = TimeVisual?.RenderOpen())
                        acts[2]?.Invoke(dc);
                });
            });
        }

        private protected async void ApplyDataChanges()
        {
            await Redraw();
            ApplyChangesToAll();
        }
        private protected void ApplyRenderChanges()
        {
            Rendering();
            ApplyChangesToAll();
        }
        private protected Task Redraw()
        {
            return Task.Run(() =>
            {
                if (Chart.StartTime == DateTime.FromBinary(0)) return;
                CalculateData();
                Rendering();
            });
        }

        private readonly List<Hook> Subhooks = new List<Hook>();

        private protected virtual List<Hook> CreateSubhooks() => new List<Hook>();

        public event Action<HookElement, int> Moving;
        private protected void ToFront() => Dispatcher.Invoke(() => Moving(this, 2));
        private protected void ToBack() => Dispatcher.Invoke(() => Moving(this, -2));

        public abstract List<(string Name, Action Act)> GetContextMenu();

        private protected TimeSpan dT;
        private protected Point GetPoint(DateTime time, double val) => new Point(GetX(time), GetY(val));
        private protected double GetX(DateTime time) => (time - Chart.TimeA) / dT;
        private protected double GetY(double val) => Chart.ChHeight * (Chart.PricesMin + Chart.PricesDelta - val / Chart.TickSize) / Chart.PricesDelta;
    }
    public enum ChangesElementType
    {
        Price,
        Point
    }
}
