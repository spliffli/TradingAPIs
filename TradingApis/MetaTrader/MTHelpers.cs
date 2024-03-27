using System.Globalization;

namespace TradingApis.MetaTrader;

public class MTHelpers
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
        var fileName = GetFileNameFromPath(path);

        try
		{
			return File.ReadAllText(path);
		}
		catch (DirectoryNotFoundException)
		{
			Console.WriteLine("MT4Helpers.TryReadFile(): DirectoryNotFoundException. Returning empty string");
			return "";
		}
		catch (FileNotFoundException)
		{
			Console.WriteLine(
				$"MT4Helpers.TryReadFile(): FileNotFoundException. Creating empty file at path ({path}) & returning empty string");
			CreateEmptyFile(path);
			return "";
		}
		catch (IOException)
		{
			Console.WriteLine("MT4Helpers.TryReadFile(): IOException. Race condition. Most likely this process and the MetaTrader EA both trying to access/use the file simultaneously. Returning empty string");
			return "";
		}
	}

	public static void CreateEmptyFile(string filepath)
	{
		File.Create(filepath).Dispose();
	}

    private static string GetFileNameFromPath(string path)
    {
        try
        {
            return path.Split("\\").Last();
        }
        catch (Exception)
        {
            try
            {
                return path.Split("/").Last();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}