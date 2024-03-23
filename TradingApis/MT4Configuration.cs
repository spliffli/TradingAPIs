using System.Globalization;
using Salaros.Configuration;

namespace TradingApis;

public class MT4Configuration : ISessionConfiguration
{
    // The name of the configuration
    public string Name { get; set; }
    // The directory path of the MetaTrader instance
    public string MetaTraderDirPath { get; }
    public bool SubscribeToTickData { get; set; }
    public bool SubscribeToBarData { get; set; }
    public string[] SymbolsTickData { get; set; }
    public string[,] SymbolsBarData { get; set; }


    // Constructor for the MT4Configuration class that initializes configuration using a specified file path.
    public MT4Configuration(string configFilePath)
    {
        // Initialize a ConfigParser object with custom settings to parse the configuration file.
        var configFileFromPath = new ConfigParser(configFilePath, new ConfigParserSettings
        {
            MultiLineValues = MultiLineValues.Simple | MultiLineValues.AllowValuelessKeys | MultiLineValues.QuoteDelimitedValues,
            // Optionally, set a specific culture for parsing values, e.g., "en-US".
            // Culture = new CultureInfo("en-US")
        });

        // Check if the configFileFromPath is null which means the configuration file couldn't be loaded.
        if (configFileFromPath == null)
            throw new Exception("The configuration file is null");
        // Check if the configuration file is empty by looking at the count of lines in the file.
        else if (configFileFromPath.Lines.Count == 0)
            throw new Exception("The configuration file is empty");

        // Extract section names from the configFileSections into a list for easier processing.
        var configFileSections = configFileFromPath.Sections.Select(section => section.Content).ToList();
        // Define a list of required configuration sections that the file must contain.
        var requiredConfigSections = new List<string> { "[MetaData]", "[DataSubscriptions]", "[Symbols]", "[Symbols.BarData]" };

        // Check if any required sections are missing from the configuration file and throw an exception if so.
        CheckIfSectionsMissing(requiredConfigSections, configFileSections);

        // Retrieve specific values from the configuration file, such as instance name and directory path.
        Name = configFileFromPath.GetValue("MetaData", "instanceName");
        MetaTraderDirPath = configFileFromPath.GetValue("MetaData", "metaTraderDirPath");

        // Retrieve boolean subscription settings
        SubscribeToTickData = configFileFromPath.GetValue("DataSubscriptions", "subscribeToTickData", false);
        SubscribeToBarData = configFileFromPath.GetValue("DataSubscriptions", "subscribeToBarData", false);

        // Parse symbols for tick and bar data from the configuration file.
        SymbolsTickData = configFileFromPath.GetArrayValue("Symbols", "tickDataSymbols");
        // var symbolsTickData =configFileFromPath.JoinMultilineValue("Symbols", "symbolsTickData", ",");
        SymbolsBarData = ParseSymbolsBarData(configFileFromPath);
    }


    private static int CheckIfSectionsMissing(List<string> requiredConfigSections, List<string> configFileSections)
    {
        var missingSections = new List<string>();

        foreach (string requiredSection in requiredConfigSections)
        {
            if (!configFileSections.Contains(requiredSection))
            {
                missingSections.Add(requiredSection);
            }
        }

        if (missingSections.Any())
        {
            throw new InvalidOperationException($"The configuration file is missing the following sections: {string.Join(", ", missingSections)}");
        }

        return 0;
    }

    private string[,]? ParseSymbolsBarData(ConfigParser configFileFromPath)
    {
        var symbolsList = new List<string[]> { };

        var timeframeKeyNames = new string[] { "M1", "M5", "M15", "M30", "H1", "H4", "D1", "W1", "MN1" };

        foreach (var timeframeKeyName in timeframeKeyNames)
        {
            var symbolsInKeyArrayValue = configFileFromPath.GetArrayValue("Symbols.BarData", timeframeKeyName);

            if (symbolsInKeyArrayValue != null)
            {
                foreach (var symbol in symbolsInKeyArrayValue)
                {
                    symbolsList.Add([symbol, timeframeKeyName]);
                }
            }
            // else if (symbolsInKeyArrayValue == null)
            // {
            //     throw new Exception($"The configuration file is missing the following key: {timeframeKeyName}");
            // }
        }

        return CreateRectangularArray(symbolsList);
    }

    private static T[,] CreateRectangularArray<T>(IList<T[]> arrays)
    {
        // TODO: Validation and special-casing for arrays.Count == 0
        int minorLength = arrays[0].Length;
        T[,] ret = new T[arrays.Count, minorLength];
        for (int i = 0; i < arrays.Count; i++)
        {
            var array = arrays[i];
            if (array.Length != minorLength)
            {
                throw new ArgumentException
                    ("All arrays must be the same length");
            }

            for (int j = 0; j < minorLength; j++)
            {
                ret[i, j] = array[j];
            }
        }

        return ret;
    }

    public MT4Configuration(string metaTraderDirPath, string name, bool subscribeToTickData = true, bool subscribeToBarData = false, string[] symbolsTickData = null, string[,] symbolsBarData = null)
    {
        Name = name;
        MetaTraderDirPath = metaTraderDirPath;
        SubscribeToTickData = subscribeToTickData;
        SubscribeToBarData = subscribeToBarData;
        SymbolsTickData = symbolsTickData;
        SymbolsBarData = symbolsBarData;
        // Initialize other properties here
    }
}