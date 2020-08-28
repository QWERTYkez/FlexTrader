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

using ChartModules;
using ChartModules.PaintingModule;
using ChartModules.StandardModules;
using FlexTrader.MVVM.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FlexTrader.MVVM.Views
{
    public partial class ChartView : UserControl, IChart
    {
        private readonly PriceMarksModule PriceMarksModule;
        private readonly CursorModule CursorModule;
        private readonly TimeLineModule TimeLineModule;
        private readonly PriceLineModule PriceLineModule;
        private readonly CandlesModule CandlesModule;

        private readonly PaintingModule PaintingModule;
        private readonly HooksModule HooksModule;

        public IChartWindow MWindow { get; }
        public ChartView() { } //конструктор для intellisense
        public ChartView(IChartWindow mainView)
        {
            this.MWindow = mainView;

            InitializeComponent();
            this.MouseEnter += (s, e) => MWindow.InstrumentsHandler = this;
            this.MouseLeave += (s, e) =>
            {
                if (MWindow.InstrumentsHandler == this)
                    MWindow.InstrumentsHandler = null;
            };
            ChartGrid.MouseLeave += (s, e) => CursorLeave?.Invoke();
            ChartGrid.MouseMove += (s, e) =>
            {
                var P = e.GetPosition(this);
                Task.Run(() => CursorNewPosition?.Invoke(P));
            };

            MWindow.PrepareInstrument += PrepareInstrument;
            this.ShowSettings += MWindow.ShowSettings;
            
            PriceMarksModule = new PriceMarksModule(this, LevelsLayer, PaintingMarksLayer);

            PaintingModule = new PaintingModule(this, PaintingsLayer, PaintingMarksLayer, PaintingTimeLayer,
                PrototypeLayer, PrototypePriceLayer, PrototypeTimeLayer);
            MWindow.ClearPrototypes += PaintingModule.ClearPrototype;

            HooksModule = new HooksModule(this, HooksLayer, HookPriceLayer, HookTimeLayer,
                () => CursorModule.MarksPen,
                () => PaintingModule.VisibleHooks,
                new List<FrameworkElement>
                {
                    SubLayers,
                    PaintingMarksLayer
                });

            PriceLineModule = new PriceLineModule(this, PriceLineCD, GridLayer, PricesLayer, PriceMarksModule);
            PriceLineModule.VerticalСhanges += () => VerticalСhanges.Invoke();

            TimeLineModule = new TimeLineModule(this, GridLayer, TimesLayer);
            TimeLineModule.HorizontalСhanges += () => HorizontalСhanges.Invoke();

            CursorModule = new CursorModule(this, CursorLinesLayer, CursorLayer, MagnetLayer, 
                CursorTimeMarkLayer, CursorPriceMarkLayer, CursorLeave);

            CandlesModule = new CandlesModule(this, CandlesLayer, PriceLineModule, TimeLineModule,
                Translate, ScaleX, ScaleY, TimeLine, PriceLine,
                new Vector(ScaleX.ScaleX, ScaleY.ScaleY));

            ChartGrid.PreviewMouseRightButtonDown += (s, e) =>
            {
                var items = HooksModule.ShowContextMenu(s, e);
                if (items == null)
                {
                    items = (new List<(string Name, Action Act)>()
                    {
                        ("Test 1", () => { Debug.WriteLine("Test 1"); }),
                        ("+++", null),
                        ("Test 2", () => { Debug.WriteLine("Test 2"); }),
                        ("+++", null),
                        ("Test 3", () => { Debug.WriteLine("Test 3"); })
                    },
                    null, null);
                }
                MWindow.ShowContextMenu(items.Value);
            };

            CandlesModule.WhellScalled += () => CursorModule.Redraw();

            var DC = DataContext as ChartViewModel;
            DC.PropertyChanged += DC_PropertyChanged;
            DC.Inicialize();

            SetsDefinition();
        }

        public void Destroy()
        {
            this.PriceMarksModule.Restruct();
            this.CursorModule.Restruct();
            this.TimeLineModule.Restruct();
            this.PriceLineModule.Restruct();
            this.CandlesModule.Restruct();
            this.PaintingModule.Restruct();

            var DC = DataContext as ChartViewModel;
            DC.PropertyChanged -= DC_PropertyChanged;
        }
        private void DC_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Task.Run(() =>
            {
                var DC = sender as ChartViewModel;

                switch (e.PropertyName)
                {
                    case "Marks":
                        {
                            foreach (var m in DC.Marks)
                                this.PriceMarksModule.Levels.AddMark(m);
                        }
                        break;
                    case "BaseFontSize": BaseFontSize = DC.BaseFontSize; break;
                    case "FontBrush": FontBrush = DC.FontBrush; break;
                    case "TickSize":
                        {
                            TickSize = DC.TickSize;
                            TickPriceFormat = TickSize.ToString().Replace('1', '0').Replace(',', '.');
                            digits = TickPriceFormat.ToCharArray().Length - 2;
                        }
                        break;
                    case "NewCandles":
                        {
                            if (DC.NewCandles != null && DC.NewCandles.Count > 0)
                                CandlesModule.AddCandles(DC.NewCandles);
                        }
                        break;
                }
            });
        }

        #region Instruments
        //LBDInstrument
        public Action<MouseButtonEventArgs> Interacion { get; set; }
        public Action<MouseButtonEventArgs> Moving { get; set; }
        public Action<MouseButtonEventArgs> PaintingLevel { get; set; }
        public Action<MouseButtonEventArgs> PaintingTrend { get; set; }
        private void PrepareInstrument(string Instrument)
        {
            Task.Run(() => 
            {
                switch (Instrument)
                {
                    case "PaintingLevels": PaintingModule.PreparePaintingLevel(); return;
                    case "PaintingTrends": PaintingModule.PreparePaintingTrend(); return;
                }
            });
        }
        //MMInstrument
        public Action<MouseEventArgs> HookElement { get; set; }
        #endregion

        public List<Point> PaintingPoints { get; set; }

        private int ChangesCounter = 0;
        private void ChartGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Task.Run(() =>
            {
                ChangesCounter += 1;
                var x = ChangesCounter;
                Thread.Sleep(50);
                if (x != ChangesCounter) return;

                ChHeight = ChartGrid.ActualHeight;
                ChWidth = ChartGrid.ActualWidth;
                CandlesModule.HorizontalReset(e.HeightChanged);
            });
        }
        private void MouseWheelSpinning(object sender, MouseWheelEventArgs e) => CandlesModule.WhellScalling(e);

        public double TickSize { get; private set; } = 0.00000001;
        public double ChHeight { get; private set; }
        public double ChWidth { get; private set; }
        public string TickPriceFormat { get; private set; }

        public List<CandlesModule.MagnetPoint> MagnetPoints { get => CandlesModule.MagnetPoints; }
        public Brush ChartBackground { get => (DataContext as ChartViewModel).ChartBackground; }
        public Vector CurrentTranslate { get => CandlesModule.CurrentTranslate; }
        public Vector CurrentScale { get => CandlesModule.CurrentScale; }
        public DateTime? StartTime { get => CandlesModule.StartTime; }
        public TimeSpan? DeltaTime { get => CandlesModule.DeltaTime; }
        public double PricesDelta { get => PriceLineModule.PricesDelta; }
        public double PricesMin { get => PriceLineModule.PricesMin; }
        public double PriceLineWidth { get => PriceLineModule.PriceLineWidth; }
        public DateTime TimeA { get => TimeLineModule.TimeA; }
        public DateTime TimeB { get => TimeLineModule.TimeB; }
        public Point CurrentCursorPosition { get => CursorModule.CurrentPosition; }
        public Grid ChartGrid { get => ChartGRD; }

        public event Action VerticalСhanges;
        public event Action HorizontalСhanges;
        public event Action<Point> CursorNewPosition;
        public event Action CursorLeave;

        public double PriceShift { get => 6; }

        public Typeface FontNumeric { get; } = new Typeface(new FontFamily("Agency FB"),
                FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        public Typeface FontText { get; } = new Typeface(new FontFamily("Myriad Pro Cond"),
                FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        private int digits = 8;
        public double HeightToPrice(in double height) => 
            Math.Round(PricesMin * TickSize + PricesDelta * (ChHeight * TickSize - TickSize * height) / ChHeight, digits);
        public double PriceToHeight(in double price) =>
            (ChHeight * (PricesDelta * TickSize - price + PricesMin * TickSize)) / (PricesDelta * TickSize);
        public DateTime CorrectTimePosition(ref double X)
        {
            var dt = StartTime.Value -
                Math.Round((StartTime.Value - WidthToTime(X)) / DeltaTime.Value) * DeltaTime.Value;
            X = TimeToWidth(dt);
            return dt;
        }
        public DateTime CorrectTimePosition(ref Point pos)
        {
            var dt = StartTime.Value -
                Math.Round((StartTime.Value - WidthToTime(pos.X)) / DeltaTime.Value) * DeltaTime.Value;
            pos.X = TimeToWidth(dt);
            return dt;
        }
        public double TimeToWidth(in DateTime dt) =>
            ChWidth * ((dt - TimeA) / (TimeB - TimeA));
        public DateTime WidthToTime(in double width)
        {
            if(ChWidth == 0) return TimeA + (TimeB - TimeA) / 2;
            return TimeA + ((width - 2) / ChWidth) * (TimeB - TimeA);
        }
            

        private event Action<List<(string SetsName, List<Setting> Sets)>, 
                             List<(string SetsName, List<Setting> Sets)>, 
                             List<(string SetsName, List<Setting> Sets)>> ShowSettings;
        private void ShowBaseSettings(object sender, RoutedEventArgs e)
        {
            Task.Run(() => 
            {
                var basesets = new List<(string SetsName, List<Setting> Sets)>();
                var normalsets = new List<(string SetsName, List<Setting> Sets)>();
                var transsets = new List<(string SetsName, List<Setting> Sets)>();

                basesets.Add(("Настройки поля", SpaceSets));
                basesets.Add(CandlesModule.GetSets());
                basesets.Add(CursorModule.GetSets());

                var s = PaintingModule.GetSets();
                normalsets.Add(s);

                ShowSettings.Invoke(basesets, normalsets, transsets);
            });
        }

        private readonly List<Setting> SpaceSets = new List<Setting>();
        public Pen LinesPen { get; private set; } = new Pen(Brushes.DarkGray, 1);
        private double basefontsize = 1000;
        public double BaseFontSize
        {
            get => basefontsize;
            private set
            {
                var ft = new FormattedText
                            (
                                "0",
                                CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                FontNumeric,
                                Math.Round(value * 1.4),
                                null,
                                VisualTreeHelper.GetDpi(TimeLine).PixelsPerDip
                            );
                Dispatcher.Invoke(() =>
                {
                    TimeLineRD.Height = new GridLength(PriceShift + ft.Height);
                    TimeLine.Height = PriceShift + ft.Height;
                });

                basefontsize = value;
            }
        }
        public Brush FontBrush { get; private set; }
        public event Action FontBrushChanged;

        private void SetsDefinition()
        {
            var SetGridThicknesses = new Action<object>(b =>
            {
                Dispatcher.Invoke(() => { LinesPen.Thickness = (b as double?).Value / 10; });
                PriceLineModule.Redraw(); TimeLineModule.Redraw();
            });
            var SetGridBrush = new Action<object>(b =>
            {
                Dispatcher.Invoke(() => { LinesPen.Brush = b as Brush; });
                PriceLineModule.Redraw(); TimeLineModule.Redraw();
            });
            var SetChartBackground = new Action<object>(b => 
            { 
                Dispatcher.Invoke(() => 
                { 
                    var br = b as Brush; br.Freeze();
                    (DataContext as ChartViewModel).ChartBackground = (SolidColorBrush)br;
                }); 
            });
            var SetFontBrush = new Action<object>(b => 
            {
                Dispatcher.Invoke(() => 
                {
                    var br = b as Brush; br.Freeze();
                    (DataContext as ChartViewModel).FontBrush = br;
                });
                FontBrushChanged.Invoke(); 
            });
            var SetBaseFontSize = new Action<object>(b => 
            { 
                BaseFontSize = (b as double?).Value; 
            });

            SpaceSets.Add(new Setting("Фон", () => ChartBackground, SetChartBackground, new SolidColorBrush(Color.FromRgb(30, 30, 30))));
            Setting.SetsLevel(SpaceSets, "Сетка", new Setting[] 
            {
                new Setting("Цвет", () => LinesPen.Brush, SetGridBrush, Brushes.DarkGray),
                new Setting(SetType.DoubleSlider, "Толщина", () => LinesPen.Thickness * 10, SetGridThicknesses, 1d, 20d, 10d)
            });
            Setting.SetsLevel(SpaceSets, "Текст", new Setting[]
            {
                new Setting("Цвет", () => FontBrush, SetFontBrush, Brushes.White),
                new Setting(SetType.DoubleSlider, "Размер", () => BaseFontSize, SetBaseFontSize, 10d, 40d, 18d)
            });
        }
    }
}