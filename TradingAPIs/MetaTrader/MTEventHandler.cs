using Newtonsoft.Json.Linq;
using TradingAPIs.Common;
using TradingAPIs.Common.Loggers;

namespace TradingAPIs.MetaTrader;

public class MTEventHandler : IMTEventHandler, IEventHandler
{
    private MTConfiguration _config;
    private Logger _logger;
    private bool _verbose;

    // Constructor
    public MTEventHandler(Logger logger, bool verbose=true)
    {
        _logger = logger;
        _verbose = verbose;
    }


    // OnTick method to handle tick data from MT4
    public void OnTick(MTConnectionClient client, string symbol, double bid, double ask)
    {
        // Logic to process the tick data
        // For example, logging the tick data or performing some analysis
        if (_verbose)
            Console.WriteLine("OnTick: " + symbol + " | Bid: " + bid + " | Ask: " + ask);


        // print(dwx.accountInfo);
        // print(dwx.openOrders);

        // to open multiple orders:
        // if (first) {
        // 	first = false;
        // // dwx.closeAllOrders();
        // 	for (int i=0; i<5; i++) {
        // 		dwx.openOrder(symbol, "buystop", 0.05, ask+0.01, 0, 0, 77, "", 0);
        // 	}
        // }
    }

    public void OnBarData(MTConnectionClient client, string symbol, string timeFrame, string time, double open, double high,
        double low, double close, int tickVolume)
    {
        Console.WriteLine("onBarData: " + symbol + ", " + timeFrame + ", " + time + ", " + open + ", " + high + ", " + low + ", " + close + ", " + tickVolume);

        foreach (var x in client.HistoricData)
            Console.WriteLine(x.Key + ": " + x.Value);
    }

    public void OnHistoricData(MTConnectionClient client, string symbol, string timeFrame, JObject data)
    {
        // you can also access historic data via: client.HistoricData.keySet()
        Console.WriteLine("onHistoricData: " + symbol + ", " + timeFrame + ", " + data);
    }

    public void OnHistoricTrades(MTConnectionClient client)
    {
        Console.WriteLine("OnHistoricTrades: " + client.HistoricTrades);
    }

    public void OnMessage(MTConnectionClient client, JObject message)
    {
        if (((string)message["type"]).Equals("ERROR"))
            Console.WriteLine(message["type"] + " | " + message["error_type"] + " | " + message["description"]);
        else if (((string)message["type"]).Equals("INFO"))
            Console.WriteLine(message["type"] + " | " + message["message"]);
    }

    public void OnOrderEvent(MTConnectionClient client)
    {
        Console.WriteLine("onOrderEvent: " + client.OpenOrders.Count + " open orders");

        // client.OpenOrders is a JSONObject, which can be accessed like this:
        // foreach (var x in client.OpenOrders)
        //     Console.WriteLine(x.Key + ": " + x.Value);
    }
}