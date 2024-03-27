using System.Collections;
using Newtonsoft.Json.Linq;
using TradingAPIs.Common;
using TradingAPIs.Common.Loggers;

namespace TradingAPIs.MetaTrader;

public class MTConnectionClient : IConnectionClient
{
    private readonly MTEventHandler _eventHandler;
    private string _metaTraderDirPath;  // { get; private set; }
    private readonly Logger _logger;
    private readonly int _sleepDelayMilliseconds;
    private readonly int _maxRetryCommandSeconds;
    private readonly bool _loadOrdersFromFile;
    private readonly bool _verbose;

    private readonly string _pathOrders;
    private readonly string _pathMessages;
    private readonly string _pathMarketData;
    private readonly string _pathBarData;
    private readonly string _pathHistoricData;
    private readonly string _pathHistoricTrades;
    private readonly string _pathOrdersStored;
    private readonly string _pathMessagesStored;
    private readonly string _pathCommandsPrefix;

    private const int MaxCommandFiles = 20;
    private int _commandId = 0;
    private long _lastMessagesMillis = 0;
    private string _lastOpenOrdersStr = "";
    private string _lastMessagesStr = "";
    private string _lastMarketDataStr = "";
    private string _lastBarDataStr = "";
    private string _lastHistoricDataStr = "";
    private string _lastHistoricTradesStr = "";

    public JObject OpenOrders = new JObject();
    public JObject AccountInfo = new JObject();
    public JObject MarketData = new JObject();
    public JObject BarData = new JObject();
    public JObject HistoricData = new JObject();
    public JObject HistoricTrades = new JObject();

    private JObject _lastBarData = new JObject();
    private JObject _lastMarketData = new JObject();

    public bool Active = true;
    private bool _start = false;

    private readonly Thread _openOrdersThread;
    private readonly Thread _messageThread;
    private readonly Thread _marketDataThread;
    private readonly Thread _barDataThread;
    private readonly Thread _historicDataThread;

    public bool SubscribeToTickData { get; set; }
    public bool SubscribeToBarData { get; set; }
    public string[] SymbolsTickData { get; set; }
    public string[,] SymbolsBarData { get; set; }

