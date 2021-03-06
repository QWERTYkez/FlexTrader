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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ChartsCore.Core.StandardModules
{
    public class PriceMarksModule : ChartModule
    {
        private readonly List<MarksLayer> MarksLayers = new List<MarksLayer>();
        public MarksLayer Levels { get; }

        public PriceMarksModule(View chart, DrawingCanvas MarksCanvas, DrawingCanvas PriceLine) : base(chart)
        {
            this.MarksCanvas = MarksCanvas;
            this.PriceLine = PriceLine;

            #region Слои отметок
            {
                Levels = new MarksLayer(MarksCanvas, PriceLine, RedrawMarks); MarksLayers.Add(Levels);
            }
            #endregion
        }

        private protected override string SetsName { get; }

        private readonly DrawingCanvas MarksCanvas;
        private readonly DrawingCanvas PriceLine;
        private protected override void Destroy()
        {
            MarksCanvas.ClearVisuals();
            PriceLine.ClearVisuals();
        }

        private bool СlearedSpace = true;
        private void RedrawMarks(MarksLayer Layer)
        {
            var marksData = new List<RedrawData>();

            {   // get redraw data
                if (Layer.Marks != null)
                {
                    var pricesMax = (Chart.PricesMin + Chart.PricesDelta) * Chart.TickSize;
                    foreach (var mark in Layer.Marks)
                    {
                        if (mark.Price > Chart.PricesMin * Chart.TickSize && mark.Price < pricesMax)
                        {
                            var height = Chart.PriceToHeight(mark.Price);

                            var ft = new FormattedText
                                            (
                                                mark.Price.ToString(Chart.TickPriceFormat),
                                                CultureInfo.CurrentCulture,
                                                FlowDirection.LeftToRight,
                                                Chart.FontNumeric,
                                                Chart.BaseFontSize,
                                                mark.TextBrush,
                                                VisualTreeHelper.GetDpi(Layer.PriceVisual).PixelsPerDip
                                            );

                            var linpen = new Pen(mark.LineBrush, mark.LineThikness); linpen.Freeze();
                            var geopen = new Pen(mark.LineBrush, 2); geopen.Freeze();

                            var linps = new List<Point>();
                            if (mark.LineIndent == 0)
                            {
                                linps.Add(new Point(0, height));
                                linps.Add(new Point(4096, height));
                            }
                            else
                            {
                                double s = 0;
                                while (s < 4096)
                                {
                                    linps.Add(new Point(s, height)); s += mark.LineDash;
                                    linps.Add(new Point(s, height)); s += mark.LineIndent;
                                }
                            }

                            var geo = new PathGeometry(new[] { new PathFigure(new Point(0, height),
                                    new[]
                                    {
                                        new LineSegment(new Point(Chart.PriceShift, height + ft.Height / 2), true),
                                        new LineSegment(new Point(Chart.PriceLineWidth - 2, height + ft.Height / 2), true),
                                        new LineSegment(new Point(Chart.PriceLineWidth - 2, height - ft.Height / 2), true),
                                        new LineSegment(new Point(Chart.PriceShift, height - ft.Height / 2), true)
                                    },
                                    true)
                                }); geo.Freeze();

                            marksData.Add(new RedrawData(
                                new Point(0, height),
                                new Point(Chart.ChWidth + 2, height),
                                ft, mark.MarkFill,
                                new Point(Chart.PriceShift + 1, height - ft.Height / 2),
                                linpen, linps, geopen, geo));
                        }
                    }
                }
            } // get redraw data

            if (marksData.Count > 0)
            {
                СlearedSpace = false;
                Dispatcher.Invoke(() =>
                {
                    using var dcCH = Layer.ChartVisual.RenderOpen();
                    using var dcP = Layer.PriceVisual.RenderOpen();

                    foreach (var rd in marksData)
                    {
                        for (int i = 0; i < rd.LinPoints.Count; i += 2)
                            dcCH.DrawLine(rd.Linpen, rd.LinPoints[i], rd.LinPoints[i + 1]);

                        dcP.DrawGeometry(rd.Fill, rd.Geopen, rd.Geo);
                        dcP.DrawText(rd.Ft, rd.T);
                    }
                });
            }
            else if (!СlearedSpace)
            {
                СlearedSpace = true;
                Dispatcher.Invoke(() =>
                {
                    Layer.ChartVisual.RenderOpen().Close();
                    Layer.PriceVisual.RenderOpen().Close();
                });
            }
        }
        public Task Redraw()
        {
            var tasks = new List<Task>();
            foreach (var ml in MarksLayers) tasks.Add(Task.Run(() => RedrawMarks(ml)));
            return Task.WhenAll(tasks);
        }

        private struct RedrawData
        {
            public RedrawData(Point A, Point B, FormattedText ft, Brush Fill, Point T, Pen linpen, List<Point> linPoints, Pen geopen, PathGeometry geo)
            {
                this.Ft = ft;
                this.Fill = Fill;
                this.T = T;
                this.Linpen = linpen;
                this.LinPoints = linPoints;
                this.Geopen = geopen;
                this.Geo = geo;
            }

            public FormattedText Ft { get; }
            public Brush Fill { get; }
            public Point T { get; }
            public Pen Linpen { get; }
            public List<Point> LinPoints { get; }
            public Pen Geopen { get; }
            public PathGeometry Geo { get; }
        }
    }

    public struct MarksLayer
    {
        public MarksLayer(DrawingCanvas MarksLayer, DrawingCanvas PriceLine, Action<MarksLayer> act)
        {
            ChartVisual = new DrawingVisual(); MarksLayer.AddVisual(ChartVisual);
            PriceVisual = new DrawingVisual(); PriceLine.AddVisual(PriceVisual);

            var lm = new ObservableCollection<PriceMark>();
            Marks = lm; Act = null;

            var x = this;
            Act = () => act(x);
            lm.CollectionChanged += (s, e) => act(x);
        }
        private readonly Action Act;

        public void AddMark(PriceMark pm) => Marks.Add(pm);
        public void AddMark(double Price, SolidColorBrush TextBrush, SolidColorBrush MarkFill, 
            SolidColorBrush LineBrush = null, double LineThikness = 0, double LineDash = 0, double LineIndent = 0) =>
            Marks.Add(new PriceMark(Price, Act, TextBrush, MarkFill, LineBrush, LineThikness, LineDash, LineIndent));

        public DrawingVisual ChartVisual { get; }
        public DrawingVisual PriceVisual { get; }
        public ObservableCollection<PriceMark> Marks { get; }
    }
    public class PriceMark
    {
        public PriceMark(double Price, Action ApplyChanges, SolidColorBrush TextBrush, SolidColorBrush MarkFill, 
            SolidColorBrush LineBrush = null, double LineThikness = 0, double LineDash = 0, double LineIndent = 0)
        {
            this.Price = Price;
            this.TextBrush = TextBrush;
            this.MarkFill = MarkFill;
            this.LineBrush = LineBrush;
            this.LineDash = LineDash;
            this.LineIndent = LineIndent;
            this.LineThikness = LineThikness;

            this.TextBrush?.Freeze(); this.MarkFill?.Freeze(); this.LineBrush?.Freeze();
        }

        public double Price { get; set; }
        public SolidColorBrush TextBrush { get; set; }
        public SolidColorBrush MarkFill { get; set; }
        public SolidColorBrush LineBrush { get; set; }
        public double LineDash { get; set; }
        public double LineIndent { get; set; }
        public double LineThikness { get; set; }
    }
}