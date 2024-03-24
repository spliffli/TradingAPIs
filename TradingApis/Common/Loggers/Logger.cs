using System.Globalization;

namespace TradingApis.Common.Loggers;

public abstract class Logger
{
    public abstract void Log(string message);

    public string FormatMessage(string message)
    {
        return $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}: {message}";
    }
}