    // Constructor definition with parameters for configuration
    public MTConnectionClient(MTConfiguration config, MTEventHandler eventHandler, Logger logger, int sleepDelayMilliseconds = 5, int maxRetryCommandSeconds = 10, bool loadOrdersFromFile = true, bool verbose = true)
    {
        _logger = logger;
        _logger.Log("MT4ConnectionClient(): Initializing...");

        if (config == null)
            throw new ArgumentException("config cannot be null.");
        if (logger == null)
            throw new ArgumentException("logger cannot be null.");

        if (config.SubscribeToTickData)
        {
            SubscribeToTickData = true;

            if (config.SymbolsTickData == null || config.SymbolsTickData.Length == 0)
                throw new ArgumentException("SymbolsTickData cannot be null or empty if SubscribeToTickData is true.");

            SymbolsTickData = config.SymbolsTickData;
        }

        if (config.SubscribeToBarData)
        {
            SubscribeToBarData = true;

            if (config.SymbolsBarData == null || config.SymbolsBarData.Length == 0)
                throw new ArgumentException("SymbolsBarData cannot be null or empty if SubscribeToBarData is true.");

            SymbolsBarData = config.SymbolsBarData;
        }

        // Assign the constructor parameters to the class's fields
        _eventHandler = eventHandler;                          // Event handler for managing MT4 events
        _metaTraderDirPath = config.MetaTraderDirPath;         // Directory path of the MetaTrader installation
        _sleepDelayMilliseconds = sleepDelayMilliseconds;      // Sleep delay in milliseconds between checks
        _maxRetryCommandSeconds = maxRetryCommandSeconds;      // Maximum time in seconds to retry a command
        _loadOrdersFromFile = loadOrdersFromFile;              // Flag to load orders from a file
        _verbose = verbose;                                    // Verbose mode flag

        // Check if the MetaTrader directory path exists; if not, exit the program
        if (!Directory.Exists(_metaTraderDirPath))
        {
            Console.WriteLine("ERROR: metaTraderDirPath does not exist! metaTraderDirPath: " + _metaTraderDirPath);
            Environment.Exit(1);
        }

        // Initialize the paths for various types of data files within the MetaTrader directory
        _logger.Log("MT4ConnectionClient(): Initializing paths for data files within MetaTrader directory.");
        _pathOrders = Path.Join(_metaTraderDirPath, "DWX", "DWX_Orders.txt");
        _pathMessages = Path.Join(_metaTraderDirPath, "DWX", "DWX_Messages.txt");
        _pathMarketData = Path.Join(_metaTraderDirPath, "DWX", "DWX_Market_Data.txt");
        _pathBarData = Path.Join(_metaTraderDirPath, "DWX", "DWX_Bar_Data.txt");
        _pathHistoricData = Path.Join(_metaTraderDirPath, "DWX", "DWX_Historic_Data.txt");
        _pathHistoricTrades = Path.Join(_metaTraderDirPath, "DWX", "DWX_Historic_Trades.txt");
        _pathOrdersStored = Path.Join(_metaTraderDirPath, "DWX", "DWX_Orders_Stored.txt");
        _pathMessagesStored = Path.Join(_metaTraderDirPath, "DWX", "DWX_Messages_Stored.txt");
        _pathCommandsPrefix = Path.Join(_metaTraderDirPath, "DWX", "DWX_Commands_");
        _logger.Log("MT4ConnectionClient(): Paths initialized.");

        _logger.Log("MT4ConnectionClient(): Loading messages from file.");
        LoadMessages(); // Load the initial messages from the file

        if (loadOrdersFromFile)
        {
            _logger.Log("MT4ConnectionClient(): Loading orders from file, as specified.");
            LoadOrders(); // Load the initial orders from the file, if specified
        }

        // Initialize and start threads for continuously checking and processing open orders, messages, market data, etc.
        _logger.Log("MT4ConnectionClient(): Initializing and starting threads for continuous data processing...");

        _logger.Log("MT4ConnectionClient(): Starting thread for checking open orders.");
        _openOrdersThread = new Thread(() => CheckOpenOrders());
        _openOrdersThread.Start();

        _logger.Log("MT4ConnectionClient(): Starting thread for checking messages.");
        _messageThread = new Thread(() => CheckMessages());
        _messageThread.Start();

        _logger.Log("MT4ConnectionClient(): Starting thread for checking market data.");
        _marketDataThread = new Thread(() => CheckMarketData());
        _marketDataThread.Start();

        _logger.Log("MT4ConnectionClient(): Starting thread for checking bar data.");
        _barDataThread = new Thread(() => CheckBarData());
        _barDataThread.Start();

        _logger.Log("MT4ConnectionClient(): Starting thread for checking historic data.");
        _historicDataThread = new Thread(() => CheckHistoricData());
        _historicDataThread.Start();

        _logger.Log("MT4ConnectionClient(): Threads initialized and started.");

        _logger.Log("MT4ConnectionClient(): Resetting command IDs to their initial state.");
        ResetCommandIDs(); // Reset command IDs to their initial state

        // Start processing. If an event handler is provided, delay the start slightly to ensure readiness
        if (eventHandler == null)
        {
            _logger.Log("MT4ConnectionClient(): Starting processing without an event handler.");
            Start();
        }
        else
        {
            Thread.Sleep(1000); // Wait for 1 second before starting
            _logger.Log("MT4ConnectionClient(): Starting processing with an event handler.");
            Start();
            // eventHandler.Start(this); // Start the event handler, passing this instance for context
        }

        _logger.Log("MT4ConnectionClient(): Initialization complete.");
    }

    public MTConnectionClient(string pathHistoricTrades)
    {
        _pathHistoricTrades = pathHistoricTrades;
    }

