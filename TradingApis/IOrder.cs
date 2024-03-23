namespace TradingApis;

public interface IOrder
{
    public string Symbol { get; }

    public OrderSide Side { get; }
    public OrderType OrderType { get; }
    public decimal Quantity { get; }

    public decimal OpenPrice { get; }
    public DateTime OpenTime { get; }

    public decimal ClosePrice { get; }
    public DateTime CloseTime { get; }

    public void Open();

    public void Close();
}