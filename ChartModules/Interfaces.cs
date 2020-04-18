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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ChartModules
{
    public interface IChart
    {
        Dispatcher Dispatcher { get; }
        double TickSize { get; }
        double PriceToHeight(double price);
        double HeightToPrice(double height);
        double TimeToWidth(DateTime dt);
        DateTime WidthToTime(double width);
        DateTime CorrectTimePosition(ref double X);
        DateTime CorrectTimePosition(ref Point pos);
        DateTime TimeA { get; }
        DateTime TimeB { get; }
        DateTime? StartTime { get; }
        TimeSpan? DeltaTime { get; }
        Vector CurrentTranslate { get; }
        Vector CurrentScale { get; }
        Brush ChartBackground { get; }
        double PriceShift { get; }
        double ChWidth { get; }
        double ChHeight { get; }
        double PriceLineWidth { get; }
        string TickPriceFormat { get; }
        Typeface FontNumeric { get; }
        Typeface FontText { get; }
        double BaseFontSize { get; }
        double PricesMin { get; }
        double PricesDelta { get; }
        Pen LinesPen { get; }
        Brush FontBrush { get; }
        event Action FontBrushChanged;
    }

    public interface IDrawingCanvas
    {
        public List<Visual> Visuals { get; }
        public void AddVisual(Visual visual);
        public void DeleteVisual(Visual visual);
        public void AddVisualsRange(List<Visual> visuals);
        public void ClearVisuals();

        public int Index { get; }

        public double Width { get; set; }
        public double Height { get; set; }
        Transform RenderTransform { get; set; }
        public event MouseButtonEventHandler PreviewMouseDown;
    }

    public interface IChartWindow
    {
        public event Action<Vector, int> Moving;
        public void StartMoveCursor(MouseButtonEventArgs e, int t);
        public abstract Grid BaseGrid { get; }
        public void ShowSettings(List<(string SetsName, List<Setting> Sets)> sb,
                                 List<(string SetsName, List<Setting> Sets)> sn,
                                 List<(string SetsName, List<Setting> Sets)> st);
    }
}