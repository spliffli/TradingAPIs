using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NetMQ;
// using NetMQ.Monitoring;

namespace TradingAPIs.MetaTraderZmq;

internal class ZeroMQConnector
{
    public bool IsConnected { get; private set; } = false;
    public bool IsActive { get; private set; } = false;
    public string ClientId { get; init; }
    public string Host { get; init; }
    public string Protocol { get; init; }
    public string Url => $"{Protocol}://{Host}:";
    public int PushPort { get; init; }
    public int PullPort { get; init; }
    public int SubPort { get; init; }
    public char Delimiter { get; init; }
    public object[]? PullDataHandlers { get; init; }
    public object[]? SubDataHandlers { get; init; }
    public bool Verbose { get; init; }
    public int PollTimeoutMs { get; init; }
    public int SleepDelayMs { get; init; }
    public bool ShouldMonitor { get; init; }
    public ZeroMQConnector(string clientId      = "DefaultZeroMQConnector",
                           string host          = "localhost",
                           string protocol      = "tcp",
                           int pushPort         = 32768,
                           int pullPort         = 32769,
                           int subPort          = 32770,
                           char delimiter       = ';',
                           object[]? pullDataHandlers = null,
                           object[]? subDataHandlers  = null,
                           bool verbose         = true,
                           int pollTimeoutMs    = 1000,
                           int sleepDelayMs     = 1000,
                           bool shouldMonitor   = false)
    {
        IsActive = true;
        ClientId = clientId;
        Host = host;
        Protocol = protocol;
        PushPort = pushPort;
        PullPort = pullPort;
        SubPort = subPort;
        Delimiter = delimiter;
        PullDataHandlers = pullDataHandlers;
        SubDataHandlers = subDataHandlers;
        Verbose = verbose;
        PollTimeoutMs = pollTimeoutMs;
        SleepDelayMs = sleepDelayMs;
        ShouldMonitor = shouldMonitor;
    }
}
