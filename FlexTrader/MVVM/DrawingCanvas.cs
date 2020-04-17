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

using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace FlexTrader.MVVM
{
    public class DrawingCanvas : Canvas
    {
        public List<Visual> Visuals = new List<Visual>();
        protected override int VisualChildrenCount => Visuals.Count;
        protected override Visual GetVisualChild(int index) => Visuals[index];
        public void AddVisual(Visual visual)
        {
            Visuals.Add(visual);
            base.AddVisualChild(visual);
            base.AddLogicalChild(visual);
        }
        public void DeleteVisual(Visual visual)
        {
            Visuals.Remove(visual);
            base.RemoveVisualChild(visual);
            base.RemoveLogicalChild(visual);
        }
        public void AddVisualsRange(List<Visual> visuals)
        {
            visuals.AddRange(visuals);
            foreach (Visual v in visuals)
            {
                base.AddVisualChild(v);
                base.AddLogicalChild(v);
            }
        }
        public void ClearVisuals()
        {
            foreach (Visual v in Visuals)
            {
                base.RemoveVisualChild(v);
                base.RemoveLogicalChild(v);
            }
            Visuals.Clear();
        }

        public int Index { get; set; }
    }
}