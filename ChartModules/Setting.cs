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
    public class Setting
    {
        private Setting()
        { 
            this.Type = SetType.GoUp;
        }
        private Setting(string Name)
        {
            this.Name = Name;
            this.Type = SetType.GoDown;
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
        }
        /// <summary>
        /// New Numeric Setting
        /// </summary>
        public Setting(NumericType Type, string Name, Func<double> Get,
                       Action<double> Set, double? Min = null, double? Max = null, double? Standart = null)
        {
            this.Name = Name;
            this.Type = Type switch
            {
                NumericType.Picker => SetType.DoublePicker,
                NumericType.Slider => SetType.DoubleSlider,
                _ => throw new Exception()
            };
            this.Get = () => Get();
            this.Set = o => Set((double)o);
            this.ResetObj = Standart;
            this.Param1 = Min;
            this.Param2 = Max;
        }
        public Setting(NumericType Type, string Name, Func<int> Get,
                       Action<int> Set, int? Min = null, int? Max = null, int? Standart = null)
        {
            this.Name = Name;
            this.Type = Type switch
            {
                NumericType.Picker => SetType.IntPicker,
                NumericType.Slider => SetType.IntSlider,
                _ => throw new Exception()
            };
            this.Get = () => Get();
            this.Set = o => Set((int)o);
            this.ResetObj = Standart;
            this.Param1 = Min;
            this.Param2 = Max;
        }
        /// <summary>
        /// New Lock Setting
        /// </summary>
        public Setting(Func<object> Get, Action<object> Set)
        {
            this.Type = SetType.Lock;
            this.Get = Get;
            this.Set = Set;
        }
        /// <summary>
        /// New Delete Setting
        /// </summary>
        public Setting(Action Set)
        {
            this.Type = SetType.Delete;
            this.Set = o => Set();
        }
        /// <summary>
        /// New Move Setting
        /// </summary>
        public Setting(Action<int> Set)
        {
            this.Type = SetType.Move;
            this.Set = o => Set((int)o);
        }

        private static readonly object key = new object();
        public static void SetsLevel(List<Setting> Sets, string Name, Setting[] args)
        {
            lock(key)
            {
                Sets.Add(new Setting(Name));
                Sets.AddRange(args);
                Sets.Add(new Setting());
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

    public static class SettingsExtension
    {
        public static void AddLevel(this List<Setting> Sets, string Name, Setting[] args) =>
            Setting.SetsLevel(Sets, Name, args);
    }

    public enum SetType
    {
        Brush,
        Delete,
        DoubleSlider,
        DoublePicker,
        GoDown,
        GoUp,
        IntSlider,
        IntPicker,
        Lock,
        Move
    }

    public enum NumericType
    {
        Picker,
        Slider
    }
}
