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

using ChartModules.StandardModules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
            Func<Pen> GetCursorPen, Action<Action<MouseButtonEventArgs>> SetInstrument, List<FrameworkElement> OtherLayers) : base(chart)
        {
            this.ChartWindow = ChartWindow;
            this.SetInstrument = SetInstrument;
            this.OtherLayers = OtherLayers;
            this.HooksLayer = HooksLayer;
            this.HookPriceLayer = HookPriceLayer;
            this.HookTimeLayer = HookTimeLayer;
            this.GetCursorPen = GetCursorPen;

            this.SetMenuAct = ChartWindow.SetMenu;

            HooksLayer.AddVisual(HookVisual);
            HooksLayer.AddVisual(NewHookVisual);
            HooksLayer.AddVisual(PointVisual);
            HookPriceLayer.AddVisual(HookPriceVisual);
            HookPriceLayer.AddVisual(NewHookPriceVisual);
            HookTimeLayer.AddVisual(NewHookTimeVisual);
            HookTimeLayer.AddVisual(HookTimeVisual);
        }
        private protected override void Destroy()
        {
            HooksLayer.DeleteVisual(HookVisual);
            HooksLayer.DeleteVisual(NewHookVisual);
            HooksLayer.DeleteVisual(PointVisual);
            HookPriceLayer.DeleteVisual(HookPriceVisual);
            HookPriceLayer.DeleteVisual(NewHookPriceVisual);
            HookTimeLayer.DeleteVisual(NewHookTimeVisual);
            HookTimeLayer.DeleteVisual(HookTimeVisual);
        }
        private protected override void SetsDefinition() { }
        public override Task Redraw() => null;

        private readonly Func<Pen> GetCursorPen;
        private readonly IChartWindow ChartWindow;
        private readonly List<FrameworkElement> OtherLayers;
        private readonly Action<Action<MouseButtonEventArgs>> SetInstrument;

        private readonly Action<Grid> SetMenuAct;
        private void TestSetMenu(string Content = null)
        {
            var grd = new Grid { Background = Brushes.DarkGray };
            grd.Children.Add(new Label
            {
                Foreground = Brushes.White,
                Background = Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Content = Content
            });
            if (Content != null) SetMenuAct(grd);
            else SetMenuAct(null);
        }
        public Action RemoveHook;

        private readonly DrawingVisual HookVisual = new DrawingVisual();
        private readonly DrawingVisual HookPriceVisual = new DrawingVisual();
        private readonly DrawingVisual HookTimeVisual = new DrawingVisual();
        private readonly DrawingVisual PointVisual = new DrawingVisual();
        private readonly DrawingVisual NewHookVisual = new DrawingVisual();
        private readonly DrawingVisual NewHookPriceVisual = new DrawingVisual();
        private readonly DrawingVisual NewHookTimeVisual = new DrawingVisual();
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
        private Point LastValue;
        private Point NewValue;
        public void HookElement(object s, MouseEventArgs e)
        {
            if (Manipulating) return;

            var pn = GetCursorPen.Invoke();
            var P = e.GetPosition((IInputElement)Chart);

            var AllHooks = new List<Hook>();
            foreach (var m in Chart.HooksModules)
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
                    LastValue = NewHook.GetValue(P);
                    NewValue = LastValue;
                    CurrentHook = NewHook;
                    
                    NewHook.Catch(LastValue, NewHookVisual, NewHookPriceVisual, NewHookTimeVisual);
                    
                    SetInstrument.Invoke(e =>
                    {
                        if (!Manipulating)
                        {
                            LastValue = NewHook.GetValue(e.GetPosition((IInputElement)Chart));
                            Manipulating = true;
                            NewHook.Hide(LastValue, HookVisual, HookPriceVisual, HookTimeVisual);
                            foreach (var l in OtherLayers)
                                l.Visibility = Visibility.Visible;
                            PointVisual.Transform = null;
                            TestSetMenu("Захвачено");
                            using var dc = PointVisual.RenderOpen();
                            dc.DrawLine(pn, new Point(LastValue.X + 10, LastValue.Y + 10), new Point(LastValue.X - 10, LastValue.Y - 10));
                            dc.DrawLine(pn, new Point(LastValue.X + 10, LastValue.Y - 10), new Point(LastValue.X - 10, LastValue.Y + 10));
                        }

                        ChartWindow.MoveCursor(e,
                            vec =>
                            {
                                if (vec != null)
                                {
                                    NewValue = LastValue + vec.Value;
                                    NewHook.Manipulating(LastValue, vec.Value, NewHookVisual, NewHookPriceVisual, NewHookTimeVisual);
                                    PointVisual.Transform = new TranslateTransform(vec.Value.X, vec.Value.Y);
                                }
                                else
                                {
                                    Manipulating = false;

                                    NewHook.AcceptNewCoordinates(NewValue);

                                    LastValue = NewHook.GetValue(e.GetPosition((IInputElement)Chart));
                                    NewHook.Hide(LastValue, HookVisual, HookPriceVisual, HookTimeVisual);

                                    //PointVisual.Transform = null;
                                    //using var dc = PointVisual.RenderOpen();
                                    //dc.DrawLine(pn, new Point(LastValue.X + 10, LastValue.Y + 10), new Point(LastValue.X - 10, LastValue.Y - 10));
                                    //dc.DrawLine(pn, new Point(LastValue.X + 10, LastValue.Y - 10), new Point(LastValue.X - 10, LastValue.Y + 10));
                                    NewHookVisual.Transform = null;
                                    NewHookPriceVisual.Transform = null;
                                    NewHookTimeVisual.Transform = null;
                                    NewHook.Catch(LastValue, NewHookVisual, NewHookPriceVisual, NewHookTimeVisual);
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

                RemoveHook = () => 
                {
                    RemoveHook = null;
                    TestSetMenu(null);
                    PointVisual.RenderOpen().Close();
                };

                foreach (var l in OtherLayers)
                    l.Visibility = Visibility.Visible;

                var dc = HookVisual.RenderOpen(); dc.Close();
                dc = HookPriceVisual.RenderOpen(); dc.Close();
                dc = HookTimeVisual.RenderOpen(); dc.Close();
            }
        }
    }

    public class Hook
    {
        public Hook(Func<double, double, double> Zxy, 
            Action<Point, DrawingVisual, DrawingVisual, DrawingVisual> Catch,
            Action<Point, DrawingVisual, DrawingVisual, DrawingVisual> Hide,
            Action<Point, Vector, DrawingVisual, DrawingVisual, DrawingVisual> Manipulate, 
            Func<Point, Point> GetValue,
            Action<Point> AcceptChanges, 
            double MagnetRadius, 
            Func<Grid> ShowSettingsMenu, 
            List<Hook> SubHooks = null)
        {
            this.Zxy = Zxy;
            this.ActionCatch = Catch;
            this.ActionHide = Hide;
            this.GetVal = GetValue;
            this.Manipulate = Manipulate;
            this.AcceptChanges = AcceptChanges;
            this.MagnetRadius = MagnetRadius;
            this.SubHooks = SubHooks;
            this.ShowSM = ShowSettingsMenu;
        }

        public double MagnetRadius { get; }
        public List<Hook> SubHooks { get; }
        private Action<Point, Vector, DrawingVisual, DrawingVisual, DrawingVisual> Manipulate { get; }
        private Action<Point> AcceptChanges { get; }
        private Func<Grid> ShowSM { get; }
        private Func<double, double, double> Zxy;
        private Func<Point, Point> GetVal;
        private Action<Point, DrawingVisual, DrawingVisual, DrawingVisual> ActionCatch;
        private Action<Point, DrawingVisual, DrawingVisual, DrawingVisual> ActionHide;

        public void Manipulating(Point LastPosition, Vector ChangesVector, DrawingVisual HookVisual, DrawingVisual HookPriceVisual, DrawingVisual HookTimeVisual) => 
            Manipulate.Invoke(LastPosition, ChangesVector, HookVisual, HookPriceVisual, HookTimeVisual);
        public void AcceptNewCoordinates(Point NewCoordinates) => AcceptChanges.Invoke(NewCoordinates);
        public Point GetCurrentPosition(Point CursorPos) => GetVal.Invoke(CursorPos);
        public double GetDistance(Point CursorPoint) => Math.Abs(Zxy.Invoke(CursorPoint.X, CursorPoint.Y));
        public Point GetValue(Point P) => GetVal.Invoke(P);
        public void Catch(Point point, DrawingVisual HookVisual, DrawingVisual HookPriceVisual, DrawingVisual HookTimeVisual) =>
            Task.Run(() => ActionCatch.Invoke(point, HookVisual, HookPriceVisual, HookTimeVisual));
        public void Hide(Point point, DrawingVisual HookVisual, DrawingVisual HookPriceVisual, DrawingVisual HookTimeVisual) =>
            Task.Run(() => ActionHide.Invoke(point, HookVisual, HookPriceVisual, HookTimeVisual));
        public void ShowSettingsMenu() => ShowSM.Invoke();
    }
}