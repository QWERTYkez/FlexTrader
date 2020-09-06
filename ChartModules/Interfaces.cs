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

using ChartModules.PaintingModule;
using ChartModules.StandardModules;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static ChartModules.StandardModules.CandlesModule;

namespace ChartModules
{
    public interface IChartWindow
    {
        public void MoveCursor(MouseButtonEventArgs e, Action<Vector?> ActA, Action ActB = null);
        public void ShowSettings(List<(string SetsName, List<Setting> Sets)> sb,
                                 List<(string SetsName, List<Setting> Sets)> sn,
                                 List<(string SetsName, List<Setting> Sets)> st);

        public void SetMenu(string SetsName, List<Setting> Sets, Action DrawHook = null, Action RemoveHook = null);
        public void ShowContextMenu((List<(string Name, Action Act)> Items, Action DrawHook, Action RemoveHook) Menu);
        public void ResetInstrument(string Name);
        public IHaveInstruments InstrumentsHandler { get; set; }
        public event Action ClearPrototypes;
        public event Action<PInstrument> PrepareInstrument;
        public event Action<CursorT> SetCursor;
        public event Action RemoveHooks;
        public event Action NonInteraction;
        public event Action<bool> ToggleMagnet;
        public Action MMInstrument { get; }
        public bool Controlled { get; }
        public bool ControlUsed { get; set; }
    }

    public interface IHaveInstruments
    {
        public Action<MouseButtonEventArgs> Interacion { get; set; }
        public Action<MouseButtonEventArgs> Moving { get; set; }
        public Action<MouseButtonEventArgs> PaintingLevel { get; set; }
        public Action<MouseButtonEventArgs> PaintingTrend { get; set; }

        public Action HookElement { get; set; }
        public Action DrawPrototype { get; set; }
    }

    public interface IChart: IHaveInstruments
    {
        public bool Manipulating { get; }
        public IChartWindow MWindow { get; }
        public Grid ChartGrid { get; }
        public List<Point> PaintingPoints { get; set; }
        Dispatcher Dispatcher { get; }
        double TickSize { get; }
        double PriceToHeight(in double price);
        double HeightToPrice(in double height);
        double TimeToWidth(in DateTime dt);
        DateTime WidthToTime(in double width);
        DateTime CorrectTimePosition(ref double X);
        DateTime CorrectTimePosition(ref Point pos);
        DateTime TimeA { get; }
        DateTime TimeB { get; }
        DateTime StartTime { get; }
        TimeSpan DeltaTime { get; }
        Vector CurrentTranslate { get; }
        Vector CurrentScale { get; }
        public List<MagnetPoint> MagnetPoints { get; }
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
        event Action VerticalСhanges;
        event Action HorizontalСhanges;
        CursorPosition CursorPosition { get; }
        public List<ICandle> AllCandles { get; }
        public event Action<List<ICandle>> CandlesChanged;
        public event Action<IEnumerable<ICandle>> AllHorizontalReset;
        public event Action<double> NewXScale;
        public event Action<double> NewXTrans;
        public string FSF { get; }
        public event Action<string> NewFSF;
        public Brush CandleBrushUp { get; }
        public Brush CandleBrushDown { get; }
        public Brush CursorFontBrush { get; }
        public Pen CursorMarksPen { get; }
        public bool CursorHide { get; }
        public DrawingVisual CursorLinesVisual { get; }
        public DrawingVisual CursorVisual { get; }
        public Action<MouseButtonEventArgs> MovingChart { get; }
        public int digits { get; }
    }
}