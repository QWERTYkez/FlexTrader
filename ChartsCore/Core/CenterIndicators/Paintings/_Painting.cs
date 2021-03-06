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
using System.Windows.Media;

namespace ChartsCore.Core.CenterIndicators.Paintings
{
    public abstract class Painting : HookElement
    {
        public Painting()
        {
            Sets.Add(new Setting(() => this.Locked, b => this.Locked = b));

            PriceVisual = new DrawingVisual();
            TimeVisual = new DrawingVisual();
        }

        public override List<(string Name, Action Act)> GetContextMenu()
        {
            (string Name, Action Act) Lock;
            if (Locked) Lock = ("Unlock", () => Locked = !Locked);
            else Lock = ("Lock", () => Locked = !Locked);

            return new List<(string Name, Action Act)>
            {
                ("ToFront", ToFront),
                ("ToBack", ToBack),
                ("+++", null),
                Lock,
                ("+++", null),
                ("Delete", Delete)
            };
        }
    }
}
