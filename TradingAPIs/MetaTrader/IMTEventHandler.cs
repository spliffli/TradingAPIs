using Newtonsoft.Json.Linq;

namespace TradingAPIs.MetaTrader;

internal interface IMTEventHandler
{
    void OnTick(MTConnectionClient client, string symbol, double bid, double ask);

    public void OnBarData(MTConnectionClient client, string symbol, string timeFrame, string time, double open, double high, double low, double close, int tickVolume);

    public void OnHistoricData(MTConnectionClient client, string symbol, string timeFrame, JObject data);

    public void OnHistoricTrades(MTConnectionClient client);

    public void OnMessage(MTConnectionClient client, JObject message);

    public void OnOrderEvent(MTConnectionClient client);
}