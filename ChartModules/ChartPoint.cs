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
using System.Windows;

namespace ChartModules
{
    public struct ChartPoint
    {
        public ChartPoint(in DateTime Time, in double Price, IChart Chart)
        {
            this.Time = Time;
            this.Price = Price;
            this.Chart = Chart;
        }
        public ChartPoint(in Point P, IChart Chart)
        {
            this.Time = Chart.WidthToTime(P.X);
            this.Price = Chart.HeightToPrice(P.Y);
            this.Chart = Chart;
        }

        public DateTime Time;
        public double Price;
        private IChart Chart;

        public Point ToPoint() =>
            new Point(Chart.TimeToWidth(this.Time), Chart.PriceToHeight(this.Price));
    }

    public static class PointExtension
    {
        public static ChartPoint ToChartPoint(in this Point P, IChart C) =>
            new ChartPoint(C.WidthToTime(P.X), C.HeightToPrice(P.Y), C);

        public static void GetCoeffs(in this Point P1, in Point P2, out double A, out double B)
        { A = (P2.Y - P1.Y) / (P2.X - P1.X); B = -A * P1.X + P1.Y; }

        public static double GetDistance(in this Point A, in Point B) =>
            Math.Sqrt(Math.Pow(B.X - A.X, 2) + Math.Pow(B.Y - A.Y, 2));
    }
}
