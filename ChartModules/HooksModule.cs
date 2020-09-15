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
using System.Windows.Threading;

namespace ChartModules
{
    public class HooksModule : ChartModule
    {
        private readonly DrawingCanvas HooksLayer;
        private readonly DrawingCanvas HookPriceLayer;
        private readonly DrawingCanvas HookTimeLayer;

        public HooksModule(IChart chart, DrawingCanvas HooksLayer, DrawingCanvas HookPriceLayer, DrawingCanvas HookTimeLayer,
            Func<Pen> GetCursorPen, Func<List<Hook>> GetVisibleHooks, List<FrameworkElement> OtherLayers) : base(chart)
        {
            this.OtherLayers = OtherLayers;
            this.HooksLayer = HooksLayer;
            this.HookPriceLayer = HookPriceLayer;
            this.HookTimeLayer = HookTimeLayer;
            this.GetCursorPen = GetCursorPen;
            this.GetVisibleHooks = GetVisibleHooks;

            this.SetMenuAct = chart.MWindow.SetMenu;

            Chart.VerticalСhanges += () => Task.Run(() => ResizeHook?.Invoke());
            Chart.HorizontalСhanges += () => Task.Run(() => ResizeHook?.Invoke());

            Chart.ChartGrid.MouseEnter += (s, e) => Chart.Interacion = MoveHook;
            Chart.ChartGrid.MouseLeave += (s, e) => { if (Chart.Interacion == MoveHook) Chart.Interacion = null; };
            Chart.HookElement = HookElement;
            Chart.MWindow.RemoveHooks += () => Task.Run(() => RemoveHook?.Invoke());
            Chart.MWindow.ToggleInteraction += b =>
            {
                if (b) RemoveHook = null;
                else
                {
                    Task.Run(() =>
                    {
                        RemoveHook = RemoveLastHook;
                        RestoreChart();
                    });
                }
            };

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
            HooksLayer.RemoveVisual(ShadowVisual);
            HooksLayer.RemoveVisual(OverVisual);
            HooksLayer.RemoveVisual(PointVisual);
            HookPriceLayer.RemoveVisual(ShadowPriceVisual);
            HookPriceLayer.RemoveVisual(OverPriceVisual);
            HookTimeLayer.RemoveVisual(OverTimeVisual);
            HookTimeLayer.RemoveVisual(ShadowTimeVisual);
        }

        private protected override string SetsName { get; }

        private readonly Func<Pen> GetCursorPen;
        private readonly Func<List<Hook>> GetVisibleHooks;
        private readonly List<FrameworkElement> OtherLayers;

        private readonly Action<string, List<Setting>, Action, Action> SetMenuAct;
        private void SetMenu(string SetsName, List<Setting> Sets, Action DrawHook) => 
            SetMenuAct.Invoke(SetsName, Sets, DrawHook, () => 
            {
                ResizeHook = null;
                Dispatcher.Invoke(() =>
                {
                    RemoveHook = null;

                    ShadowVisual.RenderOpen().Close();
                    ShadowPriceVisual.RenderOpen().Close();
                    ShadowTimeVisual.RenderOpen().Close();
                    OverVisual.RenderOpen().Close();
                    OverPriceVisual.RenderOpen().Close();
                    OverTimeVisual.RenderOpen().Close();
                    PointVisual.RenderOpen().Close();

                    RestoreChart();
                });
            });
        private Action RemoveHook;
        private Action ResizeHook;

        private readonly DrawingVisual ShadowVisual = new DrawingVisual();
        private readonly DrawingVisual ShadowPriceVisual = new DrawingVisual();
        private readonly DrawingVisual ShadowTimeVisual = new DrawingVisual();
        private readonly DrawingVisual PointVisual = new DrawingVisual();
        private readonly DrawingVisual OverVisual = new DrawingVisual();
        private readonly DrawingVisual OverPriceVisual = new DrawingVisual();
        private readonly DrawingVisual OverTimeVisual = new DrawingVisual();

        public (List<(string Name, Action Act)> Menus, Action DrawHook, Action RemoveHook)? 
            ShowContextMenu(object s, MouseEventArgs e)
        {
            var P = e.GetPosition((IInputElement)Chart);
            var Hook = ScanHooks(GetVisibleHooks.Invoke(), P);

            if (Hook != null)
            {
                P = Hook.GetHookPoint(P);
                var pn = GetCursorPen.Invoke();


                return (Hook.GetContextMenu(),
                        () =>
                        {
                            using var dc = PointVisual.RenderOpen();
                            dc.DrawLine(pn, new Point(P.X + 10, P.Y + 10), new Point(P.X - 10, P.Y - 10));
                            dc.DrawLine(pn, new Point(P.X + 10, P.Y - 10), new Point(P.X - 10, P.Y + 10));
                        },
                        () => RemoveLastHook());
            }
            else
            {
                RemoveLastHook();
                return null;
            }
        }