    /*START can be used to check if the client has been initialized.  
    */
    public void Start()
    {
        _start = true;

        if (!SubscribeToTickData && !SubscribeToBarData)
            throw new ArgumentException(
                "At least one of SubscribeSymbolsTickData or SubscribeSymbolsBarData must be true to start the MT4EventHandler.");

        // Logic to Start handling events from the MT4 instance
        // account information is stored in client.AccountInfo.
        // open orders are stored in client.OpenOrders.
        // historic trades are stored in client.HistoricTrades.

        Console.WriteLine("\nAccount info:\n" + AccountInfo + "\n");

        if (SubscribeToTickData)
        {
            // subscribe to tick data:
            // string[] symbolsTickData = { "EURUSD", "GBPUSD" };
            Console.WriteLine("Subscribing to tick data.");
            SubscribeSymbolsTickData(SymbolsTickData);
        }

        if (SubscribeToBarData)
        {
            // subscribe to bar data:
            Console.WriteLine("Subscribing to bar data.");
            // string[,] symbolsBarData = new string[,] { { "EURUSD", "M1" }, { "AUDCAD", "M5" }, { "GBPCAD", "M15" } };
            SubscribeSymbolsBarData(SymbolsBarData);
        }
    }


    /*Regularly checks the file for open orders and triggers
    the eventHandler.OnOrderEvent() function.
    */
    private void CheckOpenOrders()
    {
        while (Active)
        {

            Thread.Sleep(_sleepDelayMilliseconds);

            if (!_start)
                continue;

            string text = MTHelpers.TryReadFile(_pathOrders);

            if (text.Length == 0 || text.Equals(_lastOpenOrdersStr))
                continue;

            _lastOpenOrdersStr = text;

            JObject data;

            try
            {
                data = JObject.Parse(text);
            }
            catch
            {
                continue;
            }

            if (data == null)
                continue;

            JObject dataOrders = (JObject)data["orders"];

            bool newEvent = false;
            foreach (var x in OpenOrders)
            {
                // JToken value = x.Value;
                if (dataOrders[x.Key] == null)
                {
                    newEvent = true;
                    if (_verbose)
                        Console.WriteLine("Order removed: " + OpenOrders[x.Key].ToString());
                }
            }
            foreach (var x in dataOrders)
            {
                // JToken value = x.Value;
                if (OpenOrders[x.Key] == null)
                {
                    newEvent = true;
                    if (_verbose)
                        Console.WriteLine("New order: " + dataOrders[x.Key].ToString());
                }
            }

            OpenOrders = dataOrders;
            AccountInfo = (JObject)data["account_info"];

            if (_loadOrdersFromFile)
                MTHelpers.TryWriteToFile(_pathOrdersStored, data.ToString());

            if (_eventHandler != null && newEvent)
                _eventHandler.OnOrderEvent(this);
        }
    }


    /*Regularly checks the file for messages and triggers
    the eventHandler.OnMessage() function.
    */
    private void CheckMessages()
    {
        while (Active)
        {

            Thread.Sleep(_sleepDelayMilliseconds);

            if (!_start)
                continue;

            string text = MTHelpers.TryReadFile(_pathMessages);

            if (text.Length == 0 || text.Equals(_lastMessagesStr))
                continue;

            _lastMessagesStr = text;

            JObject data;

            try
            {
                data = JObject.Parse(text);
            }
            catch
            {
                continue;
            }

            if (data == null)
                continue;

            // var sortedObj = new JObject(data.Properties().OrderByDescending(p => (int)p.Value));

            // make sure that the message are sorted so that we don't miss messages because of (millis > lastMessagesMillis).
            ArrayList millisList = new ArrayList();

            foreach (var x in data)
            {
                if (data[x.Key] != null)
                {
                    millisList.Add(x.Key);
                }
            }
            millisList.Sort();
            foreach (string millisStr in millisList)
            {
                if (data[millisStr] != null)
                {
                    long millis = Int64.Parse(millisStr);
                    if (millis > _lastMessagesMillis)
                    {
                        _lastMessagesMillis = millis;
                        if (_eventHandler != null)
                            _eventHandler.OnMessage(this, (JObject)data[millisStr]);
                    }
                }
            }
            MTHelpers.TryWriteToFile(_pathMessagesStored, data.ToString());
        }
    }


