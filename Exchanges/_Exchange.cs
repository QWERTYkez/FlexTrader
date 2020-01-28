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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FlexTrader.Exchanges
{
    public abstract class Exchange
    {
        private protected abstract string Endpoint { get; }
        public abstract CandleIntervalKey[] CandleIntervalKeys { get; }
        public abstract Dictionary<CandleIntervalKey, TimeSpan> CandleIntervals { get; }
        
        private protected T BaseGet<T>(string req, JsonConverter[] converters = null) where T : class
        {
            try
            {
                var request = WebRequest.Create($"{Endpoint}{req}");
                using (var response = request.GetResponse())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        using (var reader = new System.IO.StreamReader(stream))
                        {
                            return JsonConvert.DeserializeObject<T>(reader.ReadToEnd(), converters);
                        }
                    }
                }
            }
            catch { return null; }
        }

        public abstract List<Candle> GetCandles(string baseAsset, string quoteAsset, CandleIntervalKey interval,
            int? count = null, DateTime? startTime = null, DateTime? endTime = null);

        public static Dictionary<CandleIntervalKey, TimeSpan> AllCandleIntervals { get; } =
            new Dictionary<CandleIntervalKey, TimeSpan>
            {
                { CandleIntervalKey.m1, new TimeSpan(0,1,0) },
                { CandleIntervalKey.m3, new TimeSpan(0,3,0) },
                { CandleIntervalKey.m5, new TimeSpan(0,5,0) },
                { CandleIntervalKey.m15, new TimeSpan(0,15,0) },
                { CandleIntervalKey.m30, new TimeSpan(0,30,0) },
                { CandleIntervalKey.h1, new TimeSpan(1,0,0) },
                { CandleIntervalKey.h2, new TimeSpan(2,0,0) },
                { CandleIntervalKey.h4, new TimeSpan(4,0,0) },
                { CandleIntervalKey.h6, new TimeSpan(6,0,0) },
                { CandleIntervalKey.h8, new TimeSpan(8,0,0) },
                { CandleIntervalKey.h12, new TimeSpan(12,0,0) },
                { CandleIntervalKey.d1, new TimeSpan(1,0,0,0) },
                { CandleIntervalKey.d3, new TimeSpan(3,0,0,0) },
                { CandleIntervalKey.w1, new TimeSpan(7,0,0,0) },
                { CandleIntervalKey.M1, new TimeSpan(30,0,0,0) }
            };
    }

    public enum CandleIntervalKey
    {
        m1,
        m3,
        m5,
        m15,
        m30,
        h1,
        h2,
        h4,
        h6,
        h8,
        h12,
        d1,
        d3,
        w1,
        M1
    }

    public struct Candle
    {
        public Candle(decimal TimeStamp, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume)
        {
            this.TimeStamp = App.BaseDT.AddMilliseconds(Convert.ToInt64(TimeStamp));
            this.Open = Open;
            this.High = High;
            this.Low = Low;
            this.Close = Close;
            this.Volume = Volume;
        }

        public DateTime TimeStamp { get; private set; }
        public decimal Open { get; private set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public bool UP => Close > Open;
        public decimal Volume { get; set; }
    }
}
