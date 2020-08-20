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
using ChartModules.PaintingModules;
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

        private readonly LevelsModule LevelsModule;
        private readonly TrendsModule TrendsModule;

        private readonly HooksModule HooksModule;

        public ChartView() { } //конструктор для intellisense
        public ChartView(ChartWindow mainView)
        {
            InitializeComponent();

            ResetInstrument = mainView.ResetPB;
            mainView.SetInstrument += SetInsrument;
            mainView.SetMagnet += SetMagnetState;
            this.ShowSettings += mainView.ShowSettings;
            ChartGRD.MouseLeave += (s, e) => CursorLeave?.Invoke();
            ChartGRD.MouseMove += (s, e) => CursorNewPosition?.Invoke(e.GetPosition(this));

            mainView.KeyPressed += e => { if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) GetControl(); };
            mainView.KeyReleased += e => { if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) LoseControl(); };

            PriceMarksModule = new PriceMarksModule(this, LevelsLayer, MarksLayer);

            LevelsModule = new LevelsModule(this, PriceMarksModule.Levels, ResetInstrument);
            TrendsModule = new TrendsModule(this, TrendsLayer, ResetInstrument);

            PriceLineModule = new PriceLineModule(this, PriceLineCD, GridLayer, PricesLayer, PriceMarksModule);
            PriceLineModule.VerticalСhanges += () => VerticalСhanges.Invoke();

            TimeLineModule = new TimeLineModule(this, GridLayer, TimeLine);
            TimeLineModule.HorizontalСhanges += () => HorizontalСhanges.Invoke();

            CursorModule = new CursorModule(this, ChartGRD, CursorLinesLayer, CursorLayer, MagnetLayer, TimeLine, CursorMarkLayer, CursorLeave);

            CandlesModule = new CandlesModule(this, CandlesLayer, PriceLineModule, TimeLineModule,
                Translate, ScaleX, ScaleY, mainView, TimeLine, PriceLine,
                new Vector(ScaleX.ScaleX, ScaleY.ScaleY));

            HooksModule = new HooksModule(this, mainView, HooksLayer, HookPriceLayer, HookTimeLayer,
                () => CursorModule.MarksPen,
                act => { Instrument = act; },
                new List<FrameworkElement>
                {
                    SubLayers,
                    MarksLayer
                });
            ChartGRD.PreviewMouseDown += (s, e) =>
            {
                e.Handled = true;
                HooksModule.RemoveHook?.Invoke();
                Instrument?.Invoke(e);
            };

            CandlesModule.WhellScalled += () => CursorModule.Redraw();

            var DC = DataContext as ChartViewModel;
            DC.PropertyChanged += DC_PropertyChanged;
            DC.Inicialize();

            SetsDefinition();

            SetInsrument(mainView.CurrentInstrument);
            SetMagnetState(mainView.CurrentMagnetState);




            /////////
            LevelsModule.AddLevel(208.99, Brushes.White, (SolidColorBrush)ChartBackground, Brushes.Yellow, 5, 5, 2);
            LevelsModule.AddLevel(206.95, (SolidColorBrush)ChartBackground, Brushes.Azure, Brushes.Azure, 4, 6, 3);
            LevelsModule.AddLevel(204.90, Brushes.Lime, (SolidColorBrush)ChartBackground, Brushes.Lime, 3, 7, 4);
            //////////
        }

        public void Destroy()
        {
            this.PriceMarksModule.Restruct();
            this.CursorModule.Restruct();
            this.TimeLineModule.Restruct();
            this.PriceLineModule.Restruct();
            this.CandlesModule.Restruct();
            this.LevelsModule.Restruct();
            this.TrendsModule.Restruct();

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

        #region Instrument
        private Action<MouseButtonEventArgs> Instrument;
        private bool MagnetInstrument = false;
        private readonly Action<string> ResetInstrument;
        private bool Interaction = false;
        private bool Painting = false;
        private void SetInsrument(string InstrumentName)
        {
            Task.Run(() =>
            {
                CursorT t = CursorT.None;
                switch (InstrumentName)
                {
                    case "PaintingLevels":
                        Instrument = LevelsModule.PaintingLevel; Painting = true;
                        MagnetInstrument = true; t = CursorT.Paint; break;

                    case "PaintingTrends":
                        Instrument = TrendsModule.PaintingElement; Painting = true;
                        MagnetInstrument = true; t = CursorT.Paint; break;

                    case "Interacion":
                        Instrument = null; Painting = false;
                        MagnetInstrument = true; t = CursorT.Hook; break;

                    default:
                        Instrument = CandlesModule.MovingChart; Painting = false;
                        MagnetInstrument = false; t = CursorT.Standart; break;
                }
                if (InstrumentName == "Interacion")
                {
                    ChartGRD.MouseMove += HooksModule.HookElement;
                    Interaction = true;
                }
                else
                {
                    ChartGRD.MouseMove -= HooksModule.HookElement;
                    HooksModule.RemoveHook = HooksModule.RemoveLastHook;
                    HooksModule.RestoreChart();
                    Interaction = false;
                }

                CursorModule.SetCursor(t);
                SetMagnetState(CurrentMagnetState);
            });
        }
        #endregion
        #region Control
        public bool Controlled { get; private set; } = false;
        public bool ControlUsed { get; set; }
        private void GetControl()
        {
            if (!Controlled)
            {
                Task.Run(() =>
                {
                    if (Instrument == CandlesModule.MovingChart)
                    {
                        ResetInstrument.Invoke("Interacion");
                        Controlled = true;
                    }
                    if (Painting)
                    {
                        Controlled = true;
                        ControlUsed = false;
                    }
                });
            }
            
        }
        private void LoseControl()
        {
            if (Controlled) Task.Run(() =>
            {
                if (Interaction || ControlUsed) ResetInstrument.Invoke(null);
                Controlled = false;
            });
        }
        #endregion
        #region Magnet
        private bool CurrentMagnetState;
        private void SetMagnetState(bool st) => Task.Run(() =>
        {
            CurrentMagnetState = st;
            if (MagnetInstrument && CurrentMagnetState)
            {
                CursorModule.MagnetAdd();
                CandlesModule.MagnetStatus = true;
                CandlesModule.UpdateMagnetData();
            }
            else
            {
                CursorModule.MagnetRemove();
                CandlesModule.MagnetStatus = false;
                CandlesModule.ResetMagnetData();
            }
        });
        #endregion

        private int ChangesCounter = 0;
        private void ChartGRD_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Task.Run(() =>
            {
                ChHeight = ChartGRD.ActualHeight;
                ChWidth = ChartGRD.ActualWidth;
                ChangesCounter += 1;
                var x = ChangesCounter;
                Thread.Sleep(50);
                if (x != ChangesCounter) return;

                CandlesModule.HorizontalReset(e.HeightChanged);
                if (CurrentMagnetState) CandlesModule.ResetMagnetData();
            });
        }
        private void MouseWheelSpinning(object sender, MouseWheelEventArgs e) => CandlesModule.WhellScalling(e);

        public double TickSize { get; private set; } = 0.00000001;
        public double ChHeight { get; private set; }
        public double ChWidth { get; private set; }
        public string TickPriceFormat { get; private set; }

        public List<IHooksModule> HooksModules { get; } = new List<IHooksModule>();
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
        public double HeightToPrice(double height) => 
            Math.Round(PricesMin * TickSize + PricesDelta * (ChHeight * TickSize - TickSize * height) / ChHeight, digits);
        public double PriceToHeight(double price) =>
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
        public double TimeToWidth(DateTime dt) =>
            ChWidth * ((dt - TimeA) / (TimeB - TimeA));
        public DateTime WidthToTime(double width) =>
            TimeA + ((width - 2) / ChWidth) * (TimeB - TimeA);

        private event Action<List<(string SetsName, List<Setting> Sets)>, 
                             List<(string SetsName, List<Setting> Sets)>, 
                             List<(string SetsName, List<Setting> Sets)>> ShowSettings;
        private void ShowBaseSettings(object sender, RoutedEventArgs e)
        {
            var basesets = new List<(string SetsName, List<Setting> Sets)>();
            var normalsets = new List<(string SetsName, List<Setting> Sets)>();
            var transsets = new List<(string SetsName, List<Setting> Sets)>();

            basesets.Add(("Настройки поля", SpaceSets));
            basesets.Add(CandlesModule.GetSets());
            basesets.Add(CursorModule.GetSets());

            var s = LevelsModule.GetSets();
            normalsets.Add(s);

            ShowSettings.Invoke(basesets, normalsets, transsets);
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