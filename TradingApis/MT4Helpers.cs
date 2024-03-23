using System.Globalization;

namespace TradingApis;

public class MT4Helpers
{

    /*Prints to console output.

    Args:
        obj (Object): Object to print.

    */
    public static void Print(object obj)
    {
        Console.WriteLine(obj);
    }


    /*Tries to write to a file.

    Args:
        filePath (string): file path of the file.
        text (string): text to write.

    */
    public static bool TryWriteToFile(string filePath, string text)
    {
        try
        {
            File.WriteAllText(filePath, text);
            return true;
        }
        catch
        {
            return false;
        }
    }


    /*Tries to delete a file.

    Args:
        filePath (string): file path of the file.

    */
    public static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
        }
    }


    /*Formats a double value to string.

    Args:
        value (double): numeric value to format.

    */
    public static string Format(double value)
    {
        return value.ToString("G", CultureInfo.CreateSpecificCulture("en-US"));
    }

    public static string TryReadFile(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine("MT4Helpers.TryReadFile(): DirectoryNotFoundException. Returning empty string");
            return "";
        }
    }
}