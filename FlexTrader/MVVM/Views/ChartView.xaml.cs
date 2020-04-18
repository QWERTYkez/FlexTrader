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

using FlexTrader.MVVM.ViewModels;
using ChartModules;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ChartModules.StandardModules;

namespace FlexTrader.MVVM.Views
{
    public partial class ChartView : UserControl, IChart
    {
        private readonly PriceMarksModule PriceMarksModule;
        private readonly CursorModule CursorModule;
        private readonly TimeLineModule TimeLineModule;
        private readonly PriceLineModule PriceLineModule;
        private readonly CandlesModule CandlesModule;

        private readonly List<ChartModule> ModulesNormal = new List<ChartModule>();
        private readonly List<ChartModule> ModulesTransformed = new List<ChartModule>();

        public ChartView() { } //конструктор для intellisense
        public ChartView(ChartWindow mainView)
        {
            InitializeComponent();

            this.ShowSettings += mainView.ShowSettings;

            PriceLineModule = new PriceLineModule(this, PriceLineCD, GridLayer, PriceLine);
            TimeLineModule = new TimeLineModule(this, GridLayer, TimeLine);
            PriceMarksModule = new PriceMarksModule(this, MarksLayer, PriceLine);
            CursorModule = new CursorModule(this, ChartGRD, CursorLayer, TimeLine, PriceLine);

            CandlesModule = new CandlesModule(this, CandlesLayer, PriceLineModule, TimeLineModule, 
                ModulesNormal, Translate, ScaleX, ScaleY, mainView, ChartGRD, TimeLine, PriceLine,
                new Vector(ScaleX.ScaleX, ScaleY.ScaleY));

            ModulesNormal.Add(PriceMarksModule);

            var DC = DataContext as ChartViewModel;
            DC.PropertyChanged += DC_PropertyChanged;
            DC.Inicialize();

            SetsDefinition();
        }
        public void Destroy()
        {
            this.PriceLineModule.Restruct();
            this.TimeLineModule.Restruct();
            this.PriceMarksModule.Restruct();
            this.CursorModule.Restruct();
            this.CandlesModule.Restruct();

            foreach (var m in ModulesTransformed) m.Restruct();
            foreach (var m in ModulesNormal) m.Restruct();

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
                            this.PriceMarksModule.Marks.Clear();
                            foreach (var m in DC.Marks)
                                this.PriceMarksModule.Marks.Add(m);
                        }
                        break;
                    case "BaseFontSize": BaseFontSize = DC.BaseFontSize; break;
                    case "FontBrush": FontBrush = DC.FontBrush; break;
                    case "TickSize":
                        {
                            TickSize = DC.TickSize;
                            TickPriceFormat = TickSize.ToString().Replace('1', '0').Replace(',', '.');
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

        private int ChangesCounter = 0;
        private void ChartGRD_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Task.Run(() =>
            {
                ChHeight = ChartGRD.ActualHeight;
                ChWidth = ChartGRD.ActualWidth;
                ChangesCounter += 1;
                var x = ChangesCounter;
                Thread.Sleep(100);
                if (x != ChangesCounter) return;
                CandlesModule.HorizontalReset();
            });
        }
        private void MouseWheelSpinning(object sender, MouseWheelEventArgs e) => CandlesModule.WhellScalling(e);

        public double TickSize { get; private set; } = 0.00000001;
        public double ChHeight { get; private set; }
        public double ChWidth { get; private set; }
        public string TickPriceFormat { get; private set; }

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

        public double PriceShift { get => 6; }

        public Typeface FontNumeric { get; } = new Typeface(new FontFamily("Agency FB"),
                FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        public Typeface FontText { get; } = new Typeface(new FontFamily("Myriad Pro Cond"),
                FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        public double HeightToPrice(double height) =>
            PricesMin * TickSize + PricesDelta * (ChHeight * TickSize - TickSize * height) / ChHeight;
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

            foreach (var mn in ModulesNormal)
                normalsets.Add(mn.GetSets());

            foreach (var mt in ModulesTransformed)
                normalsets.Add(mt.GetSets());

            ShowSettings.Invoke(basesets, normalsets, transsets);
        }

        private readonly List<Setting> SpaceSets = new List<Setting>();
        public Pen LinesPen { get; private set; } = new Pen(Brushes.DarkGray, 1);
        private double basefontsize;
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
                    (DataContext as ChartViewModel).ChartBackground = br;
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

            SpaceSets.Add(new Setting(SetType.Brush, "Фон", ChartBackground, SetChartBackground, new SolidColorBrush(Color.FromRgb(30, 30, 30))));
            Setting.SetsLevel(SpaceSets, "Сетка", new Setting[] 
            {
                new Setting(SetType.Brush, "Цвет", LinesPen.Brush, SetGridBrush, Brushes.DarkGray),
                new Setting(SetType.DoubleSlider, "Толщина", LinesPen.Thickness * 10, SetGridThicknesses, 10d, 1d, 20d)
            });
            Setting.SetsLevel(SpaceSets, "Текст", new Setting[]
            {
                new Setting(SetType.Brush, "Цвет", FontBrush, SetFontBrush, Brushes.White),
                new Setting(SetType.DoubleSlider, "Размер", BaseFontSize, SetBaseFontSize, 18d, 10d, 40d)
            });
        }
    }
}