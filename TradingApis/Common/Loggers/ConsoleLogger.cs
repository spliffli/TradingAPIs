namespace TradingAPIs.Common.Loggers;

public class ConsoleLogger : Logger
{
    public override void Log(string message)
    {
        var formattedMessage = this.FormatMessage(message);
        Console.WriteLine(formattedMessage);
    }
}