﻿using Salaros.Configuration;
using TradingAPIs.Common;

namespace TradingAPIs.MetaTrader.MTXConnect;

public class MTXConfig : ISessionConfiguration
{
    public string Name { get; set; }
    public string AccountId { get; set; }
    public string MetaTraderDirPath { get; }

    public int SleepDelayMilliseconds { get; } = 5;
    public int MaxRetryCommandSeconds { get; } = 10;
    public bool LoadOrdersFromFile { get; } = true;
    public bool Verbose { get; } = true;

    public bool StartMessageThread { get; }
    public bool StartOpenOrdersThread { get; }
    public bool StartMarketDataThread { get; }
    public bool StartBarDataThread { get; }
    public bool StartHistoricDataThread { get; }

    public bool SubscribeToTickData { get; }
    public bool SubscribeToBarData { get; }
    public string[] SymbolsMarketData { get; }
    public string[,] SymbolsBarData { get; }


    // Constructor for the MT4Configuration class that initializes configuration using a specified file path.
    public MTXConfig(string configFilePath)
    {
        if (!File.Exists(configFilePath))
            throw new FileNotFoundException("The MetaTrader configuration file was not found", configFilePath);

        // Initialize a ConfigParser object with custom settings to parse the configuration file.
        var configFileFromPath = new ConfigParser(configFilePath, new ConfigParserSettings
        {
            MultiLineValues = MultiLineValues.Simple | MultiLineValues.AllowValuelessKeys | MultiLineValues.QuoteDelimitedValues,
            // Optionally, set a specific culture for parsing values, e.g., "en-US".
            // Culture = new CultureInfo("en-US")
        });

        // Check if the configFileFromPath is null which means the configuration file couldn't be loaded.
        if (configFileFromPath == null)
            throw new Exception("The MetaTrader configuration file is null");
        // Check if the configuration file is empty by looking at the count of lines in the file.
        else if (configFileFromPath.Lines.Count == 0)
            throw new Exception("The MetaTrader configuration file is empty");


        // Extract section names from the configFileSections into a list for easier processing.
        var configFileSections = configFileFromPath.Sections.Select(section => section.Content).ToList();
        // Define a list of required configuration sections that the file must contain.
        var requiredConfigSections = new List<string> { "[MetaData]", "[Init]", "[Threads]", "[DataSubscriptions]", "[Symbols]", "[Symbols.BarData]" };

        // Check if any required sections are missing from the configuration file and throw an exception if so.
        var missingSections = CheckForMissingSections(requiredConfigSections, configFileSections);
        if (missingSections.Any())
            throw new InvalidOperationException($"The configuration file is missing the following sections: {string.Join(", ", missingSections)}");


        // Retrieve specific values from the configuration file, such as instance name and directory path.
        // [MetaData]
        _name = configFileFromPath.GetValue("MetaData", "clientName");
        _accountIdStr = configFileFromPath.GetValue("MetaData", "accountId");
        _metaTraderDirPath = configFileFromPath.GetValue("MetaData", "metaTraderDirPath");

        // [Init]
        _sleepDelayMilliseconds = configFileFromPath.GetValue("Init", "sleepDelayMilliseconds", 5);
        _maxRetryCommandSeconds = configFileFromPath.GetValue("Init", "maxRetryCommandSeconds", 10);
        _loadOrdersFromFile = configFileFromPath.GetValue("Init", "loadOrdersFromFile", true);
        _verbose = configFileFromPath.GetValue("Init", "verbose", true);

        // [Threads]
        _startMessageThread = configFileFromPath.GetValue("Threads", "startMessageThread", false);
        _startOpenOrdersThread = configFileFromPath.GetValue("Threads", "startOpenOrdersThread", false);
        _startMarketDataThread = configFileFromPath.GetValue("Threads", "startMarketDataThread", false);
        _startBarDataThread = configFileFromPath.GetValue("Threads", "startBarDataThread", false);
        _startHistoricDataThread = configFileFromPath.GetValue("Threads", "startHistoricDataThread", false);

        // [DataSubscriptions]
        _subscribeToTickData = configFileFromPath.GetValue("DataSubscriptions", "subscribeToTickData", false);
        _subscribeToBarData = configFileFromPath.GetValue("DataSubscriptions", "subscribeToBarData", false);

        // [Symbols]
        _symbolsMarketData = configFileFromPath.GetArrayValue("Symbols", "tickDataSymbols");
        // var SymbolsMarketData =configFileFromPath.JoinMultilineValue("Symbols", "SymbolsMarketData", ",");
        _symbolsBarData = ParseSymbolsBarData(configFileFromPath);
    }


    private static List<string> CheckForMissingSections(List<string> requiredConfigSections, List<string> configFileSections)
    {
        var missingSections = new List<string>();

        foreach (string requiredSection in requiredConfigSections)
        {
            if (!configFileSections.Contains(requiredSection))
            {
                missingSections.Add(requiredSection);
            }
        }

        // if (missingSections.Any())
        // {
        //     throw new InvalidOperationException($"The configuration file is missing the following sections: {string.Join(", ", missingSections)}");
        // }
        return missingSections;
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

    public MTXConfig(string metaTraderDirPath, string name, bool subscribeToTickData = true, bool subscribeToBarData = false, string[] SymbolsMarketData = null, string[,] symbolsBarData = null)
    {
        _name = name;
        _metaTraderDirPath = metaTraderDirPath;
        _subscribeToTickData = subscribeToTickData;
        _subscribeToBarData = subscribeToBarData;
        _symbolsMarketData = SymbolsMarketData;
        _symbolsBarData = symbolsBarData;
        // Initialize other properties here
    }
}