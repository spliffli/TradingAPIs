using Newtonsoft.Json.Linq;

namespace TradingAPIs.MetaTrader;

internal interface IMTEventHandler
{
    void OnTick(MetaTraderClient client, string symbol, double bid, double ask);

    public void OnBarData(MetaTraderClient client, string symbol, string timeFrame, string time, double open, double high, double low, double close, int tickVolume);

    public void OnHistoricData(MetaTraderClient client, string symbol, string timeFrame, JObject data);

    public void OnHistoricTrades(MetaTraderClient client);

    public void OnMessage(MetaTraderClient client, JObject message);

    public void OnOrderEvent(MetaTraderClient client);
}