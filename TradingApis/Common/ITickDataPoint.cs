namespace TradingApis.Common;

public interface ITickDataPoint
{
    public string Symbol { get; }
    public decimal Bid { get; }
    public decimal Ask { get; }

    public DateTime Time { get; }
}