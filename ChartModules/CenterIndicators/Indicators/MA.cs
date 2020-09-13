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
using System.Windows.Media;

namespace ChartModules.CenterIndicators.Indicators
{
    public class MA : CenterIndicator
    {
        public override string ElementName => "MA";

        public override bool VisibilityOnChart => throw new NotImplementedException();

        public override List<(string Name, Action Act)> GetContextMenu()
        {
            throw new NotImplementedException();
        }

        public override double GetMagnetRadius()
        {
            throw new NotImplementedException();
        }

        public override Action<DrawingContext>[] PrepareToDrawing(Vector? vec, double PixelsPerDip, bool DrawOver = false)
        {
            throw new NotImplementedException();
        }

        private protected override void DrawShadow(DrawingVisual ElementsVisual, DrawingVisual PricesVisual, DrawingVisual TimesVisual)
        {
            throw new NotImplementedException();
        }

        private protected override double GetDistance(Point P)
        {
            throw new NotImplementedException();
        }

        private protected override Point GetHookPoint(Point P)
        {
            throw new NotImplementedException();
        }

        private protected override List<Setting> GetIndSets()
        {
            throw new NotImplementedException();
        }
    }
}
