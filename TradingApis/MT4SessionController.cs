using System.Globalization;

namespace TradingApis;

public class MT4SessionController : TradingSessionController
{
    private Thread _instanceThread;
    private MT4Configuration _config;
    private MT4EventHandler _eventHandler;
    private MT4ConnectionClient _client;
    private Logger _logger;


    public MT4SessionController(MT4Configuration config, MT4EventHandler eventHandler, Logger logger) 
        : base(eventHandler, logger)
    {
        // Initialize the config and the thread
        _config = config;
        _instanceThread = new Thread(new ThreadStart(Run));
        _eventHandler = eventHandler;
        _logger = logger;
    }

    public override void Run()
    {
        // Start the connection client
        _client = new MT4ConnectionClient(_eventHandler, _config.MetaTraderDirPath, _logger);
        _client.Start();
    }

    public override void SubscribeToTickData()
    {
        throw new System.NotImplementedException();
    }

    public override void UnsubscribeToTickData()
    {
        throw new System.NotImplementedException();
    }

    public override IOrder PlaceOrder(IOrder order)
    {
        throw new System.NotImplementedException();
    }

    public override IOrder ModifyOrder(IOrder order)
    {
        throw new System.NotImplementedException();
    }

    public override IOrder CloseOrder(IOrder order)
    {
        throw new System.NotImplementedException();
    }
}
