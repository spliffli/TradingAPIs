namespace TradingAPIs.MetaTrader.MTXConnect;

public class MqlParams
{
    public static MqlParam MillisecondTimer = new MqlParam("millisecondTimer", typeof(int));
    public static MqlParam NumLastMessages = new MqlParam("numLastMessages", typeof(int));
    public static MqlParam OpenChartsForBarData = new MqlParam("openChartsForBarData", typeof(bool));
    public static MqlParam OpenChartsForHistoricData = new MqlParam("openChartsForHistoricData", typeof(bool));
    public static MqlParam MaximumOrders = new MqlParam("maximumOrders", typeof(int));
    public static MqlParam MaximumLotSize = new MqlParam("maximumLotSize", typeof(double));
    public static MqlParam SlippagePoints = new MqlParam("slippagePoints", typeof(int));
}
