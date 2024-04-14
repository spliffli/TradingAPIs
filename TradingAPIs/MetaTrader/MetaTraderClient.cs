using System.Collections;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json.Linq;
using TradingAPIs.Common;
using TradingAPIs.Common.Loggers;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using ThreadState = System.Threading.ThreadState;

namespace TradingAPIs.MetaTrader;
public class MetaTraderClient : IConnectionClient
{
    private MTConfiguration _config;
    private readonly IMTEventHandler _eventHandler;
    private string _metaTraderDirPath;  // { get; private set; }
    private bool _developMql = false;
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

    private JObject _messages = new JObject();
    private JObject _openOrders = new JObject();
    private JObject _accountInfo = new JObject();
    private JObject _marketData = new JObject();
    private JObject _barData = new JObject();
    private JObject _historicData = new JObject();
    private JObject _historicTrades = new JObject();

    public JObject Messages { get { return _messages; } }
    public JObject OpenOrders { get { return _openOrders; } }
    public JObject AccountInfo { get { return _accountInfo; } }
    public JObject MarketData { get { return _marketData; } }
    public JObject BarData { get { return _barData; } }
    public JObject HistoricData { get { return _historicData; } }
    public JObject HistoricTrades { get { return _historicTrades; } }

    private JObject _lastBarData = new JObject();
    private JObject _lastMarketData = new JObject();

    public bool Active = true;
    private bool _start = false;

    private readonly Thread _messageThread;
    private readonly Thread _openOrdersThread;
    private readonly Thread _marketDataThread;
    private readonly Thread _barDataThread;
    private readonly Thread _historicDataThread;

    private readonly ThreadStates _threadStates;

    public bool SubscribesToTickData { get; set; }
    public bool SubscribesToBarData { get; set; }
    
    // public bool StartCheckHistoricDataThread { get; set; }
    public string[] SymbolsMarketData { get; set; }
    public string[,] SymbolsBarData { get; set; }

    // public MetaTraderClient(MTConfiguration config)
    // {
    //     Console.WriteLine("MTConnectionClient | Initializing from MTConfiguration");
    // 
    //     if (config == null)
    //         throw new ArgumentException("config cannot be null.");
    //
    //     _config = config;
    // }

