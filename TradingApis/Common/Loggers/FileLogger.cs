namespace TradingApis.Common.Loggers;

public class FileLogger : Logger
{
    private string _logFilePath;
    private readonly bool _logToConsole;

    public FileLogger(string logFilePath, bool logToConsole=false)
    {
        _logFilePath = logFilePath;
        _logToConsole = logToConsole;
    }

    public override void Log(string message)
    {
        var formattedMessage = this.FormatMessage(message);
        File.AppendAllText(_logFilePath, formattedMessage + Environment.NewLine);

        if (_logToConsole)
        {
            Console.WriteLine(formattedMessage);
        }
    }
}