namespace TradingApis;

public interface IEventHandler
{
    public void Start(MT4ConnectionClient client);
    void OnTick(MT4ConnectionClient client, string symbol, double bid, double ask);
    public void OnOrderEvent(MT4ConnectionClient client);
}