    // Constructor definition with parameters for configuration
    public MetaTraderClient(MTConfiguration config, MTEventHandler eventHandler, Logger? logger = null)
    {
        if (logger == null)
        {
            _logger = new ConsoleLogger();
            _logger.Log("MTConnectionClient | No logger was passed so defaulting to ConsoleLogger");
        }
        else
            _logger = logger;

        _logger.Log("MTConnectionClient | Initializing...");

        if (config == null)
            throw new ArgumentException("config cannot be null.");

        _config = config;

        if (!_config.StartMessageThread && !_config.StartOpenOrdersThread && !_config.StartMarketDataThread && !_config.StartBarDataThread && !_config.StartHistoricDataThread)
            throw new ArgumentException("At least one of the threads must be configured to start for the client to initialize.");

        if (_config.SubscribeToTickData && !_config.StartMarketDataThread)
            throw new ArgumentException("If SubscribeToTickData is true, StartMarketDataThread must also be true.");

        if (_config.SubscribeToBarData && !_config.StartBarDataThread)
            throw new ArgumentException("If SubscribeToBarData is true, StartBarDataThread must also be true.");

        if (_config.SubscribeToTickData)
        {
            SubscribesToTickData = true;

            if (_config.SymbolsMarketData == null || _config.SymbolsMarketData.Length == 0)
                throw new ArgumentException("SymbolsMarketData cannot be null or empty if SubscribeToTickData is true.");

            SymbolsMarketData = _config.SymbolsMarketData;
        }

        if (_config.SubscribeToBarData)
        {
            SubscribesToBarData = true;

            if (_config.SymbolsBarData == null || _config.SymbolsBarData.Length == 0)
                throw new ArgumentException("SymbolsBarData cannot be null or empty if SubscribeToBarData is true.");

            SymbolsBarData = _config.SymbolsBarData;
        }

        // Assign the constructor parameters to the class's fields
        _eventHandler = eventHandler;                               // Event handler for managing MT4 events
        _metaTraderDirPath = _config.MetaTraderDirPath;             // Directory path of the MetaTrader installation
        _sleepDelayMilliseconds = _config.SleepDelayMilliseconds;   // Sleep delay in milliseconds between checks
        _maxRetryCommandSeconds = _config.MaxRetryCommandSeconds;   // Maximum time in seconds to retry a command
        _loadOrdersFromFile = _config.LoadOrdersFromFile;           // Flag to load orders from a file
        _verbose = _config.Verbose;                                 // Verbose mode flag

        // Check if the MetaTrader directory path exists; if not, exit the program
        if (!Directory.Exists(_metaTraderDirPath))
        {
            Console.WriteLine("ERROR: metaTraderDirPath does not exist! metaTraderDirPath: " + _metaTraderDirPath);
            Environment.Exit(1);
        }

        // Initialize the paths for various types of data files within the MetaTrader directory
        if (_verbose)
            _logger.Log("MTConnectionClient | Initializing paths for data files within MetaTrader directory.");
        _pathOrders = Path.Join(_metaTraderDirPath, "DWX", "DWX_Orders.txt");
        _pathMessages = Path.Join(_metaTraderDirPath, "DWX", "DWX_Messages.txt");
        _pathMarketData = Path.Join(_metaTraderDirPath, "DWX", "DWX_Market_Data.txt");
        _pathBarData = Path.Join(_metaTraderDirPath, "DWX", "DWX_Bar_Data.txt");
        _pathHistoricData = Path.Join(_metaTraderDirPath, "DWX", "DWX_Historic_Data.txt");
        _pathHistoricTrades = Path.Join(_metaTraderDirPath, "DWX", "DWX_Historic_Trades.txt");
        _pathOrdersStored = Path.Join(_metaTraderDirPath, "DWX", "DWX_Orders_Stored.txt");
        _pathMessagesStored = Path.Join(_metaTraderDirPath, "DWX", "DWX_Messages_Stored.txt");
        _pathCommandsPrefix = Path.Join(_metaTraderDirPath, "DWX", "DWX_Commands_");
        if (_verbose)
            _logger.Log("MTConnectionClient | Paths initialized.");

        _logger.Log("MTConnectionClient | Loading messages from file.");
        LoadMessages(); // Load the initial messages from the file

        if (_config.LoadOrdersFromFile)
        {
            _logger.Log("MTConnectionClient | Loading orders from file, as specified.");
            LoadOrders(); // Load the initial orders from the file, if specified
        }

        // Initialize the threads for checking messages, open orders, market data, bar data, and historic data
        _messageThread = new Thread(() => CheckMessages());
        _openOrdersThread = new Thread(() => CheckOpenOrders());
        _marketDataThread = new Thread(() => CheckMarketData());
        _barDataThread = new Thread(() => CheckBarData());
        _historicDataThread = new Thread(() => CheckHistoricData());

        _threadStates = new ThreadStates(_messageThread, _openOrdersThread, _marketDataThread, _barDataThread, _historicDataThread);

        // Start the threads which are configured to start
        _logger.Log("MTConnectionClient | Starting threads for continuous data processing...");

        if (_config.StartMessageThread)
        {
            _logger.Log("MTConnectionClient | Starting thread for checking messages.");
            _messageThread.Start();
        }
        else
        {
            _logger.Log("MTConnectionClient | Not starting thread for checking messages. WARNING: This might stop the MetaTrader connection client from working properly.");
        }

        if (_config.StartOpenOrdersThread)
        {
            _logger.Log("MTConnectionClient | Starting thread for checking open orders.");
            _openOrdersThread.Start();
        }
        else
        {
            _logger.Log("MTConnectionClient | Not starting thread for checking open orders.");
        }

        if (_config.StartMarketDataThread)
        {
            _logger.Log("MTConnectionClient | Starting thread for checking market data.");
            _marketDataThread.Start();
        }
        else
        {
            _logger.Log("MTConnectionClient | Not starting thread for checking market data.");
        }

        if (_config.StartBarDataThread)
        {
            _logger.Log("MTConnectionClient | Starting thread for checking bar data.");
            _barDataThread.Start();
        }
        else
        {
            _logger.Log("MTConnectionClient | Not starting thread for checking bar data.");
        }

        if (_config.StartHistoricDataThread)
        {
            _logger.Log("MTConnectionClient | Starting thread for checking historic data.");
            _historicDataThread.Start();
        }
        else
        {
            _logger.Log("MTConnectionClient | Not starting thread for checking historic data.");
        }

        _logger.Log("MTConnectionClient | Threads initialized and started.");

        _logger.Log("MTConnectionClient | Resetting command IDs to their initial state.");
        ResetCommandIDs(); // Reset command IDs to their initial state

        // Start processing. If an event handler is provided, delay the start slightly to ensure readiness
        if (eventHandler == null)
        {
            _logger.Log("MTConnectionClient | Starting processing without an event handler.");
            Start();
        }
        else
        {
            Thread.Sleep(1000); // Wait for 1 second before starting
            _logger.Log("MTConnectionClient | Starting processing with an event handler.");
            Start();
            // eventHandler.Start(this); // Start the event handler, passing this instance for context
        }

        _logger.Log("MTConnectionClient | Initialization complete.");
    }

