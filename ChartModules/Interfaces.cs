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

using ChartModules.CenterIndicators;
using ChartModules.StandardModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        void MoveElement(MouseButtonEventArgs e, Action<Vector> ActA, Action ActB = null);
        void EndMoving(Dispatcher Dispatcher);
        void EndMoving();

        void MoveElements(MouseButtonEventArgs e, List<Func<Vector, Task>> ActsA, List<Func<Task>> ActsB = null);
        void EndMovings(Dispatcher Dispatcher);
        void EndMovings();

        void ShowSettings(List<(string SetsName, List<Setting> Sets)> sb,
                                 List<(string SetsName, List<Setting> Sets)> sn,
                                 List<(string SetsName, List<Setting> Sets)> st);

        void SetMenu(string SetsName, List<Setting> Sets, Action DrawHook = null, Action RemoveHook = null);
        void ShowContextMenu((List<(string Name, Action Act)> Items, Action DrawHook, Action RemoveHook) Menu);
        void ResetInstrument(string Name);
        IHaveInstruments InstrumentsHandler { get; set; }
        List<IClipCandles> Candles { get; }
        ILookup<TimeSpan, IClipCandles> ClipsCandles { get; }
        void ResetClips();
        void ChartGotFocus(IChart sender);

        event Action ClearPrototypes;
        event Action<PInstrument> PrepareInstrument;
        event Action<CursorT> SetCursor;
        event Action RemoveHooks;
        event Action<bool> ToggleInteraction;
        event Action<bool> ToggleMagnet;
        event Action<bool> ToggleClipTime;
        Action MMInstrument { get; }
        bool Controlled { get; }
        bool ControlUsed { get; set; }
    }

    public interface IHaveInstruments
    {
        Action<MouseButtonEventArgs> Interaction { get; set; }
        Action<MouseButtonEventArgs> Moving { get; set; }
        Action<MouseButtonEventArgs> PaintingLevel { get; set; }
        Action<MouseButtonEventArgs> PaintingTrend { get; set; }

        Action HookElement { get; set; }
        Action DrawPrototype { get; set; }

        bool Selected { set; }
    }

    public interface IClipCandles
    {
        TimeSpan DeltaTime { get; }
        void ResetTimeScale();
        double LastScaleX { get; set; }
        Task TimeScaling(Vector vec);
        Vector LastTranslateVector { get; set; }
        Task MovingChart(Vector vec);
        Task WhellScalling(MouseWheelEventArgs e);
    }

    public interface IChart : IHaveInstruments
    {
        bool Clipped { get; }
        bool Manipulating { get; }
        IChartWindow MWindow { get; }
        Grid ChartGrid { get; }
        List<Point> PaintingPoints { get; set; }
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
        List<MagnetPoint> MagnetPoints { get; }
        DrawingCanvas CursorLinesLayer { get; }
        DrawingCanvas TimesLayer { get; }
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
        List<ICandle> AllCandles { get; }
        event Action<List<ICandle>> CandlesChanged;
        event Action<IEnumerable<ICandle>> AllHorizontalReset;
        event Action<double> NewXScale;
        event Action<double> NewXTrans;
        string FSF { get; }
        event Action<string> NewFSF;
        Brush CandleBrushUp { get; }
        Brush CandleBrushDown { get; }
        Brush CursorFontBrush { get; }
        Pen CursorMarksPen { get; }
        bool CursorHide { get; }
        DrawingVisual CursorLinesVisual { get; }
        DrawingVisual CursorVisual { get; }
        Action<object, MouseEventArgs> SetMoving { get; }
        Action<object, MouseEventArgs> BreakMoving { get; }
        int Digits { get; }
    }

    public interface IHooksContainer
    {
        List<Hook> VisibleHooks { get; }
    }
}