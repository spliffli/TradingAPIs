using TradingAPIs.MetaTrader;

namespace TradingAPIs.Common;

public interface IEventHandler
{
    void OnTick(MetaTraderClient client, string symbol, double bid, double ask);
    public void OnOrderEvent(MetaTraderClient client);
}