    public MetaTraderClient(string pathHistoricTrades)
    {
        _pathHistoricTrades = pathHistoricTrades;
    }

    /*START can be used to check if the client has been initialized.  
    */
    public void Start()
    {
        _logger.Log("MTConnectionClient.Start | Starting the client.");
        _start = true;

        _logger.Log("MTConnectionClient.Start | Thread States:");
        // foreach (var x in GetThreadStates())
        // {
        //     _logger.Log("Thread: " + x.Key + " | State: " + x.Value);
        // }

        Console.WriteLine(_threadStates.ToString());

        // if (!SubscribeToTickData && !SubscribeToBarData)
        //     throw new ArgumentException(
        //         "At least one of SubscribeSymbolsMarketData or SubscribeSymbolsBarData must be true to start the MT4EventHandler.");

        // Logic to Start handling events from the MT4 instance
        // account information is stored in client.AccountInfo.
        // open orders are stored in client.OpenOrders.
        // historic trades are stored in client.HistoricTrades.

        Console.WriteLine("\nAccount info:\n" + _accountInfo + "\n");

        if (SubscribesToTickData)
        {
            // subscribe to tick data:
            // string[] SymbolsMarketData = { "EURUSD", "GBPUSD" };
            Console.WriteLine("Subscribing to tick data.");
            SubscribeSymbolsMarketData(SymbolsMarketData);
        }

        if (SubscribesToBarData)
        {
            // subscribe to bar data:
            Console.WriteLine("Subscribing to bar data.");
            // string[,] symbolsBarData = new string[,] { { "EURUSD", "M1" }, { "AUDCAD", "M5" }, { "GBPCAD", "M15" } };
            SubscribeSymbolsBarData(SymbolsBarData);
        }
    }


    private Dictionary<string, ThreadState> GetThreadStates()
    {
        var ThreadStates = new Dictionary<string, ThreadState>();

        ThreadStates.Add("messageThread", GetThreadState(_messageThread));
        ThreadStates.Add("openOrdersThread", GetThreadState(_openOrdersThread));
        ThreadStates.Add("marketDataThread", GetThreadState(_marketDataThread));
        ThreadStates.Add("barDataThread", GetThreadState(_barDataThread));
        ThreadStates.Add("historicDataThread", GetThreadState(_historicDataThread));

        return ThreadStates;
    }

    private static ThreadState GetThreadState(Thread thread)
    {
        if (thread == null)
            return ThreadState.Unstarted;
        return thread.ThreadState;
    }

