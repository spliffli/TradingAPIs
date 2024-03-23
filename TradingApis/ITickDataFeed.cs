namespace TradingApis;

public interface ITickDataFeed
{
    public void Start();
    public void Stop();
    public void Subscribe();
    public void Unsubscribe();
    public ITickDataPoint GetNextTick();
}