    /*Regularly checks the file for market data and triggers
    the eventHandler.OnTick() function.
    */
    private void CheckMarketData()
    {
        while (Active)
        {

            Thread.Sleep(_sleepDelayMilliseconds);

            if (!_start)
                continue;

            string text = MTHelpers.TryReadFile(_pathMarketData);

            if (text.Length == 0 || text.Equals(_lastMarketDataStr))
                continue;

            _lastMarketDataStr = text;

            JObject data;

            try
            {
                data = JObject.Parse(text);
            }
            catch
            {
                continue;
            }

            if (data == null)
                continue;

            MarketData = data;

            if (_eventHandler != null)
            {
                foreach (var x in MarketData)
                {
                    string symbol = x.Key;
                    if (_lastMarketData[symbol] == null || !MarketData[symbol].Equals(_lastMarketData[symbol]))
                    {
                        // JObject jo = (JObject)marketData[symbol];
                        _eventHandler.OnTick(this, symbol,
                                            (double)MarketData[symbol]["bid"],
                                            (double)MarketData[symbol]["ask"]);
                    }
                }
            }
            _lastMarketData = data;
        }
    }


    /*Regularly checks the file for bar data and triggers
    the eventHandler.OnBarData() function.
    */
    private void CheckBarData()
    {
        while (Active)
        {

            Thread.Sleep(_sleepDelayMilliseconds);

            if (!_start)
                continue;

            string text = MTHelpers.TryReadFile(_pathBarData);

            if (text.Length == 0 || text.Equals(_lastBarDataStr))
                continue;

            _lastBarDataStr = text;

            JObject data;

            try
            {
                data = JObject.Parse(text);
            }
            catch
            {
                continue;
            }

            if (data == null)
                continue;

            BarData = data;

            if (_eventHandler != null)
            {
                foreach (var x in BarData)
                {
                    string st = x.Key;
                    if (_lastBarData[st] == null || !BarData[st].Equals(_lastBarData[st]))
                    {
                        string[] stSplit = st.Split("_");
                        if (stSplit.Length != 2)
                            continue;
                        // JObject jo = (JObject)barData[symbol];
                        _eventHandler.OnBarData(this, stSplit[0], stSplit[1],
                                               (String)BarData[st]["time"],
                                               (double)BarData[st]["open"],
                                               (double)BarData[st]["high"],
                                               (double)BarData[st]["low"],
                                               (double)BarData[st]["close"],
                                               (int)BarData[st]["tick_volume"]);
                    }
                }
            }
            _lastBarData = data;
        }
    }


    /*Regularly checks the file for historic data and triggers
    the eventHandler.OnHistoricData() function.
    */
    private void CheckHistoricData()
    {
        while (Active)
        {

            Thread.Sleep(_sleepDelayMilliseconds);

            if (!_start)
                continue;

            string text = MTHelpers.TryReadFile(_pathHistoricData);

            if (text.Length > 0 && !text.Equals(_lastHistoricDataStr))
            {
                _lastHistoricDataStr = text;

                JObject data;

                try
                {
                    data = JObject.Parse(text);
                }
                catch
                {
                    data = null;
                }

                if (data != null)
                {
                    foreach (var x in data)
                    {
                        HistoricData[x.Key] = data[x.Key];
                    }

                    MTHelpers.TryDeleteFile(_pathHistoricData);

                    if (_eventHandler != null)
                    {
                        foreach (var x in data)
                        {
                            string st = x.Key;
                            string[] stSplit = st.Split("_");
                            if (stSplit.Length != 2)
                                continue;
                            // JObject jo = (JObject)barData[symbol];
                            _eventHandler.OnHistoricData(this, stSplit[0], stSplit[1], (JObject)data[x.Key]);
                        }
                    }
                }


            }

            // also check historic trades in the same thread. 
            text = MTHelpers.TryReadFile(_pathHistoricTrades);

            if (text.Length > 0 && !text.Equals(_lastHistoricTradesStr))
            {
                _lastHistoricTradesStr = text;

                JObject data;

                try
                {
                    data = JObject.Parse(text);
                }
                catch
                {
                    data = null;
                }

                if (data != null)
                {
                    HistoricTrades = data;

                    MTHelpers.TryDeleteFile(_pathHistoricTrades);

                    if (_eventHandler != null)
                        _eventHandler.OnHistoricTrades(this);
                }


            }
        }
    }


