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

namespace FlexTrader.MVVM.Views.ChartModules
{
    public struct Setting
    {
        public Setting(SetType Type, string Name = null, object Obj = null,
                       Action<object> Set = null, object Param1 = null, object Param2 = null)
        { 
            this.Name = Name;
            this.Type = Type;
            this.Obj = Obj;
            this.Set = Set;
            this.Param1 = Param1;
            this.Param2 = Param2;
        }

        public static void SetsLevel(List<Setting> Sets, string Name, Setting[] args)
        {
            Sets.Add(new Setting(SetType.GoDown, Name));
            Sets.AddRange(args);
            Sets.Add(new Setting(SetType.GoUp));
        }

        public readonly string Name;
        public readonly SetType Type;
        public readonly object Obj;
        public readonly Action<object> Set;
        public readonly object Param1;
        public readonly object Param2;
    }

    public enum SetType
    {
        Brush,
        DoubleSlider,
        GoDown,
        GoUp
    }
}
