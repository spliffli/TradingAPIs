using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingAPIs.MetaTrader;

public enum OrderType
{
    Buy,
    Sell,
    BuyLimit,
    SellLimit,
    BuyStop,
    SellStop
}