    /*Loads stored orders from file (in case of a restart). 
    */
    private void LoadOrders()
    {

        string text = MTHelpers.TryReadFile(_pathOrdersStored);

        if (text.Length == 0)
            return;

        JObject data;

        try
        {
            data = JObject.Parse(text);
        }
        catch
        {
            return;
        }

        if (data == null)
            return;

        _lastOpenOrdersStr = text;
        OpenOrders = (JObject)data["orders"];
        AccountInfo = (JObject)data["account_info"];
    }


    /*Loads stored messages from file (in case of a restart). 
    */
    private void LoadMessages()
    {

        string text = MTHelpers.TryReadFile(_pathMessagesStored);

        if (text.Length == 0)
            return;

        JObject data;

        try
        {
            data = JObject.Parse(text);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            return;
        }

        if (data == null)
            return;

        _lastMessagesStr = text;

        // here we don't have to sort because we just need the latest millis value. 
        foreach (var x in data)
        {
            long millis = Int64.Parse(x.Key);
            if (millis > _lastMessagesMillis)
                _lastMessagesMillis = millis;
        }
    }


    /*Sends a SUBSCRIBE_SYMBOLS command to subscribe to market (tick) data.

    Args:
        symbols (String[]): List of symbols to subscribe to.

    Returns:
        null

        The data will be stored in marketData. 
        On receiving the data the eventHandler.OnTick() 
        function will be triggered. 
    */
    public void SubscribeSymbolsTickData(string[] symbols)
    {
        SendCommand("SUBSCRIBE_SYMBOLS", String.Join(",", symbols));
    }


    /*Sends a SUBSCRIBE_SYMBOLS_BAR_DATA command to subscribe to bar data.

    Args:
        symbols (string[,]): List of lists containing symbol/time frame 
        combinations to subscribe to. For example:
        string[,] symbols = new string[,]{{"EURUSD", "M1"}, {"USDJPY", "H1"}};

    Returns:
        null

        The data will be stored in barData. 
        On receiving the data the eventHandler.OnBarData() 
        function will be triggered. 
    */
    public void SubscribeSymbolsBarData(string[,] symbols)
    {
        string content = "";
        for (int i = 0; i < symbols.GetLength(0); i++)
        {
            if (i != 0) content += ",";
            content += symbols[i, 0] + "," + symbols[i, 1];
        }
        SendCommand("SUBSCRIBE_SYMBOLS_BAR_DATA", content);
    }


    /*Sends a GET_HISTORIC_DATA command to request historic data.

    Args:
        symbol (String): Symbol to get historic data.
        timeFrame (String): Time frame for the requested data.
        Start (long): Start timestamp (seconds since epoch) of the requested data.
        end (long): End timestamp of the requested data.

    Returns:
        null

        The data will be stored in historicData. 
        On receiving the data the eventHandler.OnHistoricData() 
        function will be triggered. 
    */
    public void GetHistoricData(String symbol, String timeFrame, long start, long end)
    {
        string content = symbol + "," + timeFrame + "," + start + "," + end;
        SendCommand("GET_HISTORIC_DATA", content);
    }



    /*Sends a GET_HISTORIC_TRADES command to request historic trades.

    Kwargs:
        lookbackDays (int): Days to look back into the trade history. 
                            The history must also be visible in MT4. 

    Returns:
        None

        The data will be stored in historicTrades. 
        On receiving the data the eventHandler.OnHistoricTrades() 
        function will be triggered. 
    */
    public void GetHistoricTrades(int lookbackDays)
    {
        SendCommand("GET_HISTORIC_TRADES", lookbackDays.ToString());
    }


    /*Sends an OPEN_ORDER command to open an order.

    Args:
        symbol (String): Symbol for which an order should be opened. 
        order_type (String): Order type. Can be one of:
            'buy', 'sell', 'buylimit', 'selllimit', 'buystop', 'sellstop'
        lots (double): Volume in lots
        price (double): Price of the (pending) order. Can be zero 
            for market orders. 
        stop_loss (double): SL as absoute price. Can be zero 
            if the order should not have an SL. 
        take_profit (double): TP as absoute price. Can be zero 
            if the order should not have a TP.  
        magic (int): Magic number
        comment (String): Order comment
        expriation (long): Expiration time given as timestamp in seconds. 
            Can be zero if the order should not have an expiration time.  
    */
    public void OpenOrder(string symbol, string orderType, double lots, double price, double stopLoss, double takeProfit, int magic, string comment, long expiration)
    {
        string content = symbol + "," + orderType + "," + MTHelpers.Format(lots) + "," + MTHelpers.Format(price) + "," + MTHelpers.Format(stopLoss) + "," + MTHelpers.Format(takeProfit) + "," + magic + "," + comment + "," + expiration;
        SendCommand("OPEN_ORDER", content);
    }


