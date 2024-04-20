using TradingAPIs.MetaTrader.MTXConnect;

namespace TradingAPIs.Common;

public interface IEventHandler
{
    void OnTick(IConnectionClient client, string symbol, double bid, double ask);
    public void OnOrderEvent(IConnectionClient client);
}