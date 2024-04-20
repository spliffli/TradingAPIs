/*

namespace TradingAPIs.MetaTrader.MTXConnect;

public class MqlParams
{
    private Dictionary<string, object> _dict = [];

    private MTXClient _client;


    public MqlParams(MTXClient client)
    {
        _client = client;
    }
    public int MillisecondTimer
    {
        get { return _client.GetMqlIntParam("millisecondTimer"); }
        set { _client.SetMqlIntParam("millisecondTimer", value); }
    }
    public int NumLastMessages
    {
        get { return _client.GetMqlIntParam("numLastMessages"); }
        set { _client.SetMqlIntParam("numLastMessages", value); }
    }
    public bool OpenChartsForBarData
    {
        get { return _client.GetMqlBoolParam("openChartsForBarData") == true; }
        set { _client.SetMqlBoolParam("openChartsForBarData", value ? true : false); }
    }
    public bool OpenChartsForHistoricData
    {
        get { return _client.GetMqlBoolParam("openChartsForHistoricData") == true; }
        set { _client.SetMqlBoolParam("openChartsForHistoricData", value ? true : false); }
    }
    public int MaximumOrders
    {
        get { return _client.GetMqlIntParam("maximumOrders"); }
        set { _client.SetMqlIntParam("maximumOrders", value); }
    }
    public double MaximumLotSize
    {
        get { return _client.GetMqlDoubleParam("maximumLotSize"); }
        set { _client.SetMqlDoubleParam("maximumLotSize", value); }
    }
    public int SlippagePoints
    {
        get { return _client.GetMqlIntParam("slippagePoints"); }
        set { _client.SetMqlIntParam("slippagePoints", value); }
    }


    public int GetMqlIntParam()
    {
        throw new NotImplementedException();
        //return _client.GetMqlIntParam();
    }

}

*/