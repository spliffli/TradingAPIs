using TradingAPIs.MetaTrader;

namespace TradingAPIs.Common;

public interface IEventHandler
{
    void OnTick(MTConnectionClient client, string symbol, double bid, double ask);
    public void OnOrderEvent(MTConnectionClient client);
}