    /*Sends a MODIFY_ORDER command to modify an order.

    Args:
        ticket (int): Ticket of the order that should be modified.
        price (double): Price of the (pending) order. Non-zero only 
            works for pending orders. 
        stop_loss (double): New stop loss price.
        take_profit (double): New take profit price. 
        expriation (long): New expiration time given as timestamp in seconds. 
            Can be zero if the order should not have an expiration time. 
    */
    public void ModifyOrder(int ticket, double price, double stopLoss, double takeProfit, long expiration)
    {
        string content = ticket + "," + MTHelpers.Format(price) + "," + MTHelpers.Format(stopLoss) + "," + MTHelpers.Format(takeProfit) + "," + expiration;
        SendCommand("MODIFY_ORDER", content);
    }


    /*Sends a CLOSE_ORDER command to close an order.

    Args:
        ticket (int): Ticket of the order that should be closed.
        lots (double): Volume in lots. If lots=0 it will try to 
            close the complete position. 
    */
    public void CloseOrder(int ticket, double lots = 0)
    {
        string content = ticket + "," + MTHelpers.Format(lots);
        SendCommand("CLOSE_ORDER", content);
    }


    /*Sends a CLOSE_ALL_ORDERS command to close all orders
    with a given symbol.

    Args:
        symbol (str): Symbol for which all orders should be closed. 
    */
    public void CloseAllOrders()
    {
        SendCommand("CLOSE_ALL_ORDERS", "");
    }


    /*Sends a CLOSE_ORDERS_BY_SYMBOL command to close all orders
    with a given symbol.

    Args:
        symbol (str): Symbol for which all orders should be closed. 
    */
    public void CloseOrdersBySymbol(string symbol)
    {
        SendCommand("CLOSE_ORDERS_BY_SYMBOL", symbol);
    }


    /*Sends a CLOSE_ORDERS_BY_MAGIC command to close all orders
    with a given magic number.

    Args:
        magic (str): Magic number for which all orders should 
            be closed. 
    */
    public void CloseOrdersByMagic(int magic)
    {
        SendCommand("CLOSE_ORDERS_BY_MAGIC", magic.ToString());
    }

    /*Sends a RESET_COMMAND_IDS command to reset stored command IDs. 
    This should be used when restarting the java side without restarting 
    the mql side.
    */
    public void ResetCommandIDs()
    {
        _commandId = 0;

        SendCommand("RESET_COMMAND_IDS", "");

        // sleep to make sure it is read before other commands.
        Thread.Sleep(500);
    }


    /*Sends a command to the mql server by writing it to 
    one of the command files. 

    Multiple command files are used to allow for fast execution 
    of multiple commands in the correct chronological order. 
    */
    void SendCommand(string command, string content)
    {
        // Need lock so that different threads do not use the same 
        // commandID or write at the same time.
        lock (this)
        {
            _commandId = (_commandId + 1) % 100000;

            string text = "<:" + _commandId + "|" + command + "|" + content + ":>";

            DateTime now = DateTime.UtcNow;
            DateTime endTime = DateTime.UtcNow + new TimeSpan(0, 0, _maxRetryCommandSeconds);

            // trying again for X seconds in case all files exist or are 
            // currently read from mql side. 
            while (now < endTime)
            {
                // using 10 different files to increase the execution speed 
                // for muliple commands. 
                bool success = false;
                for (int i = 0; i < MaxCommandFiles; i++)
                {
                    string filePath = _pathCommandsPrefix + i + ".txt";
                    if (!File.Exists(filePath) && MTHelpers.TryWriteToFile(filePath, text))
                    {
                        success = true;
                        break;
                    }
                }
                if (success) break;
                Thread.Sleep(_sleepDelayMilliseconds);
                now = DateTime.UtcNow;
            }
        }
    }

}