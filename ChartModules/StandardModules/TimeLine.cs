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
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ChartModules.StandardModules
{
    public class TimeLineModule : ChartModule
    {
        public DateTime TimeA = DateTime.FromBinary(0);
        public DateTime TimeB = DateTime.FromBinary(0);

        private readonly DrawingCanvas GridLayer;
        public TimeLineModule(IChart chart, DrawingCanvas GridLayer) : base(chart)
        {
            this.GridLayer = GridLayer;

            Chart.FontBrushChanged += () => Redraw();
            GridLayer.AddVisual(TimeGridVisual);
            Chart.TimesLayer.AddVisual(TimesVisual);
        }
        private protected override string SetsName { get; }
        private Pen LinesPen => Chart.LinesPen;
        private double YearFontSize => Math.Round(Chart.BaseFontSize * 1.4);

        private readonly DrawingVisual TimesVisual = new DrawingVisual();
        private readonly DrawingVisual TimeGridVisual = new DrawingVisual();
        private protected override void Destroy()
        {
            Chart.FontBrushChanged -= () => Redraw();
            GridLayer.RemoveVisual(TimeGridVisual);
            Chart.TimesLayer.RemoveVisual(TimesVisual);
        }

        public event Action HorizontalСhanges;
        public Task Redraw()
        {
            if (Chart.ChWidth == 0) return null;
            return Task.Run(() =>
            {
                TimeA = Chart.StartTime - ((Chart.ChWidth / Chart.CurrentScale.X + Chart.CurrentTranslate.X - 7.5) / 15) * Chart.DeltaTime;
                TimeB = Chart.StartTime - ((Chart.CurrentTranslate.X - 7.5) / 15) * Chart.DeltaTime;

                double count = Math.Floor((Chart.ChWidth / (Chart.BaseFontSize * 10)));
                if (count == 0) return;
                var step = (Chart.TimeB - Chart.TimeA) / count;
                int Ystep = 0; int Mstep = 0; int Dstep = 0; int Hstep = 0; int Mnstep = 0;

                if (step.Days > 3650) { Ystep = 10; }
                else if (step.Days > 3285) { Ystep = 9; }
                else if (step.Days > 2920) { Ystep = 8; }
                else if (step.Days > 2555) { Ystep = 7; }
                else if (step.Days > 2190) { Ystep = 6; }
                else if (step.Days > 1825) { Ystep = 5; }
                else if (step.Days > 1460) { Ystep = 4; }
                else if (step.Days > 1095) { Ystep = 3; }
                else if (step.Days > 730) { Ystep = 2; }
                else if (step.Days > 365) { Ystep = 1; }
                else if (step.Days > 240) { Mstep = 8; }
                else if (step.Days > 180) { Mstep = 6; }
                else if (step.Days > 120) { Mstep = 4; }
                else if (step.Days > 90) { Mstep = 3; }
                else if (step.Days > 60) { Mstep = 2; }
                else if (step.Days > 30) { Mstep = 1; }
                else if (step.Days > 15) { Dstep = 15; }
                else if (step.Days > 10) { Dstep = 10; }
                else if (step.Days > 6) { Dstep = 6; }
                else if (step.Days > 5) { Dstep = 5; }
                else if (step.Days > 3) { Dstep = 3; }
                else if (step.Days > 2) { Dstep = 2; }
                else if (step.Days > 1) { Dstep = 1; }
                else if (step.TotalHours > 16) { Hstep = 16; }
                else if (step.TotalHours > 12) { Hstep = 12; }
                else if (step.TotalHours > 8) { Hstep = 8; }
                else if (step.TotalHours > 6) { Hstep = 6; }
                else if (step.TotalHours > 4) { Hstep = 4; }
                else if (step.TotalHours > 3) { Hstep = 3; }
                else if (step.TotalHours > 2) { Hstep = 2; }
                else if (step.TotalHours > 1) { Hstep = 1; }
                else if (step.TotalMinutes > 30) { Mnstep = 30; }
                else if (step.TotalMinutes > 15) { Mnstep = 15; }
                else if (step.TotalMinutes > 10) { Mnstep = 10; }
                else if (step.TotalMinutes > 5) { Mnstep = 5; }
                else if (step.TotalMinutes > 4) { Mnstep = 4; }
                else if (step.TotalMinutes > 2) { Mnstep = 2; }
                else if (step.TotalMinutes > 1) { Mnstep = 1; }

                var pixelsPerDip = VisualTreeHelper.GetDpi(TimesVisual).PixelsPerDip;
                var timesToDraw = new List<(FormattedText Text,
                    Point Tpoint, Point A, Point B, Point G, Point H)>();

                var stTime = new DateTime(Chart.TimeA.Year, 1, 1);
                var Y = Chart.StartTime.Year;
                if (Ystep > 0)
                {
                    var Yn = Y;
                    while (Yn > Chart.TimeA.Year) Yn -= Ystep;
                    Yn += Ystep;
                    while (Yn < Chart.TimeB.Year)
                    {
                        AddYear(timesToDraw, Yn, pixelsPerDip);
                        Yn += Ystep;
                    }
                }
                else
                {
                    var M = Chart.StartTime.Month;
                    if (Mstep > 0)
                    {
                        var currentDT = new DateTime(Y, M, 1);
                        while (currentDT > Chart.TimeA) currentDT = currentDT.AddMonths(-Mstep);
                        currentDT = currentDT.AddMonths(Mstep);
                        while (currentDT <= Chart.TimeB)
                        {
                            AddMounth(timesToDraw, currentDT, pixelsPerDip);
                            currentDT = currentDT.AddMonths(Mstep);
                        }
                    }
                    else
                    {
                        DateTime ndt;
                        var D = Chart.StartTime.Day;
                        if (Dstep > 0)
                        {
                            var currentDT = new DateTime(Chart.TimeA.Year, Chart.TimeA.Month, 1);

                            if (Dstep > 4)
                            {
                                while (currentDT <= Chart.TimeB)
                                {
                                    AddDay(timesToDraw, currentDT, pixelsPerDip);
                                    ndt = currentDT.AddDays(Dstep);
                                    if (ndt.Month != currentDT.Month || ndt.Day > 28)
                                        currentDT = new DateTime(ndt.Year, ndt.Month, 1);
                                    if (ndt.Day > 28)
                                        currentDT = new DateTime(ndt.Year, ndt.Month, 1).AddMonths(1);
                                    else currentDT = ndt;
                                }
                            }
                            else
                            {
                                while (currentDT <= Chart.TimeB)
                                {
                                    AddDay(timesToDraw, currentDT, pixelsPerDip);
                                    ndt = currentDT.AddDays(Dstep);
                                    if (ndt.Month != currentDT.Month)
                                        currentDT = new DateTime(ndt.Year, ndt.Month, 1);
                                    else currentDT = ndt;
                                }
                            }
                        }
                        else
                        {
                            var H = Chart.StartTime.Hour;
                            if (Hstep > 0)
                            {
                                if (Hstep == 16)
                                {
                                    var currentDT = new DateTime(Chart.StartTime.Year, Chart.StartTime.Month, Chart.StartTime.Day, 0, 0, 0);
                                    while (currentDT > Chart.TimeA) currentDT = currentDT.AddHours(-Hstep);
                                    currentDT = currentDT.AddHours(Hstep);
                                    while (currentDT <= Chart.TimeB)
                                    {
                                        AddHourMinute(timesToDraw, currentDT, pixelsPerDip);
                                        currentDT = currentDT.AddHours(Hstep);
                                    }
                                }
                                else
                                {
                                    var currentDT = new DateTime(Chart.TimeA.Year, Chart.TimeA.Month, Chart.TimeA.Day, 0, 0, 0);
                                    while (currentDT <= Chart.TimeB)
                                    {
                                        AddHourMinute(timesToDraw, currentDT, pixelsPerDip);
                                        currentDT = currentDT.AddHours(Hstep);
                                    }
                                }
                            }
                            else
                            {
                                var currentDT = new DateTime(Chart.TimeA.Year, Chart.TimeA.Month, Chart.TimeA.Day, 0, 0, 0);
                                while (currentDT <= Chart.TimeB)
                                {
                                    AddHourMinute(timesToDraw, currentDT, pixelsPerDip);
                                    currentDT = currentDT.AddMinutes(Mnstep);
                                }
                            }
                        }
                    }
                }

                if (timesToDraw.Count > 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        using var tvc = TimesVisual.RenderOpen();
                        using var tgvc = TimeGridVisual.RenderOpen();
                        foreach (var (Text, Tpoint, A, B, G, H) in timesToDraw)
                        {
                            tvc.DrawText(Text, Tpoint);
                            tvc.DrawLine(LinesPen, A, B);
                            tgvc.DrawLine(LinesPen, G, H);
                        }
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        using var tvc = TimesVisual.RenderOpen();
                        using var tgvc = TimeGridVisual.RenderOpen();
                    });
                }
                HorizontalСhanges?.Invoke();
            });
        }
        private void AddYear(dynamic container, int Y, double pixelsPerDip)
        {
            var ft = new FormattedText
                        (
                            Y.ToString(),
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            Chart.FontNumeric,
                            YearFontSize,
                            Chart.FontBrush,
                            pixelsPerDip
                        );
            var width = Chart.TimeToWidth(new DateTime(Y, 1, 1));
            if (width < 0 || width > Chart.ChWidth) return;
            container.Add((ft, new Point(width - ft.Width / 2, Chart.PriceShift),
                new Point(width, 0), new Point(width, Chart.PriceShift),
                new Point(width, 0), new Point(width, 4096)));
        }
        private void AddMounth(dynamic container, DateTime dt, double pixelsPerDip)
        {
            if (dt.Month == 1) AddYear(container, dt.Year, pixelsPerDip);
            else
            {
                var ft = new FormattedText
                        (
                            dt.ToString("MMMM"),
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            Chart.FontText,
                            Chart.BaseFontSize,
                            Chart.FontBrush,
                            pixelsPerDip
                        );
                var width = Chart.TimeToWidth(dt);
                if (width < 0 || width > Chart.ChWidth) return;
                container.Add((ft, new Point(width - ft.Width / 2, Chart.PriceShift),
                    new Point(width, 0), new Point(width, Chart.PriceShift),
                    new Point(width, 0), new Point(width, 4096)));
            }
        }
        private void AddDay(dynamic container, DateTime dt, double pixelsPerDip)
        {
            if (dt.Day == 1) AddMounth(container, dt, pixelsPerDip);
            else
            {
                var ft = new FormattedText
                        (
                            dt.Day.ToString(),
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            Chart.FontNumeric,
                            Chart.BaseFontSize,
                            Chart.FontBrush,
                            pixelsPerDip
                        );
                var width = Chart.TimeToWidth(dt);
                if (width < 0 || width > Chart.ChWidth) return;
                container.Add((ft, new Point(width - ft.Width / 2, Chart.PriceShift),
                    new Point(width, 0), new Point(width, Chart.PriceShift),
                    new Point(width, 0), new Point(width, 4096)));
            }
        }
        private void AddHourMinute(dynamic container, DateTime dt, double pixelsPerDip)
        {
            if (dt.Hour == 0 && dt.Minute == 0) AddDay(container, dt, pixelsPerDip);
            else
            {
                var ft = new FormattedText
                        (
                            dt.ToString("H:mm"),
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            Chart.FontNumeric,
                            Chart.BaseFontSize,
                            Chart.FontBrush,
                            pixelsPerDip
                        );
                var width = Chart.TimeToWidth(dt);
                if (width < 0 || width > Chart.ChWidth) return;
                container.Add((ft, new Point(width - ft.Width / 2, Chart.PriceShift),
                    new Point(width, 0), new Point(width, Chart.PriceShift),
                    new Point(width, 0), new Point(width, 4096)));
            }
        }
    }
}