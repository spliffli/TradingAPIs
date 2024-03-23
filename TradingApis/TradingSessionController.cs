using System;

namespace TradingApis;

public abstract class TradingSessionController
{
    private readonly Thread _instanceThread;
    private readonly Logger _logger;
    private readonly IEventHandler _eventHandler;

    public bool IsRunning { get; private set; }

    public TradingSessionController(IEventHandler eventHandler, Logger logger)
    {
        // Initialize the config and the thread
        // _config = config;

        _instanceThread = new Thread(new ThreadStart(Run));
        _logger = logger;
        _eventHandler = eventHandler;
    }
    public void StartThread()
    {
        _logger.Log("TradingSessionController.StartThread(): Starting the trading session controller thread");
        _instanceThread.Start();
        IsRunning = true;
    }

    public void StopThread()
    {
        _logger.Log("TradingSessionController.StopThread(): Stopping the trading session controller thread");
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