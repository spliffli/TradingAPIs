using TradingAPIs.Common;
using TradingAPIs.Common.Loggers;
using TradingAPIs.Common.Orders;

namespace TradingAPIs.MetaTrader;

public class MTSessionController : TradingSessionController
{
    private Thread _instanceThread;
    private MTConfiguration _config;
    private MTEventHandler _eventHandler;
    private MTConnectionClient _client;
    private Logger _logger;


    public MTSessionController(MTConfiguration config, MTConnectionClient client, Logger logger) 
        : base(client, logger)
    {
        // Initialize the config and the thread
        _config = config;
        _instanceThread = new Thread(new ThreadStart(Run));
        _client = client;
        _logger = logger;
    }

    public override void Run()
    {
        // Start the connection client
        // _client = new MTConnectionClient(_eventHandler, _config.MetaTraderDirPath, _logger);
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
