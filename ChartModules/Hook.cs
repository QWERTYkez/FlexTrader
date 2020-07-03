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
using System.Windows.Input;
using System.Windows.Media;

namespace ChartModules
{
    public class HooksModule : ChartModule
    {
        private readonly IDrawingCanvas HooksLayer;
        private readonly IDrawingCanvas HookPriceLayer;
        private readonly IDrawingCanvas HookTimeLayer;

        public HooksModule(IChart chart, IChartWindow ChartWindow, IDrawingCanvas HooksLayer, IDrawingCanvas HookPriceLayer, IDrawingCanvas HookTimeLayer,
            List<IHooksModule> HooksModules, Action<Action<MouseButtonEventArgs>> SetInstrument, List<FrameworkElement> OtherLayers) : base(chart)
        {
            this.ChartWindow = ChartWindow;
            this.HooksModules = HooksModules;
            this.SetInstrument = SetInstrument;
            this.OtherLayers = OtherLayers;
            this.HooksLayer = HooksLayer;
            this.HookPriceLayer = HookPriceLayer;
            this.HookTimeLayer = HookTimeLayer;

            HooksLayer.AddVisual(HookVisual);
            HookPriceLayer.AddVisual(HookPriceVisual);
            HookTimeLayer.AddVisual(HookTimeVisual);
        }
        private protected override void Destroy()
        {
            HooksLayer.DeleteVisual(HookVisual);
            HookPriceLayer.DeleteVisual(HookPriceVisual);
            HookTimeLayer.DeleteVisual(HookTimeVisual);
        }
        private protected override void SetsDefinition() { }
        public override Task Redraw() => null;

        private readonly List<IHooksModule> HooksModules;
        private readonly IChartWindow ChartWindow;
        private readonly List<FrameworkElement> OtherLayers;
        private readonly Action<Action<MouseButtonEventArgs>> SetInstrument;
        private readonly DrawingVisual HookVisual = new DrawingVisual();
        private readonly DrawingVisual HookPriceVisual = new DrawingVisual();
        private readonly DrawingVisual HookTimeVisual = new DrawingVisual();
        private Hook CurrentHook;
        private Hook ScanHooks(List<Hook> Hooks, Point P)
        {
            Hook NewHook = null;
            double Min = 999;
            foreach (var h in Hooks)
            {
                var d = h.GetDistance(P);
                if (d < h.MagnetRadius)
                {
                    if (d < Min)
                    {
                        NewHook = h;
                        Min = d;
                    }
                }
            }
            return NewHook;
        }
        private bool Manipulating = false;
        private int ChangesCounter = 0;
        private Point LastValue;
        public void HookElement(object s, MouseEventArgs e)
        {
            if (Manipulating) return;

            var P = e.GetPosition((IInputElement)Chart);

            var AllHooks = new List<Hook>();
            foreach (var m in HooksModules)
                AllHooks.AddRange(m.Hooks);

            var NewHook = ScanHooks(AllHooks, P);

            if (NewHook != null)
            {
                if(NewHook.SubHooks != null)
                {
                    var SubHook = ScanHooks(NewHook.SubHooks, P);
                    if (SubHook != null) NewHook = SubHook;
                }

                if (NewHook != CurrentHook)
                {
                    LastValue = NewHook.GetValue();
                    CurrentHook = NewHook;
                    NewHook.Catch(LastValue, HookVisual, HookPriceVisual, HookTimeVisual);
                    HookVisual.Transform = null;
                    HookPriceVisual.Transform = null;
                    HookTimeVisual.Transform = null;
                    SetInstrument.Invoke(e => 
                    {
                        Manipulating = true;

                        ChartWindow.MoveCursor(e, 
                            vec =>
                            {
                                if (vec != null)
                                {
                                    NewHook.Manipulate(LastValue, vec.Value, HookVisual, HookPriceVisual, HookTimeVisual);
                                    Task.Run(() => 
                                    {
                                        ChangesCounter += 1;
                                        var x = ChangesCounter;
                                        if (x != ChangesCounter) return;
                                        NewHook.AcceptChanges.Invoke(LastValue + vec.Value);
                                    });
                                }
                                else
                                {
                                    Manipulating = false;
                                    LastValue = NewHook.GetValue();
                                    NewHook.Catch(LastValue, HookVisual, HookPriceVisual, HookTimeVisual);
                                    HookVisual.Transform = null;
                                    HookPriceVisual.Transform = null;
                                    HookTimeVisual.Transform = null;
                                }
                            }); 
                    });
                    foreach (var l in OtherLayers)
                        l.Visibility = Visibility.Hidden;
                }
            }
            else if (CurrentHook != null)
            {
                CurrentHook = null;
                SetInstrument.Invoke(null);
                var dc = HookVisual.RenderOpen(); dc.Close();
                dc = HookPriceVisual.RenderOpen(); dc.Close();
                dc = HookTimeVisual.RenderOpen(); dc.Close();
                foreach (var l in OtherLayers)
                    l.Visibility = Visibility.Visible;
            }
        }
    }

    public class Hook
    {
        public Hook(Func<double, double, double> Zxy, Action<Point, DrawingVisual, DrawingVisual, DrawingVisual> Catch, 
            Action<Point, Vector, DrawingVisual, DrawingVisual, DrawingVisual> Manipulate, Func<Point> GetValue,
            Action<Point> AcceptChanges, double MagnetRadius, List<Hook> SubHooks = null)
        { 
            this.Zxy = Zxy; 
            ActionCatch = Catch;
            this.GetVal = GetValue;
            this.Manipulate = Manipulate;
            this.AcceptChanges = AcceptChanges;
            this.MagnetRadius = MagnetRadius;
            this.SubHooks = SubHooks;
        }

        public double MagnetRadius { get; }
        public List<Hook> SubHooks { get; }
        public Action<Point, Vector, DrawingVisual, DrawingVisual, DrawingVisual> Manipulate { get; }
        public Action<Point> AcceptChanges { get; }
        private Func<double, double, double> Zxy;
        public Func<Point> GetVal;
        private Action<Point, DrawingVisual, DrawingVisual, DrawingVisual> ActionCatch;
        public double GetDistance(Point CursorPoint) => Math.Abs(Zxy.Invoke(CursorPoint.X, CursorPoint.Y));
        public Point GetValue() => GetVal.Invoke();
        public void Catch(Point point, DrawingVisual HookVisual, DrawingVisual HookPriceVisual, DrawingVisual HookTimeVisual) => 
            Task.Run(() => ActionCatch.Invoke(point, HookVisual, HookPriceVisual, HookTimeVisual));
    }
}