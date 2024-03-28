using TradingAPIs.Common.Loggers;
using TradingAPIs.Common.Orders;

namespace TradingAPIs.Common;

public abstract class TradingSessionController
{
    private readonly Thread _instanceThread;
    private readonly Logger _logger;
    private readonly IConnectionClient _connectionClient;

    public bool IsRunning { get; private set; }

    internal TradingSessionController(IConnectionClient connectionClient, Logger logger)
    {
        // Initialize the config and the thread
        // _config = config;

        _instanceThread = new Thread(new ThreadStart(Run));
        _logger = logger;
        _connectionClient = connectionClient;
    }
    public void StartThread()
    {
        _logger.Log("TradingSessionController.StartThread | Starting the trading session controller thread");
        _instanceThread.Start();
        IsRunning = true;
    }

    public void StopThread()
    {
        _logger.Log("TradingSessionController.StopThread | Stopping the trading session controller thread");
        _instanceThread.Abort(); // This is not recommended, but it's a simple example
        IsRunning = false;
    }
    
    public abstract void Run();
    public abstract void SubscribeToTickData();
    public abstract void UnsubscribeToTickData();
    public abstract IOrder PlaceOrder(IOrder order);
    public abstract IOrder ModifyOrder(IOrder order);
    public abstract IOrder CloseOrder(IOrder order);
}