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

using ChartModules.BottomIndicators.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChartModules.BottomIndicators
{
    public class BottomIndicatorsManger
    {
        private readonly IChart Chart;
        private readonly Grid IndicatorsGrid;
        private readonly RowDefinition IndicatorsRow;
        private readonly RowDefinition IndicatorsSplitter;
        public BottomIndicatorsManger(IChart Chart, Grid IndicatorsGrid, 
            RowDefinition IndicatorsRowRD, RowDefinition IndicatorsSplitterRD)
        {
            this.Chart = Chart;
            this.IndicatorsGrid = IndicatorsGrid;
            this.IndicatorsRow = IndicatorsRowRD;
            this.IndicatorsSplitter = IndicatorsSplitterRD;

            IndicatorsRow.Height = new GridLength(0);
            IndicatorsSplitter.Height = new GridLength(0);

            ///////////
            AddIndicator(new Volumes(Chart));
            AddIndicator(new MACD(Chart));
        }

        private readonly List<Indicator> Indicators = new List<Indicator>();

        private readonly List<RowDefinition> SliderRows = new List<RowDefinition>();
        private readonly List<RowDefinition> IndicatorRows = new List<RowDefinition>();
        private readonly List<Grid> BaseGrds = new List<Grid>();
        private readonly List<Grid> ScaleGrds = new List<Grid>();
        private readonly List<GridSplitter> Splitters = new List<GridSplitter>();
        public void AddIndicator(Indicator Indicator)
        {
            var i = Indicators.Count * 2;

            if (i > 0)
            {
                var Rd1 = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
                IndicatorsGrid.RowDefinitions.Add(Rd1); SliderRows.Add(Rd1);

                var splitter = new GridSplitter
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(2),
                    ShowsPreview = false
                };
                Grid.SetRow(splitter, i - 1);
                Grid.SetColumnSpan(splitter, 3);
                IndicatorsGrid.Children.Add(splitter); Splitters.Add(splitter);
            }
            else 
            {
                SliderRows.Add(new RowDefinition());
                var splitter = new GridSplitter(); Splitters.Add(splitter);

                IndicatorsRow.Height = new GridLength(1, GridUnitType.Star);
                IndicatorsSplitter.Height = new GridLength(1, GridUnitType.Auto);
            }

            var Rd2 = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
            IndicatorsGrid.RowDefinitions.Add(Rd2); IndicatorRows.Add(Rd2);

            Indicator.SetRow(i);
            IndicatorsGrid.Children.Add(Indicator.BaseGrd); BaseGrds.Add(Indicator.BaseGrd);
            IndicatorsGrid.Children.Add(Indicator.ScaleGrd); ScaleGrds.Add(Indicator.ScaleGrd);
            
            Indicator.Delete += DeleteIndicator;
            Indicator.Moving += MoveIndicator;
            Indicators.Add(Indicator);
        }
        private void DeleteIndicator(Indicator indicator)
        {
            int i = Indicators.IndexOf(indicator);
            if (i + 1 < Indicators.Count)
            {
                for (int n = i + 1; n < Indicators.Count; n++)
                {
                    if(n != 1)
                    {
                        Grid.SetRow(Splitters[n], (n - 1) * 2 - 1);
                    }
                    Grid.SetRow(BaseGrds[n], (n - 1) * 2);
                    Grid.SetRow(ScaleGrds[n], (n - 1) * 2);
                }
            }

            var sl = SliderRows.Last();
            IndicatorsGrid.RowDefinitions.Remove(sl); SliderRows.Remove(sl);
            var spl = Splitters.Last();
            IndicatorsGrid.Children.Remove(spl); Splitters.Remove(spl);

            var ir = IndicatorRows.Last();
            IndicatorsGrid.RowDefinitions.Remove(ir); IndicatorRows.Remove(ir);
            IndicatorsGrid.Children.Remove(BaseGrds[i]); BaseGrds.RemoveAt(i);
            IndicatorsGrid.Children.Remove(ScaleGrds[i]); ScaleGrds.RemoveAt(i);
            Indicators.RemoveAt(i);

            if (Indicators.Count == 0)
            {
                IndicatorsRow.Height = new GridLength(0);
                IndicatorsSplitter.Height = new GridLength(0);
            }
        }
        private void MoveIndicator(Indicator indicator, int i)
        {
            if (i > 0)
            {
                i = Indicators.IndexOf(indicator); if (i == 0) return;

                Grid.SetRow(BaseGrds[i], (i - 1) * 2); Grid.SetRow(ScaleGrds[i], (i - 1) * 2);
                Grid.SetRow(BaseGrds[i - 1], i * 2); Grid.SetRow(ScaleGrds[i - 1], i * 2);

                var bg = BaseGrds[i]; 
                BaseGrds.Remove(bg);  
                BaseGrds.Insert(i - 1, bg);  

                var sg = ScaleGrds[i];
                ScaleGrds.Remove(sg);
                ScaleGrds.Insert(i - 1, sg);

                Indicators.Remove(indicator);
                Indicators.Insert(i - 1, indicator);
            }
            else
            {
                i = Indicators.IndexOf(indicator); if (i == Indicators.Count - 1) return;

                Grid.SetRow(BaseGrds[i], (i + 1) * 2); Grid.SetRow(ScaleGrds[i], (i + 1) * 2);
                Grid.SetRow(BaseGrds[i + 1], i * 2); Grid.SetRow(ScaleGrds[i + 1], i * 2);

                var bg = BaseGrds[i];
                BaseGrds.Remove(bg);
                BaseGrds.Insert(i + 1, bg);

                var sg = ScaleGrds[i];
                ScaleGrds.Remove(sg);
                ScaleGrds.Insert(i + 1, sg);

                Indicators.Remove(indicator);
                Indicators.Insert(i + 1, indicator);
            }
        }
    }
}