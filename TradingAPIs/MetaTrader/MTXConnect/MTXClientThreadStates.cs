namespace TradingAPIs.MetaTrader.MTXConnect;

struct MTXClientThreadStates
{
    private readonly Thread _messageThread;
    private readonly Thread _openOrdersThread;
    private readonly Thread _marketDataThread;
    private readonly Thread _barDataThread;
    private readonly Thread _historicDataThread;

    public ThreadState MessageThread { get { return _messageThread.ThreadState; } }
    public ThreadState OpenOrdersThread { get { return _openOrdersThread.ThreadState; } }
    public ThreadState MarketDataThread { get { return _marketDataThread.ThreadState; } }
    public ThreadState BarDataThread { get { return _barDataThread.ThreadState; } }
    public ThreadState HistoricDataThread { get { return _historicDataThread.ThreadState; } }

    public MTXClientThreadStates(Thread messageThread, Thread openOrdersThread, Thread marketDataThread, Thread barDataThread, Thread historicDataThread)
    {
        _messageThread = messageThread;
        _openOrdersThread = openOrdersThread;
        _marketDataThread = marketDataThread;
        _barDataThread = barDataThread;
        _historicDataThread = historicDataThread;
    }

    public string ToString()
    {
        return "MessageThread: " + MessageThread
            + "\nOpenOrdersThread: " + OpenOrdersThread
            + "\nMarketDataThread: " + MarketDataThread
            + "\nBarDataThread: " + BarDataThread
            + "\nHistoricDataThread: " + HistoricDataThread;
    }
}
