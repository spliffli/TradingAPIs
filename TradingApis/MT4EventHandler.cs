using Newtonsoft.Json.Linq;
using TradingApis.mt4_api;

namespace TradingApis;

public class MT4EventHandler : IMT4EventHandler, IEventHandler
{
    private MT4Configuration _config;
    private Logger _logger;
    private bool _verbose;
    
    public bool SubscribeSymbolsTickData { get; set; }
    public bool SubscribeSymbolsBarData { get; set; }
    public string[] SymbolsTickData { get; set; }
    public string[,] SymbolsBarData { get; set; }



    // Constructor
    public MT4EventHandler(MT4Configuration config, Logger logger, bool verbose=true)
    {
        _config = config;
        _logger = logger;
        _verbose = verbose;

        if (config == null)
            throw new ArgumentException("config cannot be null.");
        if (logger == null)
            throw new ArgumentException("logger cannot be null.");

        if (config.SubscribeToTickData)
        {
            SubscribeSymbolsTickData = true;

            if (config.SymbolsTickData == null || config.SymbolsTickData.Length == 0)
                throw new ArgumentException("SymbolsTickData cannot be null or empty if SubscribeToTickData is true.");

            SymbolsTickData = config.SymbolsTickData;
        }

        if (config.SubscribeToBarData)
        {
            SubscribeSymbolsBarData = true;

            if (config.SymbolsBarData == null || config.SymbolsBarData.Length == 0)
                throw new ArgumentException("SymbolsBarData cannot be null or empty if SubscribeToBarData is true.");

            SymbolsBarData = config.SymbolsBarData;
        }
    }

    public void Start(MT4ConnectionClient client)
    {
        if (!SubscribeSymbolsTickData && !SubscribeSymbolsBarData)
            throw new ArgumentException(
                "At least one of SubscribeSymbolsTickData or SubscribeSymbolsBarData must be true to start the MT4EventHandler.");

        // Logic to Start handling events from the MT4 instance
        // account information is stored in client.AccountInfo.
        // open orders are stored in client.OpenOrders.
        // historic trades are stored in client.HistoricTrades.
        
        Console.WriteLine("\nAccount info:\n" + client.AccountInfo + "\n");

        if (SubscribeSymbolsTickData)
        {
            // subscribe to tick data:
            // string[] symbolsTickData = { "EURUSD", "GBPUSD" };
            Console.WriteLine("Subscribing to tick data.");
            client.SubscribeSymbolsTickData(SymbolsTickData);
        }

        if (SubscribeSymbolsBarData)
        {
            // subscribe to bar data:
            Console.WriteLine("Subscribing to bar data.");
            // string[,] symbolsBarData = new string[,] { { "EURUSD", "M1" }, { "AUDCAD", "M5" }, { "GBPCAD", "M15" } };
            client.SubscribeSymbolsBarData(SymbolsBarData);
        }

    }

    // OnTick method to handle tick data from MT4
    public void OnTick(MT4ConnectionClient client, string symbol, double bid, double ask)
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

    public void OnBarData(MT4ConnectionClient client, string symbol, string timeFrame, string time, double open, double high,
        double low, double close, int tickVolume)
    {
        Console.WriteLine("onBarData: " + symbol + ", " + timeFrame + ", " + time + ", " + open + ", " + high + ", " + low + ", " + close + ", " + tickVolume);

        foreach (var x in client.HistoricData)
            Console.WriteLine(x.Key + ": " + x.Value);
    }

    public void OnHistoricData(MT4ConnectionClient client, string symbol, string timeFrame, JObject data)
    {
        // you can also access historic data via: client.HistoricData.keySet()
        Console.WriteLine("onHistoricData: " + symbol + ", " + timeFrame + ", " + data);
    }

    public void OnHistoricTrades(MT4ConnectionClient client)
    {
        Console.WriteLine("OnHistoricTrades: " + client.HistoricTrades);
    }

    public void OnMessage(MT4ConnectionClient client, JObject message)
    {
        if (((string)message["type"]).Equals("ERROR"))
            Console.WriteLine(message["type"] + " | " + message["error_type"] + " | " + message["description"]);
        else if (((string)message["type"]).Equals("INFO"))
            Console.WriteLine(message["type"] + " | " + message["message"]);
    }

    public void OnOrderEvent(MT4ConnectionClient client)
    {
        Console.WriteLine("onOrderEvent: " + client.OpenOrders.Count + " open orders");

        // client.OpenOrders is a JSONObject, which can be accessed like this:
        // foreach (var x in client.OpenOrders)
        //     Console.WriteLine(x.Key + ": " + x.Value);
    }
}