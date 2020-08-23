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
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ChartModules.PaintingModule
{
    public class HooksModule : ChartModule
    {
        private readonly IDrawingCanvas HooksLayer;
        private readonly IDrawingCanvas HookPriceLayer;
        private readonly IDrawingCanvas HookTimeLayer;

        public HooksModule(IChart chart, IChartWindow ChartWindow, IDrawingCanvas HooksLayer, IDrawingCanvas HookPriceLayer, IDrawingCanvas HookTimeLayer,
            Func<Pen> GetCursorPen, Func<List<Hook>> GetVisibleHooks, Action<Action<MouseButtonEventArgs>> SetInstrument, List<FrameworkElement> OtherLayers) : base(chart)
        {
            this.ChartWindow = ChartWindow;
            this.SetInstrument = SetInstrument;
            this.OtherLayers = OtherLayers;
            this.HooksLayer = HooksLayer;
            this.HookPriceLayer = HookPriceLayer;
            this.HookTimeLayer = HookTimeLayer;
            this.GetCursorPen = GetCursorPen;
            this.GetVisibleHooks = GetVisibleHooks;

            this.SetMenuAct = ChartWindow.SetMenu;

            Chart.VerticalСhanges += () => Task.Run(() => ResizeHook?.Invoke());
            Chart.HorizontalСhanges += () => Task.Run(() => ResizeHook?.Invoke());

            HooksLayer.AddVisual(ShadowVisual);
            HooksLayer.AddVisual(OverVisual);
            HooksLayer.AddVisual(PointVisual);
            HookPriceLayer.AddVisual(ShadowPriceVisual);
            HookPriceLayer.AddVisual(OverPriceVisual);
            HookTimeLayer.AddVisual(OverTimeVisual);
            HookTimeLayer.AddVisual(ShadowTimeVisual);
        }
        private protected override void Destroy()
        {
            HooksLayer.DeleteVisual(ShadowVisual);
            HooksLayer.DeleteVisual(OverVisual);
            HooksLayer.DeleteVisual(PointVisual);
            HookPriceLayer.DeleteVisual(ShadowPriceVisual);
            HookPriceLayer.DeleteVisual(OverPriceVisual);
            HookTimeLayer.DeleteVisual(OverTimeVisual);
            HookTimeLayer.DeleteVisual(ShadowTimeVisual);
        }
        public override Task Redraw() => null;

        private readonly Func<Pen> GetCursorPen;
        private readonly Func<List<Hook>> GetVisibleHooks;
        private readonly IChartWindow ChartWindow;
        private readonly List<FrameworkElement> OtherLayers;
        private readonly Action<Action<MouseButtonEventArgs>> SetInstrument;

        private readonly Action<string, List<Setting>> SetMenuAct;
        public Action RemoveHook;
        private Action ResizeHook;

        private readonly DrawingVisual ShadowVisual = new DrawingVisual();
        private readonly DrawingVisual ShadowPriceVisual = new DrawingVisual();
        private readonly DrawingVisual ShadowTimeVisual = new DrawingVisual();
        private readonly DrawingVisual PointVisual = new DrawingVisual();
        private readonly DrawingVisual OverVisual = new DrawingVisual();
        private readonly DrawingVisual OverPriceVisual = new DrawingVisual();
        private readonly DrawingVisual OverTimeVisual = new DrawingVisual();

        public (List<(string Name, Action Act)> Menus, Action RemoveMenu)? ShowContextMenu(object s, MouseEventArgs e)
        {
            var P = e.GetPosition((IInputElement)Chart);
            var Hook = ScanHooks(GetVisibleHooks.Invoke(), P);

            if (Hook != null)
            {
                RemoveLastHook();
                P = Hook.GetHookPoint(P);
                var pn = GetCursorPen.Invoke();
                using var dc = PointVisual.RenderOpen();
                dc.DrawLine(pn, new Point(P.X + 10, P.Y + 10), new Point(P.X - 10, P.Y - 10));
                dc.DrawLine(pn, new Point(P.X + 10, P.Y - 10), new Point(P.X - 10, P.Y + 10));

                return (Hook.GetContextMenu(), () => PointVisual.RenderOpen().Close());
            }
            else return null;
        }

        private Hook CurrentHook;
        private Hook ScanHooks(List<Hook> Hooks, Point P)
        {
            Hook NewHook = null;
            double Min = 999;
            foreach (var h in Hooks)
            {
                var d = h.GetDistance(P);
                if (d < h.GetMagnetRadius())
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
        private double LastPointPrice;
        private DateTime LastPointTime;
        public void HookElement(object s, MouseEventArgs e)
        {
            if (Manipulating) return;

            var pn = GetCursorPen.Invoke();
            var P = e.GetPosition((IInputElement)Chart);

            var NewHook = ScanHooks(GetVisibleHooks.Invoke(), P);

            if (NewHook != null)
            {
                RemoveHook = null;

                if (NewHook.SubHooks != null)
                {
                    var SubHook = ScanHooks(NewHook.SubHooks, P);
                    if (SubHook != null) NewHook = SubHook;
                }

                if (NewHook != CurrentHook)
                {
                    CurrentHook?.ClearEvents();

                    ShadowVisual.RenderOpen().Close();
                    ShadowPriceVisual.RenderOpen().Close();
                    ShadowTimeVisual.RenderOpen().Close();

                    LastValue = NewHook.GetHookPoint(P);
                    
                    NewValue = LastValue;
                    CurrentHook = NewHook;

                    NewHook.DrawOver(LastValue, OverVisual, OverPriceVisual, OverTimeVisual);

                    SetInstrument.Invoke(e =>
                    {
                        if (!Manipulating)
                        {
                            LastValue = NewHook.GetHookPoint(e.GetPosition((IInputElement)Chart));
                            LastPointPrice = Chart.HeightToPrice(LastValue.Y);
                            LastPointTime = Chart.WidthToTime(LastValue.X);

                            NewHook.SetResetElementAction(ce =>
                            {
                                if (ce != null)
                                {
                                    switch (ce.Value.type)
                                    {
                                        case ChangesElementType.Price:
                                            P.Y = Chart.PriceToHeight((double)ce.Value.element);
                                            break;
                                        case ChangesElementType.Point:
                                            P = (Point)ce.Value.element;
                                            break;
                                        default: throw new Exception();
                                    }

                                    LastValue = NewHook.GetHookPoint(P);
                                    NewValue = LastValue;
                                    CurrentHook = NewHook;

                                    Dispatcher.Invoke(() =>
                                    {
                                        ShadowVisual.RenderOpen().Close();
                                        ShadowPriceVisual.RenderOpen().Close();
                                        ShadowTimeVisual.RenderOpen().Close();

                                        using var dc = PointVisual.RenderOpen();
                                        dc.DrawLine(pn, new Point(LastValue.X + 10, LastValue.Y + 10), new Point(LastValue.X - 10, LastValue.Y - 10));
                                        dc.DrawLine(pn, new Point(LastValue.X + 10, LastValue.Y - 10), new Point(LastValue.X - 10, LastValue.Y + 10));
                                    });
                                }
                                NewHook.DrawShadow(LastValue, ShadowVisual, ShadowPriceVisual, ShadowTimeVisual);
                                NewHook.DrawOver(LastValue, OverVisual, OverPriceVisual, OverTimeVisual);
                            });
                            ResizeHook = () =>
                            {
                                P = new Point(Chart.TimeToWidth(LastPointTime), Chart.PriceToHeight(LastPointPrice));
                                LastValue = NewHook.GetHookPoint(P);

                                NewHook.DrawShadow(LastValue, ShadowVisual, ShadowPriceVisual, ShadowTimeVisual);
                                NewHook.DrawOver(LastValue, OverVisual, OverPriceVisual, OverTimeVisual);

                                Dispatcher.Invoke(() =>
                                {
                                    PointVisual.Transform = null;
                                    using var dc = PointVisual.RenderOpen();
                                    dc.DrawLine(pn, new Point(LastValue.X + 10, LastValue.Y + 10), new Point(LastValue.X - 10, LastValue.Y - 10));
                                    dc.DrawLine(pn, new Point(LastValue.X + 10, LastValue.Y - 10), new Point(LastValue.X - 10, LastValue.Y + 10));
                                });

                            };

                            Manipulating = true;
                            NewHook.DrawShadow(LastValue, ShadowVisual, ShadowPriceVisual, ShadowTimeVisual);
                            foreach (var l in OtherLayers)
                                l.Visibility = Visibility.Visible;
                            PointVisual.Transform = null;
                            SetMenuAct(NewHook.ElementName, NewHook.Sets);
                            using var dc = PointVisual.RenderOpen();
                            dc.DrawLine(pn, new Point(LastValue.X + 10, LastValue.Y + 10), new Point(LastValue.X - 10, LastValue.Y - 10));
                            dc.DrawLine(pn, new Point(LastValue.X + 10, LastValue.Y - 10), new Point(LastValue.X - 10, LastValue.Y + 10));
                        }

                        if (!NewHook.Locked)
                        {
                            ChartWindow.MoveCursor(e,
                            vec =>
                            {
                                if (vec != null)
                                {
                                    NewValue = LastValue + vec.Value;
                                    NewHook.DrawOver(NewValue, OverVisual, OverPriceVisual, OverTimeVisual);
                                    PointVisual.Transform = new TranslateTransform(vec.Value.X, vec.Value.Y);
                                }
                                else
                                {
                                    Manipulating = false;

                                    NewHook.AcceptNewCoordinates(NewValue);
                                    SetMenuAct(NewHook.ElementName, NewHook.Sets);

                                    LastValue = NewHook.GetHookPoint(e.GetPosition((IInputElement)Chart));
                                    LastPointPrice = Chart.HeightToPrice(LastValue.Y);
                                    LastPointTime = Chart.WidthToTime(LastValue.X);
                                    NewHook.DrawShadow(LastValue, ShadowVisual, ShadowPriceVisual, ShadowTimeVisual);

                                    OverVisual.Transform = null;
                                    OverPriceVisual.Transform = null;
                                    OverTimeVisual.Transform = null;
                                    NewHook.DrawOver(LastValue, OverVisual, OverPriceVisual, OverTimeVisual);
                                }
                            });
                        }
                        else
                        {
                            ChartWindow.MoveCursor(e,
                            vec =>
                            {
                                if(vec == null)
                                    Manipulating = false;
                            });
                        }
                    });
                    foreach (var l in OtherLayers)
                        l.Visibility = Visibility.Hidden;
                }
            }
            else if (CurrentHook != null)
            {
                CurrentHook.ClearEvents();
                RemoveHook = RemoveLastHook;
                SetInstrument.Invoke(null);
                RestoreChart();
            }
        }
        public void RemoveLastHook()
        {
            ResizeHook = null;
            Dispatcher.Invoke(() =>
            {
                RemoveHook = null;
                
                SetMenuAct(null, null);
                ShadowVisual.RenderOpen().Close();
                ShadowPriceVisual.RenderOpen().Close();
                ShadowTimeVisual.RenderOpen().Close();
                OverVisual.RenderOpen().Close();
                OverPriceVisual.RenderOpen().Close();
                OverTimeVisual.RenderOpen().Close();
                PointVisual.RenderOpen().Close();

                RestoreChart();
            });
        }
        public void RestoreChart()
        {
            if (CurrentHook != null)
            {
                CurrentHook = null;
                Dispatcher.Invoke(() => 
                {
                    foreach (var l in OtherLayers)
                        l.Visibility = Visibility.Visible;
                });
            } 
        }
    }


    public class Hook
    {
        /// <summary>
        /// Класс для манипуляции нарисованными элемнтами
        /// </summary>
        /// <param name="GetDistanceXY">Функция возвращающая дистанцию до цели</param>
        /// <param name="DrawElement">Рисование копии объекта</param>
        /// <param name="DrawShadow">Рисование тени объекта</param>
        /// <param name="GetHookPoint">Функция возвращающая точку захвата</param>
        /// <param name="AcceptNewCoordinates">Применить изменения к оригиналу</param>
        /// <param name="Element">Манипулируемый элемент</param>
        /// <param name="SubHooks"></param>
        public Hook(
            ChangingElement Element,
            Func<double, double, double> GetDistanceXY,
            Func<Point, Point> GetHookPoint,
            Action<Point?, DrawingVisual, DrawingVisual, DrawingVisual> DrawElement,
            Action<Point, DrawingVisual, DrawingVisual, DrawingVisual> DrawShadow,
            Action<Point> AcceptNewCoordinates,
            List<Hook> SubHooks = null)
        {
            this.Element = Element;
            this.GetDistanceXY = GetDistanceXY;
            ActionDrawShadow = DrawShadow;
            GetVal = GetHookPoint;
            ActionDrawOver = DrawElement;
            this.SubHooks = SubHooks;
            this.MagnetRadius = Element.GetMagnetRadius;
            this.AcceptChanges = AcceptNewCoordinates;

            Element.Changed += o => Task.Run(() => ResetElement?.Invoke(o));
        }

        private event Action<(ChangesElementType type, object element)?> ResetElement;
        public void SetResetElementAction(Action<(ChangesElementType type, object element)?> rea)
        {
            ResetElement = null;
            ResetElement += rea;
        }
        public void ClearEvents() => ResetElement = null;

        private readonly ChangingElement Element;
        public bool Locked { get => Element.Locked; }

        public List<Hook> SubHooks { get; }
        public string ElementName { get => Element.ElementName; }
        public List<Setting> Sets { get => Element.GetSettings(); }
        private Func<double> MagnetRadius { get; }
        private Action<Point> AcceptChanges { get; }
        private readonly Func<double, double, double> GetDistanceXY;
        private readonly Func<Point, Point> GetVal;
        private readonly Action<Point?, DrawingVisual, DrawingVisual, DrawingVisual> ActionDrawOver;
        private readonly Action<Point, DrawingVisual, DrawingVisual, DrawingVisual> ActionDrawShadow;

        public double GetMagnetRadius() => MagnetRadius();
        
        public Point GetCurrentPosition(Point CursorPos) => GetVal.Invoke(CursorPos);
        public double GetDistance(Point CursorPoint) => Math.Abs(GetDistanceXY.Invoke(CursorPoint.X, CursorPoint.Y));
        public Point GetHookPoint(Point P) => GetVal.Invoke(P);

        public void AcceptNewCoordinates(Point NewCoordinates)
        { AcceptChanges.Invoke(NewCoordinates); Element.ApplyChange.Invoke(); }

        public void DrawShadow(Point point, DrawingVisual ShadowVisual, DrawingVisual ShadowPriceVisual, DrawingVisual ShadowTimeVisual) =>
            ActionDrawShadow.Invoke(point, ShadowVisual, ShadowPriceVisual, ShadowTimeVisual);

        public void DrawOver(Point point, DrawingVisual ShadowVisual, DrawingVisual ShadowPriceVisual, DrawingVisual ShadowTimeVisual) =>
            ActionDrawOver.Invoke(point, ShadowVisual, ShadowPriceVisual, ShadowTimeVisual);

        public List<(string Name, Action Act)> GetContextMenu() 
        {
            var Items = Element.GetContextMenu();

            if (Items.Count > 0) Items.Add(("+++", null));
            if (Locked) Items.Add(("Unlock", () => Element.Locked = !Element.Locked));
            else Items.Add(("Lock", () => Element.Locked = !Element.Locked));
            Items.Add(("Delete", () => Element.Delete.Invoke(Element)));

            return Items;
        }
    }
}