        private Action<MouseButtonEventArgs> HookingAct = null;
        private void MoveHook(MouseButtonEventArgs e) => HookingAct?.Invoke(e);

        private Hook CurrentHook;
        private Hook ScanHooks(List<Hook> Hooks, Point P)
        {
            Hook NewHook = null;
            double Min = 999;
            List<Hook> subhooks = new List<Hook>();
            foreach (var h in Hooks) subhooks.AddRange(h.SubHooks);
            foreach (var h in subhooks)
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
            if (NewHook == null)
            {
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
            }
            return NewHook;
        }
        public bool Manipulating = false;
        private Point LastValue;
        private double LastPointPrice;
        private DateTime LastPointTime;
        public void HookElement()
        {
            if (Manipulating) return;
            Task.Run(() => 
            {
                var pn = GetCursorPen.Invoke();
                var P = Chart.CursorPosition.Current;

                var NewHook = ScanHooks(GetVisibleHooks.Invoke(), P);
                if (NewHook != null)
                {
                    RemoveHook = null;

                    if (NewHook != CurrentHook)
                    {
                        CurrentHook?.ClearEvents();

                        Dispatcher.Invoke(() => 
                        {
                            ShadowVisual.RenderOpen().Close();
                            ShadowPriceVisual.RenderOpen().Close();
                            ShadowTimeVisual.RenderOpen().Close();
                        });

                        LastValue = NewHook.GetHookPoint(P);

                        CurrentHook = NewHook;

                        NewHook.DrawOver(null, OverVisual, OverPriceVisual, OverTimeVisual);

                        HookingAct = e =>
                        {
                            if (!Manipulating)
                            {
                                LastValue = NewHook.GetHookPoint(Chart.CursorPosition.Current);
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
                                    NewHook.DrawShadow(ShadowVisual, ShadowPriceVisual, ShadowTimeVisual);
                                    NewHook.DrawOver(null, OverVisual, OverPriceVisual, OverTimeVisual);
                                });
                                ResizeHook = () =>
                                {
                                    P = new Point(Chart.TimeToWidth(LastPointTime), Chart.PriceToHeight(LastPointPrice));
                                    LastValue = NewHook.GetHookPoint(P);

                                    NewHook.DrawShadow(ShadowVisual, ShadowPriceVisual, ShadowTimeVisual);
                                    NewHook.DrawOver(null, OverVisual, OverPriceVisual, OverTimeVisual);

                                    Dispatcher.Invoke(() =>
                                    {
                                        PointVisual.Transform = null;
                                        using var dc = PointVisual.RenderOpen();
                                        dc.DrawLine(pn, new Point(LastValue.X + 10, LastValue.Y + 10), new Point(LastValue.X - 10, LastValue.Y - 10));
                                        dc.DrawLine(pn, new Point(LastValue.X + 10, LastValue.Y - 10), new Point(LastValue.X - 10, LastValue.Y + 10));
                                    });

                                };

                                Manipulating = true;
                                NewHook.DrawShadow(ShadowVisual, ShadowPriceVisual, ShadowTimeVisual);
                                foreach (var l in OtherLayers)
                                    l.Visibility = Visibility.Visible;
                                PointVisual.Transform = null;
                                SetMenu(NewHook.ElementName, NewHook.Sets, () =>
                                {
                                    using var dc = PointVisual.RenderOpen();
                                    dc.DrawLine(pn, new Point(LastValue.X + 10, LastValue.Y + 10), new Point(LastValue.X - 10, LastValue.Y - 10));
                                    dc.DrawLine(pn, new Point(LastValue.X + 10, LastValue.Y - 10), new Point(LastValue.X - 10, LastValue.Y + 10));
                                });

                            }

                            if (!NewHook.Locked)
                            {
                                Chart.MWindow.MoveCursor(e,
                                vec =>
                                {
                                    if (vec.HasValue)
                                    {
                                        vec = Chart.CursorPosition.Magnet_Current - LastValue;
                                        NewHook.DrawOver(vec, OverVisual, OverPriceVisual, OverTimeVisual);
                                        PointVisual.Transform = new TranslateTransform(vec.Value.X, vec.Value.Y);
                                    }
                                    else
                                    {
                                        Manipulating = false;

                                        NewHook.AcceptNewCoordinates();
                                        SetMenu(NewHook.ElementName, NewHook.Sets, () =>
                                        {
                                            using var dc = PointVisual.RenderOpen();
                                            dc.DrawLine(pn, new Point(LastValue.X + 10, LastValue.Y + 10), new Point(LastValue.X - 10, LastValue.Y - 10));
                                            dc.DrawLine(pn, new Point(LastValue.X + 10, LastValue.Y - 10), new Point(LastValue.X - 10, LastValue.Y + 10));
                                        });

                                        LastValue = NewHook.GetHookPoint(Chart.CursorPosition.Current);
                                        LastPointPrice = Chart.HeightToPrice(LastValue.Y);
                                        LastPointTime = Chart.WidthToTime(LastValue.X);
                                        NewHook.DrawShadow(ShadowVisual, ShadowPriceVisual, ShadowTimeVisual);

                                        OverVisual.Transform = null;
                                        OverPriceVisual.Transform = null;
                                        OverTimeVisual.Transform = null;
                                        NewHook.DrawOver(null, OverVisual, OverPriceVisual, OverTimeVisual);
                                    }
                                });
                            }
                            else
                            {
                                Chart.MWindow.MoveCursor(e,
                                vec =>
                                {
                                    if (vec == null)
                                        Manipulating = false;
                                });
                            }
                        };
                        Dispatcher.Invoke(() => 
                        {
                            foreach (var l in OtherLayers)
                                l.Visibility = Visibility.Hidden;
                        });
                    }
                }
                else if (CurrentHook != null)
                {
                    CurrentHook.ClearEvents();
                    RemoveHook = RemoveLastHook;
                    HookingAct = null;
                    RestoreChart();
                }
            });
        }
        public void RemoveLastHook()
        {
            ResizeHook = null;
            Dispatcher.Invoke(() =>
            {
                RemoveHook = null;
                
                SetMenuAct(null, null, null, null);

                RestoreChart();
            });
        }
        public void RestoreChart()
        {
            //if (CurrentHook != null)
            {
                CurrentHook = null;
                Dispatcher.Invoke(() => 
                {
                    foreach (var l in OtherLayers)
                        l.Visibility = Visibility.Visible;

                    ShadowVisual.RenderOpen().Close();
                    ShadowPriceVisual.RenderOpen().Close();
                    ShadowTimeVisual.RenderOpen().Close();
                    OverVisual.RenderOpen().Close();
                    OverPriceVisual.RenderOpen().Close();
                    OverTimeVisual.RenderOpen().Close();
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
        /// <param name="ChangeMethod">Метод передвижения</param>
        /// <param name="GetMagnetRadius">Радиус Зацепления</param>
        public Hook(
            HookElement Element,
            Func<Point, double> GetDistanceXY,
            Func<Point, Point> GetHookPoint,
            Func<double> GetMagnetRadius,
            Action<Vector?> ChangeMethod,
            Action<Vector?, DrawingVisual, DrawingVisual, DrawingVisual, bool> DrawElement,
            Action<DrawingVisual, DrawingVisual, DrawingVisual> DrawShadow,
            Action AcceptNewCoordinates,
            List<Hook> SubHooks = null)
        {
            this.Element = Element;
            this.GetDistanceXY = GetDistanceXY;
            ActionDrawShadow = DrawShadow;
            GetVal = GetHookPoint;
            this.ChangeMethod = ChangeMethod;
            ActionDrawOver = DrawElement;
            this.SubHooks = SubHooks;
            this.MagnetRadius = GetMagnetRadius;
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

        private readonly HookElement Element;
        public bool Locked { get => Element.Locked; }

        public List<Hook> SubHooks { get; }
        public string ElementName { get => Element.ElementName; }
        public List<Setting> Sets { get => Element.GetSettings(); }
        private Func<double> MagnetRadius { get; }
        private Action AcceptChanges { get; }
        private readonly Func<Point, double> GetDistanceXY;
        private readonly Func<Point, Point> GetVal;
        private readonly Action<Vector?> ChangeMethod;
        private readonly Action<Vector?, DrawingVisual, DrawingVisual, DrawingVisual, bool> ActionDrawOver;
        private readonly Action<DrawingVisual, DrawingVisual, DrawingVisual> ActionDrawShadow;

        public double GetMagnetRadius() => MagnetRadius();
        
        public Point GetCurrentPosition(Point CursorPos) => GetVal.Invoke(CursorPos);
        public double GetDistance(Point CursorPoint) => Math.Abs(GetDistanceXY.Invoke(CursorPoint));
        public Point GetHookPoint(Point P) => GetVal.Invoke(P);

        public void AcceptNewCoordinates() => AcceptChanges.Invoke();

        public void DrawShadow(DrawingVisual ShadowVisual, DrawingVisual ShadowPriceVisual, DrawingVisual ShadowTimeVisual) =>
            ActionDrawShadow.Invoke(ShadowVisual, ShadowPriceVisual, ShadowTimeVisual);

        public void DrawOver(Vector? vec, DrawingVisual ShadowVisual, DrawingVisual ShadowPriceVisual, DrawingVisual ShadowTimeVisual)
        {
            ChangeMethod.Invoke(vec);
            ActionDrawOver.Invoke(vec, ShadowVisual, ShadowPriceVisual, ShadowTimeVisual, true);
        }  

        public List<(string Name, Action Act)> GetContextMenu() 
        {
            var Items = Element.GetContextMenu();

            if (Items.Count > 0) Items.Add(("+++", null));
            if (Locked) Items.Add(("Unlock", () => Element.Locked = !Element.Locked));
            else Items.Add(("Lock", () => Element.Locked = !Element.Locked));
            Items.Add(("Delete", Element.Delete));

            return Items;
        }
    }
}