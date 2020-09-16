﻿/* 
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

namespace ChartModules
{
    public abstract class HookElement
    {
        public HookElement()
        {
            Sets.Add(new Setting(Delete));

            Subhooks.AddRange(CreateSubhooks());
            Hook = new Hook(this, GetDistance, GetHookPoint, GetMagnetRadius, ChangeMethod, DrawElement, DrawShadow, AcceptNewCoordinates, Subhooks);
        }

        private IChart chart;
        public IChart Chart { get => chart; set { chart = value; Dispatcher = value.Dispatcher; } }
        private protected Dispatcher Dispatcher { get; set; }
        public abstract string ElementName { get; }
        public abstract bool VisibilityOnChart { get; }

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
        public void Delete() => DeleteAct.Invoke(this);
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

        private protected void DrawElement(Vector? vec, DrawingVisual ElementsVisual, DrawingVisual PricesVisual, DrawingVisual TimesVisual, bool DrawOver = false)
        {
            Task.Run(() =>
            {
                Action<DrawingContext>[] acts;
                if (PricesVisual == null) acts = PrepareToDrawing(vec, 0, DrawOver);
                else acts = PrepareToDrawing(vec, VisualTreeHelper.GetDpi(PricesVisual).PixelsPerDip, DrawOver);

                Dispatcher.Invoke(() =>
                {
                    using (var dc = ElementsVisual.RenderOpen())
                        acts[0]?.Invoke(dc);
                    using (var dc = PricesVisual?.RenderOpen())
                        acts[1]?.Invoke(dc);
                    using (var dc = TimesVisual?.RenderOpen())
                        acts[2]?.Invoke(dc);
                });
            });
        }

        private readonly List<Hook> Subhooks = new List<Hook>();

        private protected virtual List<Hook> CreateSubhooks() => new List<Hook>();

        public abstract List<(string Name, Action Act)> GetContextMenu();
    }
    public enum ChangesElementType
    {
        Price,
        Point
    }
}