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
using System.Windows.Media;

namespace ChartModules
{
    public struct Setting
    {
        private Setting(SetType Type)
        { 
            this.Name = null;
            this.Type = Type;
            this.Get = null;
            this.Set = null;
            this.ResetObj = null;
            this.Param1 = null;
            this.Param2 = null;
        }
        /// <summary>
        /// New Level of Setting
        /// </summary>
        public Setting(string Name)
        {
            this.Name = Name;
            this.Type = SetType.GoDown;
            this.Get = null;
            this.Set = null;
            this.ResetObj = null;
            this.Param1 = null;
            this.Param2 = null;
        }
        /// <summary>
        /// New Brush Setting
        /// </summary>
        public Setting(string Name, Func<object> Get,
                       Action<object> Set, SolidColorBrush ResetObj = null)
        {
            this.Name = Name;
            this.Type = SetType.Brush;
            this.Get = Get;
            this.Set = Set;
            this.ResetObj = ResetObj;
            this.Param1 = 0;
            this.Param2 = 0;
        }
        /// <summary>
        /// New Double Setting
        /// </summary>
        public Setting(SetType Type, string Name, Func<object> Get,
                       Action<object> Set, double? Min = null, double? Max = null, double? Standart = null)
        {
            this.Name = Name;
            this.Type = Type;
            this.Get = Get;
            this.Set = Set;
            this.ResetObj = Standart;
            this.Param1 = Min;
            this.Param2 = Max;
        }
        /// <summary>
        /// New Lock Setting
        /// </summary>
        public Setting(Func<object> Get, Action<object> Set)
        {
            this.Name = null;
            this.Type = SetType.Lock;
            this.Get = Get;
            this.Set = Set;
            this.ResetObj = null;
            this.Param1 = null;
            this.Param2 = null;
        }

        private static readonly object key = new object();
        public static void SetsLevel(List<Setting> Sets, string Name, Setting[] args)
        {
            lock(key)
            {
                Sets.Add(new Setting(Name));
                Sets.AddRange(args);
                Sets.Add(new Setting(SetType.GoUp));
            }
        }

        public readonly string Name;
        public readonly SetType Type;
        public readonly Func<object> Get;
        public readonly Action<object> Set;
        public readonly object ResetObj;
        public readonly object Param1;
        public readonly object Param2;
    }

    public enum SetType
    {
        Brush,
        DoubleSlider,
        DoublePicker,
        GoDown,
        GoUp,
        Lock
    }
}
