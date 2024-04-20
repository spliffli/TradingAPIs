using TradingAPIs.Common;
using TradingAPIs.Common.Loggers;
using TradingAPIs.Common.Orders;

namespace TradingAPIs.MetaTrader.MTXConnect;

public class MTXConnectSession : ClientSession
{
    private Thread _instanceThread;
    private MTXConfig _config;
    private MTXEventHandler _eventHandler;
    private MTXClient _client;
    private Logger _logger;


    public MTXConnectSession(MTXConfig config, MTXClient client, Logger logger)
        : base(client, logger)
    {
        // Initialize the config and the thread
        _config = config;
        // _instanceThread = new Thread(new ThreadStart(Run));
        _client = client;
        _logger = logger;
    }

    public override void SubscribeToTickData()
    {
        throw new NotImplementedException();
    }

    public override void UnsubscribeToTickData()
    {
        throw new NotImplementedException();
    }

    public override IOrder PlaceOrder(IOrder order)
    {
        throw new NotImplementedException();
    }

    public override IOrder ModifyOrder(IOrder order)
    {
        throw new NotImplementedException();
    }

    public override IOrder CloseOrder(IOrder order)
    {
        throw new NotImplementedException();
    }
}