    private static bool IsThreadRunning(Thread thread)
    {
        if (thread == null || !thread.IsAlive)
             return false;

        if (thread.ThreadState == ThreadState.Running)
            return true;
        return false;
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
            foreach (var x in _openOrders)
            {
                // JToken value = x.Value;
                if (dataOrders[x.Key] == null)
                {
                    newEvent = true;
                    if (_verbose)
                        Console.WriteLine("Order removed: " + _openOrders[x.Key].ToString());
                }
            }
            foreach (var x in dataOrders)
            {
                // JToken value = x.Value;
                if (_openOrders[x.Key] == null)
                {
                    newEvent = true;
                    if (_verbose)
                        Console.WriteLine("New order: " + dataOrders[x.Key].ToString());
                }
            }

            _openOrders = dataOrders;
            _accountInfo = (JObject)data["account_info"];

            if (_loadOrdersFromFile)
                MTHelpers.TryWriteToFile(_pathOrdersStored, data.ToString());

            if (_eventHandler != null && newEvent)
                _eventHandler.OnOrderEvent(this);
        }
    }


    /*Regularly checks the file for messages and triggers
    the eventHandler.OnMessage() function.
    */
    private void CheckMessages(CancellationToken token = default)
    {
        while (Active && !token.IsCancellationRequested)
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

            _marketData = data;

            if (_eventHandler != null)
            {
                foreach (var x in _marketData)
                {
                    string symbol = x.Key;
                    if (_lastMarketData[symbol] == null || !_marketData[symbol].Equals(_lastMarketData[symbol]))
                    {
                        // JObject jo = (JObject)marketData[symbol];
                        _eventHandler.OnTick(this, symbol,
                                            (double)_marketData[symbol]["bid"],
                                            (double)_marketData[symbol]["ask"]);
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

            _barData = data;

            if (_eventHandler != null)
            {
                foreach (var x in _barData)
                {
                    string st = x.Key;
                    if (_lastBarData[st] == null || !_barData[st].Equals(_lastBarData[st]))
                    {
                        string[] stSplit = st.Split("_");
                        if (stSplit.Length != 2)
                            continue;
                        // JObject jo = (JObject)barData[symbol];
                        _eventHandler.OnBarData(this, stSplit[0], stSplit[1],
                                               (string)_barData[st]["time"],
                                               (double)_barData[st]["open"],
                                               (double)_barData[st]["high"],
                                               (double)_barData[st]["low"],
                                               (double)_barData[st]["close"],
                                               (int)_barData[st]["tick_volume"]);
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
                        _historicData[x.Key] = data[x.Key];
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
                    _historicTrades = data;

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
        _openOrders = (JObject)data["orders"];
        _accountInfo = (JObject)data["account_info"];
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

    public bool CheckIfMetaTraderIsInstalled()
    {
        return Directory.Exists(_metaTraderDirPath);
    }

    public Process[] GetTerminalProcesses()
    {
        Process[] processes = Process.GetProcessesByName("terminal64");
        if (processes.Length == 0)
            processes = Process.GetProcessesByName("terminal32");
        if (processes.Length == 0)
            processes = Process.GetProcessesByName("terminal");

        List<Process> terminalProcesses = new List<Process>();
        
        foreach (Process p in processes)
        {
            var fileDescription = FileVersionInfo.GetVersionInfo(p.MainModule.FileName).FileDescription;

            if (fileDescription.Contains("MetaTrader") && p.MainWindowTitle.Contains(_config.AccountId))
                terminalProcesses.Add(p);
        }

        return terminalProcesses.ToArray();
    }

    public Process? GetTerminalProcess()
    {
        if (_config.AccountId == null)
            throw new ArgumentException("accountId cannot be null. Set the value in your config.ini file, or when you initialize an MTConfiguration object in the source code.");

        Process[] terminalProcesses = GetTerminalProcesses();


        foreach (Process p in terminalProcesses)
        {
            if (p.MainWindowTitle.Contains(_config.AccountId))
                return p;
        }

        return null;
    }

    public bool CheckIfTerminalIsRunning()
    {
        return GetTerminalProcess() != null;
    }

    /*----------------------------------------------------------------*
     * BELOW ARE THE METHODS WHICH SEND COMMANDS TO THE MQL SERVER EA *
     *----------------------------------------------------------------*/

    public bool CheckIfServerEaIsRunning()
    {
        var tokenSource = new CancellationTokenSource();
        Thread tempMessageThread = new Thread(() => CheckMessages(tokenSource.Token));

        if (!_messageThread.IsAlive || !(_messageThread.ThreadState == ThreadState.Running || _messageThread.ThreadState == ThreadState.WaitSleepJoin))
            Console.WriteLine("Client's messageThread isn't running so starting tempMessageThread while checking if the server EA is running.");
            tempMessageThread.Start();

        int timeoutSeconds = 10;
        var lastMessagesStrBeforeCommand = _lastMessagesStr;
        var lastMessagesDataBeforeCommand = JObject.Parse(_lastMessagesStr);
        var lastMessageBeforeCommand = lastMessagesDataBeforeCommand.Last;

        bool isRunning = false;

        if (_verbose)
            Console.WriteLine($"lastMessageBeforeCommand: {lastMessageBeforeCommand}");

        GetHistoricTrades(1);

        Stopwatch sw = new Stopwatch();
        sw.Start();

        while (sw.Elapsed.TotalSeconds < timeoutSeconds)
        {
            if (_lastMessagesStr != lastMessagesStrBeforeCommand)
            {
                JObject lastMessagesDataAfterCommand = JObject.Parse(_lastMessagesStr);

                var lastMessageAfterCommand = lastMessagesDataAfterCommand.Last;

                if (_verbose)
                    Console.WriteLine($"lastMessageAfterCommand: {lastMessageAfterCommand}");

                // if (_lastMessagesStr.Contains)
                // return true;
                isRunning = true;
                break;
            }

            Thread.Sleep(100);
        }

        if (!isRunning)
            Console.WriteLine("Server EA is not running.");

        
        tokenSource.Cancel();   // Request cancellation. 

        if (tempMessageThread.IsAlive || tempMessageThread.ThreadState == ThreadState.Running || tempMessageThread.ThreadState == ThreadState.WaitSleepJoin)
            tempMessageThread.Join();      // To wait for cancellation, `Join` blocks the calling thread until the thread represented by this instance terminates.
        else if (_messageThread.IsAlive || _messageThread.ThreadState == ThreadState.Running || _messageThread.ThreadState == ThreadState.WaitSleepJoin)
            _messageThread.Join();
        else
            throw new InvalidOperationException("_messageThread & tempMessageThread are not running. Cannot check if server EA is running");

        tokenSource.Dispose();  // Dispose the token source.
        
        return isRunning;
    }

    /*Sends a SUBSCRIBE_SYMBOLS command to subscribe to market (tick) data.

    Args:
        symbols (string[]): List of symbols to subscribe to.

    Returns:
        null

        The data will be stored in marketData. 
        On receiving the data the eventHandler.OnTick() 
        function will be triggered. 
    */
    public void SubscribeSymbolsMarketData(string[] symbols)
    {
        if (_marketDataThread == null || !_marketDataThread.IsAlive)
        {
            // _marketDataThread = new Thread(() => CheckMarketData());
            // _marketDataThread.Start();

            throw new ArgumentException("Market data thread is not running so cannot subscribe to market data. In the MTConfiguration, if SubscribeToTickData = true, ensure that StartMarketDataThread = true as well to avoid this exception.");
        }
        SendCommand("SUBSCRIBE_SYMBOLS", string.Join(",", symbols));
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
        if (_barDataThread == null || !_barDataThread.IsAlive)
        {
            // _barDataThread = new Thread(() => CheckBarData());
            // _barDataThread.Start();

            throw new ArgumentException("Bar data thread is not running so cannot subscribe to bar data. In the MTConfiguration, if SubscribeToBarData = true, ensure that StartBarDataThread = true as well to avoid this exception.");
        }

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
    public void GetHistoricData(string symbol, string timeFrame, long start, long end)
    {
        if (_historicDataThread == null || !_historicDataThread.IsAlive)
        {
            // _historicDataThread = new Thread(() => CheckHistoricData());
            // _historicDataThread.Start();

            throw new ArgumentException("Historic data thread is not running so cannot get historic data. In the MTConfiguration, if StartHistoricDataThread = true, ensure that StartCheckHistoricDataThread = true as well to avoid this exception.");
        }

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
			lots (double): Volume in lots
			price (double): Price of the (pending) order. Non-zero only 
				works for pending orders. 
			stop_loss (double): New stop loss price.
			take_profit (double): New take profit price. 
			expriation (long): New expiration time given as timestamp in seconds. 
				Can be zero if the order should not have an expiration time. 
		*/
    public void ModifyOrder(int ticket, double lots, double price, double stopLoss, double takeProfit, long expiration)
    {
        string content = ticket + "," + MTHelpers.Format(lots) + "," + MTHelpers.Format(price) + "," + MTHelpers.Format(stopLoss) + "," + MTHelpers.Format(takeProfit) + "," + expiration;
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

    NOTE: Moved the code from the closeAllOrders() method 
    from MetaTraderClientDWXUnitTest.cs to this method,
    which checks the order count and keeps sending the
    command until all orders are closed.
    
    As long as there are open orders, it will send new 
	commands every second. This is needed because of 
	possible requotes or other errors when closing an order.
    */
    public bool CloseAllOrders(bool TryUntilTimeout = true, int timeoutSeconds = 10)
    {
        if (!TryUntilTimeout)
        {
            SendCommand("CLOSE_ALL_ORDERS", "");
            return true;
        }

        DateTime now = DateTime.UtcNow;
        DateTime endTime = DateTime.UtcNow + new TimeSpan(0, 0, timeoutSeconds);
        while (now < endTime)
        {
            // sometimes it could fail if for example there is a requote. so just try again. 
            SendCommand("CLOSE_ALL_ORDERS", "");
            Thread.Sleep(1000);
            now = DateTime.UtcNow;
            if (_openOrders.Count == 0)
                return true;
        }
        return false;
    }

    public void SafeCloseAllOrders()
    {
        _logger.Log("Safe closing all orders...");
        if (CloseAllOrders())
        {
            _logger.Log("Sucessfully closed all orders.");
            return;
        }
        else
        {
            // if it fails, try to close all orders by symbol. 
            _logger.Log("CloseAllOrders timed out. Trying to close all orders by symbol.");
            foreach (var x in _openOrders)
            {
                CloseOrdersBySymbol(x.Key);
            }

            if (_openOrders.Count != 0)
            {
                _logger.Log("CloseAllOrdersBySymbol failed. Calling SafeCloseAllOrders again from the beginning.");
                SafeCloseAllOrders();
            }
            else
                _logger.Log("All orders closed by symbol.");
            return;
        }
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

        if (!_developMql)
            SendCommand("RESET_COMMAND_IDS", "");
        else
            SendCommand("RESET_COMMAND_IDS_DEVELOP", ""); // TODO: Update this command to take the correct arguments.
                                                          // It will throw an exception currently since this command won't be recognized on the mql side.

        // sleep to make sure it is read before other commands.
        Thread.Sleep(500);
    }


    /*Sends a command to the mql server by writing it to 
    one of the command files. 

    Multiple command files are used to allow for fast execution 
    of multiple commands in the correct chronological order. 

    Note: Modified so that it returns the commandId
    */
    int SendCommand(string command, string content)
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

            return _commandId;
        }
    }

    /*--------------------------------------------------------------------------------------*
     * BELOW ARE THE METHODS WHICH SEND CUSTOM COMMANDS ADDED BY: Jonathon Quick (spliffli) *
     *--------------------------------------------------------------------------------------*/

    public MqlParams GetMqlParams()
    {
        throw new NotImplementedException();
    }

    public string WaitForMqlResponseMessage(int commandId)
    {
        if (!IsThreadRunning(_messageThread))
            throw new InvalidOperationException("Message thread is not running. Cannot wait for MQL response.");

        throw new NotImplementedException();

        DateTime now = DateTime.UtcNow;
        DateTime endTime = DateTime.UtcNow + new TimeSpan(0, 0, _maxRetryCommandSeconds);

        // trying again for X seconds in case all files exist or are 
        // currently read from mql side. 
        while (now < endTime)
        {
            bool success = false;
            
            if (success) break;
            Thread.Sleep(_sleepDelayMilliseconds);
            now = DateTime.UtcNow;
        }
    }

    private string GetMqlParamValueStr(string paramName)
    {
        int commandId = SendCommand("GET_PARAM_VALUE", paramName);

        string response = WaitForMqlResponseMessage(commandId);

        return response;
    }

    private bool SetMqlParamValueStr(string paramName, string paramValueStr, bool waitForConfirmation = true)
    {
        throw new NotImplementedException();
    }

    internal int GetMqlIntParam(string paramName)
    {
        string paramValueStr = GetMqlParamValueStr(paramName);
        
        return Int32.Parse(paramValueStr);
    }

    internal bool SetMqlIntParam(string paramName, int value)
    {
        throw new NotImplementedException();

        string paramValueStr = value.ToString();

        return SetMqlParamValueStr(paramName, paramValueStr);
    }

    internal bool GetMqlBoolParam(string paramName)
    {
        throw new NotImplementedException();
    }

    internal bool SetMqlBoolParam(string paramName, bool value)
    {
        throw new NotImplementedException();
    }

    internal double GetMqlDoubleParam(string v)
    {
        throw new NotImplementedException();
    }

    internal void SetMqlDoubleParam(string v, double value)
    {
        throw new NotImplementedException();
    }

    public bool CheckIfMarketIsOpen()
    {
        var now = DateTime.UtcNow;
        var dayOfWeek = now.DayOfWeek;

        if (dayOfWeek == DayOfWeek.Saturday)
            return false;
        else if (dayOfWeek == DayOfWeek.Sunday)
            if (now.Hour > 22)
                return true;
            else
                return false;
        else if (dayOfWeek == DayOfWeek.Friday)
            if (now.Hour < 22)
                return true;
            else
                return false;
        else
            return true;
    }
}
