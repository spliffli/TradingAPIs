using Newtonsoft.Json.Linq;

namespace TradingApis.mt4_api;

internal interface IMT4EventHandler
{
    public void Start(MT4ConnectionClient client);
    void OnTick(MT4ConnectionClient client, string symbol, double bid, double ask);

    public void OnBarData(MT4ConnectionClient client, string symbol, string timeFrame, string time, double open, double high, double low, double close, int tickVolume);

    public void OnHistoricData(MT4ConnectionClient client, string symbol, string timeFrame, JObject data);

    public void OnHistoricTrades(MT4ConnectionClient client);

    public void OnMessage(MT4ConnectionClient client, JObject message);

    public void OnOrderEvent(MT4ConnectionClient client);
}