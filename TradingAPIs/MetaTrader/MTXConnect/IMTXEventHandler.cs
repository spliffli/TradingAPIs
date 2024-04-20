using Newtonsoft.Json.Linq;

namespace TradingAPIs.MetaTrader.MTXConnect;

internal interface IMTXEventHandler
{
    void OnTick(MTXClient client, string symbol, double bid, double ask);

    public void OnBarData(MTXClient client, string symbol, string timeFrame, string time, double open, double high, double low, double close, int tickVolume);

    public void OnHistoricData(MTXClient client, string symbol, string timeFrame, JObject data);

    public void OnHistoricTrades(MTXClient client);

    public void OnMessage(MTXClient client, JObject message);

    public void OnOrderEvent(MTXClient client);

    // public event EventHandler<MTXEventArgs> OnEvent;
}