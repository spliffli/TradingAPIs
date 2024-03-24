using TradingApis.MetaTrader;

namespace TradingApis.Common;

public interface IEventHandler
{
    public void Start(MTConnectionClient client);
    void OnTick(MTConnectionClient client, string symbol, double bid, double ask);
    public void OnOrderEvent(MTConnectionClient client);
}