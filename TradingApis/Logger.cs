using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;


namespace TradingApis;

public abstract class Logger
{
    public abstract void Log(string message);

    public string FormatMessage(string message)
    {
        return $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}: {message}";
    }
}

