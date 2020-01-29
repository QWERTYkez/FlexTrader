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
    public class Binance : Exchange
    {
        private protected override string Endpoint => "https://api.binance.com";
        public override CandleIntervalKey[] CandleIntervalKeys => 
            new CandleIntervalKey[]
            {
                CandleIntervalKey.m1,
                CandleIntervalKey.m3,
                CandleIntervalKey.m5,
                CandleIntervalKey.m15,
                CandleIntervalKey.m30,
                CandleIntervalKey.h1,
                CandleIntervalKey.h2,
                CandleIntervalKey.h4,
                CandleIntervalKey.h6,
                CandleIntervalKey.h8,
                CandleIntervalKey.h12,
                CandleIntervalKey.d1,
                CandleIntervalKey.d3,
                CandleIntervalKey.w1,
                CandleIntervalKey.M1
            };
        public override Dictionary<CandleIntervalKey, TimeSpan> CandleIntervals { get; }
        
        public Binance()
        {
            CandleIntervals = AllCandleIntervals
                .Where(i => CandleIntervalKeys.Contains(i.Key))
                .ToDictionary(i => i.Key, i => i.Value);
        }

        public void GeneralInfo()
        {
            var req = BaseGet<GenInfo>("/api/v1/exchangeInfo").symbols;

            var rr = req.Where(r => r.baseAsset == "ETH" && r.quoteAsset == "USDT").ToList();
        }

        public override List<Candle> GetCandles(string baseAsset, string quoteAsset, CandleIntervalKey interval, 
            int? count = null, DateTime? startTime = null, DateTime? endTime = null)
        {
            //string req = $"/api/v1/klines?symbol={baseAsset}{quoteAsset}";
            //switch (interval)
            //{
            //    case CandleIntervalKey.m1: req = $"{req}&interval=1m"; break;
            //    case CandleIntervalKey.m3: req = $"{req}&interval=3m"; break;
            //    case CandleIntervalKey.m5: req = $"{req}&interval=5m"; break;
            //    case CandleIntervalKey.m15: req = $"{req}&interval=15m"; break;
            //    case CandleIntervalKey.m30: req = $"{req}&interval=30m"; break;
            //    case CandleIntervalKey.h1: req = $"{req}&interval=1h"; break;
            //    case CandleIntervalKey.h2: req = $"{req}&interval=2h"; break;
            //    case CandleIntervalKey.h4: req = $"{req}&interval=4h"; break;
            //    case CandleIntervalKey.h6: req = $"{req}&interval=6h"; break;
            //    case CandleIntervalKey.h8: req = $"{req}&interval=8h"; break;
            //    case CandleIntervalKey.h12: req = $"{req}&interval=12h"; break;
            //    case CandleIntervalKey.d1: req = $"{req}&interval=1d"; break;
            //    case CandleIntervalKey.d3: req = $"{req}&interval=3d"; break;
            //    case CandleIntervalKey.w1: req = $"{req}&interval=1w"; break;
            //    case CandleIntervalKey.M1: req = $"{req}&interval=1M"; break;
            //    default: return null;
            //}
            //if (count != null)
            //{
            //    if (count.Value < 1) req = $"{req}&limit={1}";
            //    else if (count.Value > 499) req = $"{req}&limit={500}";
            //    else req = $"{req}&limit={count.Value}";
            //}
            //if (startTime != null) req = $"{req}&startTime={startTime.Value}";
            //if (endTime != null) req = $"{req}&endTime={endTime.Value}";

            //try
            //{
            //    return BaseGet<List<List<decimal>>>(req)
            //    .Select(o => new Candle(o[0], o[1], o[2], o[3], o[4], o[5])).ToList();
            //}


            try
            {
                var JSON = System.IO.File.ReadAllText("JSON.txt");

                return JsonConvert.DeserializeObject<List<List<decimal>>>(JSON)
                    .Select(o => new Candle(o[0], o[1], o[2], o[3], o[4], o[5])).ToList();
            }
            catch { return null; }
        }

        #region JsonClasses
        class GenInfo
        {
            public List<Symbol> symbols { get; set; }

            public class Symbol
            {
                public string symbol { get; set; }
                public string status { get; set; }
                public string baseAsset { get; set; }
                public int baseAssetPrecision { get; set; }
                public string quoteAsset { get; set; }
                public int quotePrecision { get; set; }
                public int baseCommissionPrecision { get; set; }
                public int quoteCommissionPrecision { get; set; }
                public List<string> orderTypes { get; set; }
                public bool icebergAllowed { get; set; }
                public bool ocoAllowed { get; set; }
                public bool quoteOrderQtyMarketAllowed { get; set; }
                public bool isSpotTradingAllowed { get; set; }
                public bool isMarginTradingAllowed { get; set; }
                public List<Filter> filters { get; set; }

                public class Filter
                {
                    public string filterType { get; set; }
                    public double minPrice { get; set; }
                    public double maxPrice { get; set; }
                    public double tickSize { get; set; }
                    public string multiplierUp { get; set; }
                    public string multiplierDown { get; set; }
                    public int? avgPriceMins { get; set; }
                    public string minQty { get; set; }
                    public string maxQty { get; set; }
                    public string stepSize { get; set; }
                    public string minNotional { get; set; }
                    public bool? applyToMarket { get; set; }
                    public int? limit { get; set; }
                    public int? maxNumAlgoOrders { get; set; }
                }
            }
        }
        #endregion
    }
}