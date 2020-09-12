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
using System.Threading;
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
        /// Brush Setting
        /// </summary>
        public Setting(string Name, Func<Brush> Get,
                       Action<SolidColorBrush> Set, SolidColorBrush Standart = null)
        {
            this.Name = Name;
            this.Type = SetType.Brush;
            this.GetBrush = Get;
            this.SetBrush = Set;
            this.Reset = () => Set(Standart);
        }
        /// <summary>
        /// Double Setting
        /// </summary>
        public Setting(string Name, Func<double> Get, Action<double> Set, double? Min = null, double? Max = null,
            Action<Action<double>> GetSetMin = null, Action<Action<double>> GetSetMax = null, double? Standart = null)
        {
            this.Name = Name;
            this.Type = SetType.Double;
            this.GetDouble = Get;
            this.SetDouble = Set;
            if (Standart.HasValue) this.Reset = () => Set(Standart.Value);
            this.Param1 = Min;
            this.Param2 = Max;
        }
        /// <summary>
        /// Int Setting
        /// </summary>
        public Setting(IntType Type, string Name, Func<int> Get, Action<int> Set, int? Min = null, int? Max = null,
            Action<Action<int>> GetSetMin = null, Action<Action<int>> GetSetMax = null, int? Standart = null)
        {
            this.Name = Name;
            this.Type = Type switch
            {
                IntType.Picker => SetType.IntPicker,
                IntType.Slider => SetType.IntSlider,
                _ => throw new Exception()
            };
            this.GetInt = Get;
            this.SetInt = Set;
            if (Standart.HasValue) this.Reset = () => Set(Standart.Value);
            this.Param1 = Min;
            this.Param2 = Max;
            this.GetSetMinInt = GetSetMin;
            this.GetSetMaxInt = GetSetMax;
        }
        /// <summary>
        /// Lock Setting
        /// </summary>
        public Setting(Func<bool> Get, Action<bool> Set)
        {
            this.Type = SetType.Lock;
            this.GetBool = Get;
            this.SetBool = Set;
        }
        /// <summary>
        /// Delete Setting
        /// </summary>
        public Setting(Action Delete)
        {
            this.Type = SetType.Delete;
            this.Delete = Delete;
        }
        /// <summary>
        /// Move Setting
        /// </summary>
        public Setting(Action<int> Set)
        {
            this.Type = SetType.Move;
            this.SetInt = Set;
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

        public readonly Action Delete;
        public readonly Func<bool> GetBool;
        public readonly Action<bool> SetBool;
        public readonly Func<Brush> GetBrush;
        public readonly Action<SolidColorBrush> SetBrush;
        public readonly Func<double> GetDouble;
        public readonly Action<double> SetDouble;
        public readonly Func<int> GetInt;
        public readonly Action<int> SetInt;

        public readonly Action Reset;

        public readonly Action<Action<int>> GetSetMinInt;
        public readonly Action<Action<int>> GetSetMaxInt;
        public readonly Action<Action<double>> GetSetMinDouble;
        public readonly Action<Action<double>> GetSetMaxDouble;

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
        Double,
        GoDown,
        GoUp,
        IntSlider,
        IntPicker,
        Lock,
        Move
    }

    public enum IntType
    {
        Picker,
        Slider
    }
}
