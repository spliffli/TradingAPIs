using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TradingAPIs;
using static TradingAPIs.MetaTrader.MTXConnect.MTXHelpers;
using System.IO;
using TradingAPIs.MetaTrader.MTXConnect;

/*

Tests to check that the MetaTraderClient is working correctly.  This file is a modified version of the original Unit Test file from the DWXConnect C# project.

Please don't run this on your live account. It will open and close positions!

The MT4/5 server must be initialized with maximumOrders>=5 and maximumLotSize>=0.02. 


compile and run tests:
dotnet build
dotnet test

Or in Visual Studio: 
Test -> Run All Tests

*/

namespace TradingAPIsUnitTests.MTXConnectUnitTests;

[TestClass]
public class MTXClientUnitTest
{
    private const string _metaTraderDirPath = "C:/Users/Administrator/AppData/Roaming/MetaQuotes/Terminal/3B534B10135CFEDF8CD1AAB8BD994B13/MQL4/Files/";
    private string _symbol = "EURUSD.r";
    private int _magicNumber = 0;
    private int _numOpenOrders = 5;
    private double _lots = 0.02;  // 0.02 so that we can also test partial closing. 
    private double _priceOffset = 0.01;
    private string[] _types = { "buy", "sell", "buylimit", "selllimit", "buystop", "sellstop" };

    private string _mt4TestConfigPath = Directory.GetCurrentDirectory() + "\\mt4TestConfig.ini";
    private MTXConfig _testConfig;
    private MTXClient _testClient;

    /*Initializes MetaTraderClient and closes all open orders. 
		*/
    [TestInitialize]
    public void TestInitialize()
    {
        if (!File.Exists(_mt4TestConfigPath))
            _mt4TestConfigPath = Directory.GetCurrentDirectory() + "\\MTXConnectUnitTests\\mt4TestConfig.ini";

        Assert.IsTrue(File.Exists(_mt4TestConfigPath), $"mt4TestConfig.ini doesn't exist in the specified path: {_mt4TestConfigPath}, nor in its parent directory.");

        _testConfig = new MTXConfig(_mt4TestConfigPath);
        _testClient = new MTXClient(_testConfig, new MTXEventHandler());  // new MetaTraderClient(null, MetaTraderDirPath, 5, 10, false, false);
        Thread.Sleep(1000);
        // make sure there are no open orders when starting the test. 

        // if (!_testClient.CheckIfMarketIsOpen())
        // Assert.Inconclusive("Market is closed for the weekend so some tests will fail even if they are working when the market's open.");

        if (!_testClient.CheckIfMetaTraderIsInstalled())
            Assert.Fail("MetaTrader is not installed.");

        if (!_testClient.CheckIfTerminalIsRunning())
            Assert.Fail("Terminal is not running.");

        if (!_testClient.CheckIfServerEaIsRunning())
            Assert.Fail("Server EA is not running.");

        if (!_testClient.CloseAllOrders())
            Assert.Fail("Could not close orders in setUp().");
    }

    [TestMethod]
    public void TestCheckIfMarketIsOpen()
    {
        bool MarketIsOpen = _testClient.CheckIfMarketIsOpen();

        Assert.IsTrue(MarketIsOpen, "Market isn't open because of the weekend so some tests will fail even if they are working when the market's open.");
    }

    [TestMethod]
    public void TestCheckIfMetaTraderIsInstalled()
    {
        bool MetaTraderIsInstalled = _testClient.CheckIfMetaTraderIsInstalled();

        Assert.IsTrue(MetaTraderIsInstalled, "Metatrader isn't installed in the path specified in the config.");
    }

    [TestMethod]
    public void TestGetTerminalProcesses()
    {
        var terminalProcesses = _testClient.GetTerminalProcesses();

        Assert.IsNotNull(terminalProcesses, "terminalProcesses can't be null. Check if MetaTrader is running.");
        Assert.IsTrue(terminalProcesses.Count() > 0, "terminalProcesses.Count() must be greater than 0. Check if MetaTrader is running.");
    }

    [TestMethod]
    public void TestGetTerminalProcess()
    {
        var terminalProcess = _testClient.GetTerminalProcess();

        Assert.IsNotNull(terminalProcess, "terminalProcess can't be null. Check if MetaTrader is running.");
    }

    [TestMethod]
    public void TestCheckIfTerminalIsRunning()
    {
        bool TerminalIsRunning = _testClient.CheckIfTerminalIsRunning();

        Assert.IsTrue(TerminalIsRunning, "MetaTrader terminal isn't running so tests will fail.");
    }

    [TestMethod]
    public void TestCheckIfServerEaIsRunning()
    {
        bool ServerEaIsRunning = _testClient.CheckIfServerEaIsRunning();

        Assert.IsTrue(ServerEaIsRunning);
    }

    [TestMethod]
    public void TestFetchMqlParams()
    {
        var mqlParams = _testClient.GetMqlParams();

        Assert.IsNotNull(mqlParams);
        // Assert.IsTrue(mqlParams.Count() > 0);
    }

    [TestMethod]
    public void TestOpenOrder()
    {
        bool orderOpened = _testClient.OpenOrder(_symbol, "buy", _lots, 0, 0, 0, _magicNumber, "", 0);

        Assert.IsTrue(orderOpened, "Order couldn't be opened.");
    }
}


/// <summary>
/// This class tests the MetaTraderClient class, which is a modified version of the original Unit Test class from the DWXConnect C# project.
/// Any new methods added to the MetaTraderClient class shouldn't be tested in this class, but in the above MTXClientUnitTest class,
/// or if you are forking this code and adding new methods to the MetaTraderClient class, you can make a new TestClass for those methods.
/// </summary>
[TestClass]
public class MetaTraderClientDWXUnitTest
{
    private string _metaTraderDirPath = "C:/Users/Administrator/AppData/Roaming/MetaQuotes/Terminal/3B534B10135CFEDF8CD1AAB8BD994B13/MQL4/Files/";
    private string _symbol = "EURUSD.r";
    private int _magicNumber = 0;
    private int _numOpenOrders = 5;
    private double _lots = 0.02;  // 0.02 so that we can also test partial closing. 
    private double _priceOffset = 0.01;
    private string[] _types = { "buy", "sell", "buylimit", "selllimit", "buystop", "sellstop" };

    private string _mt4TestConfigPath = Directory.GetCurrentDirectory() + "\\mt4TestConfig.ini";
    private MTXConfig _testConfig;
    private MTXClient _testClient;



    /*Initializes DWX_Client and closes all open orders. 
		*/
    [TestInitialize]
    public void TestInitialize()
    {
        if (!File.Exists(_mt4TestConfigPath))
            _mt4TestConfigPath = Directory.GetCurrentDirectory() + "\\MTXConnectUnitTests\\mt4TestConfig.ini";

        Assert.IsTrue(File.Exists(_mt4TestConfigPath), $"mt4TestConfig.ini doesn't exist in the specified path: {_mt4TestConfigPath}");

        _testConfig = new MTXConfig(_mt4TestConfigPath);
        _testClient = new MTXClient(_testConfig, new MTXEventHandler());  // new MetaTraderClient(null, MetaTraderDirPath, 5, 10, false, false);
        Thread.Sleep(1000);

        if (!_testClient.CheckIfMetaTraderIsInstalled())
            Assert.Inconclusive("MetaTrader is not installed.");

        if (!_testClient.CheckIfTerminalIsRunning())
            Assert.Inconclusive("Terminal is not running.");

        if (!_testClient.CheckIfServerEaIsRunning())
            Assert.Inconclusive("Server EA is not running.");

        // make sure there are no open orders when starting the test. 
        if (!_testClient.CloseAllOrders())
            Assert.Fail("Could not close orders in setUp().");

    }


    /*Opens multiple orders. 

		As long as not enough orders are open, it will send new 
		open_order() commands. This is needed because of possible 
		requotes or other errors during opening of an order.
		*/
    bool openMultipleOrders()
    {
        for (int i = 0; i < _numOpenOrders; i++)
        {
            // _testClient.OpenOrder(_symbol, OrderType.Buy, _lots, 0, 0, 0, _magicNumber, "", 0);
            _testClient.OpenOrder(_symbol, "buy", _lots, 0, 0, 0, _magicNumber, "", 0);
        }
        DateTime now = DateTime.UtcNow;
        DateTime endTime = DateTime.UtcNow + new TimeSpan(0, 0, 5);
        while (now < endTime)
        {
            Thread.Sleep(1000);
            now = DateTime.UtcNow;
            if (_testClient.OpenOrders.Count == _numOpenOrders)
                return true;
            // in case there was a requote, try again:
            // _testClient.OpenOrder(_symbol, OrderType.Buy, _lots, 0, 0, 0, _magicNumber, "", 0);
            _testClient.OpenOrder(_symbol, "buy", _lots, 0, 0, 0, _magicNumber, "", 0);
        }
        return false;
    }


    /*Closes all open orders. 

		As long as there are open orers, it will send new 
		close_all_orders() commands. This is needed because of 
		possible requotes or other errors during closing of an 
		order. 
		*/
    public bool TryCloseAllOrders()
    {
        DateTime now = DateTime.UtcNow;
        DateTime endTime = DateTime.UtcNow + new TimeSpan(0, 0, 10);
        while (now < endTime)
        {
            // sometimes it could fail if for example there is a requote. so just try again. 
            _testClient.CloseAllOrders();
            Thread.Sleep(1000);
            now = DateTime.UtcNow;
            if (_testClient.OpenOrders.Count == 0)
                return true;
        }
        return false;
    }


    /*Subscribes to the test symbol. 
		*/
    [TestMethod]
    public void TestSubcribeSymbolsMarketData()
    {
        if (!_testClient.CheckIfMarketIsOpen())
            Assert.Inconclusive("Market is closed for the weekend so can't subscribe to live market data. Try again when it's open.");

        string[] symbols = new string[1];
        symbols[0] = _symbol;
        _testClient.SubscribeSymbolsMarketData(symbols);

        double bid = -1;
        DateTime now = DateTime.UtcNow;
        DateTime endTime = DateTime.UtcNow + new TimeSpan(0, 0, 5);
        while (now < endTime)
        {
            now = DateTime.UtcNow;
            try
            {
                bid = (double)_testClient.MarketData[_symbol]["bid"];
                break;
            }
            catch
            {
            }
            Thread.Sleep(100);
        }
        Assert.IsTrue(bid > 0);
    }


    /*Checks if there are open orders for each order type. 
		*/
    public bool allTypesOpen()
    {
        List<string> typesOpen = new List<string>();
        foreach (var x in _testClient.OpenOrders)
        {
            typesOpen.Add((string)_testClient.OpenOrders[x.Key]["type"]);
        }
        return typesOpen.ToArray().All(value => _types.Contains(value));
    }


    /*Tries to open an order for each type that is not already open. 
		*/
    public void openMissingTypes()
    {

        double bid = (double)_testClient.MarketData[_symbol]["bid"];
        double[] prices = { 0, 0, bid - _priceOffset, bid + _priceOffset, bid + _priceOffset, bid - _priceOffset };

        List<string> typesOpen = new List<string>();
        foreach (var x in _testClient.OpenOrders)
        {
            typesOpen.Add((string)_testClient.OpenOrders[x.Key]["type"]);
        }

        for (int i = 0; i < _types.Length; i++)
        {
            if (typesOpen.Contains(_types[i]))
                continue;
            _testClient.OpenOrder(_symbol, _types[i], _lots, prices[i], 0, 0, _magicNumber, "", 0);
        }
    }


    /*Opens at least one order for each possible order type.

		It calls openMissingTypes() until at least one order is open 
		for each possible order type.
		*/
    [TestMethod]
    public bool OpenTestOrders()
    {
        if (!_testClient.CheckIfMarketIsOpen())
            Assert.Inconclusive("Market is closed for the weekend so can't open test orders. Try again when it's open.");

        bool ato = false;
        DateTime now = DateTime.UtcNow;
        DateTime endTime = DateTime.UtcNow + new TimeSpan(0, 0, 5);
        while (now < endTime)
        {
            openMissingTypes();
            Thread.Sleep(1000);
            now = DateTime.UtcNow;
            ato = allTypesOpen();
            if (ato)
                break;
        }

        Assert.IsTrue(ato);
        return ato;
    }


    /*Modifies all open orders. 
		
		It will try to set the SL and TP for all open orders. 
		*/
    [TestMethod]
    public bool ModifyTestOrders()
    {
        if (_testClient.OpenOrders.Count == 0)
            Assert.Fail("There are no test orders to modify in ModifyTestOrders().");

        foreach (var x in _testClient.OpenOrders)
        {
            JObject jo = (JObject)_testClient.OpenOrders[x.Key];
            string type = (string)jo["type"];
            double openPrice = (double)jo["open_price"];
            double sl = openPrice - _priceOffset;
            double tp = openPrice + _priceOffset;
            if (type.Contains("sell"))
            {
                sl = openPrice + _priceOffset;
                tp = openPrice - _priceOffset;
            }
            _testClient.ModifyOrder(int.Parse(x.Key), _lots, 0, sl, tp, 0);
        }
        bool allSet = false;
        DateTime now = DateTime.UtcNow;
        DateTime endTime = DateTime.UtcNow + new TimeSpan(0, 0, 5);
        while (now < endTime)
        {
            now = DateTime.UtcNow;
            allSet = true;
            foreach (var x in _testClient.OpenOrders)
            {
                JObject jo = (JObject)_testClient.OpenOrders[x.Key];
                double sl = (double)jo["SL"];
                double tp = (double)jo["TP"];
                if (sl <= 0 || tp <= 0)
                    allSet = false;
            }
            if (allSet)
                break;
            Thread.Sleep(100);
        }
        Assert.IsTrue(allSet);
        return allSet;
    }


    /*Tries to close an one order. 

		This could fail if the closing of an orders takes too long and 
		then two orders might be closed. 
		*/
    [TestMethod]
    public void TestCloseOrder()
    {
        if (!_testClient.CheckIfMarketIsOpen())
            Assert.Inconclusive("Market is closed for the weekend so can't close orders. Try again when it's open.");

        if (_testClient.OpenOrders.Count == 0)
            Assert.Fail("There are no orders to close in closeOrder().");

        int ticket = -1;
        foreach (var x in _testClient.OpenOrders)
        {
            ticket = int.Parse(x.Key);
            break;
        }

        int numOrdersBefore = _testClient.OpenOrders.Count;

        DateTime now = DateTime.UtcNow;
        DateTime endTime = DateTime.UtcNow + new TimeSpan(0, 0, 5);
        while (now < endTime)
        {
            _testClient.CloseOrder(ticket, 0);
            Thread.Sleep(1000);
            now = DateTime.UtcNow;
            if (_testClient.OpenOrders.Count == numOrdersBefore - 1)
                break;
        }
        Assert.AreEqual(_testClient.OpenOrders.Count, numOrdersBefore - 1);
    }


    /*Tries to partially close an order. 
		*/
    [TestMethod]
    public void TestCloseOrderPartial()
    {
        if (!_testClient.CheckIfMarketIsOpen())
            Assert.Inconclusive("Market is closed for the weekend so can't close orders. Try again when it's open.");

        double closeLots = 0.01;

        if (_testClient.OpenOrders.Count == 0)
            Assert.Fail("There are no order to close in closeOrderPartial().");

        int ticket = -1;
        double lotsBefore = -1;
        foreach (var x in _testClient.OpenOrders)
        {
            string type = (string)_testClient.OpenOrders[x.Key]["type"];
            if (type.Equals("buy"))
            {
                ticket = int.Parse(x.Key);
                lotsBefore = (double)_testClient.OpenOrders[x.Key]["lots"];
                break;
            }
        }

        Assert.IsTrue(ticket >= 0);
        Assert.IsTrue(lotsBefore > 0);

        double lots = -1;

        DateTime now = DateTime.UtcNow;
        DateTime endTime = DateTime.UtcNow + new TimeSpan(0, 0, 5);
        while (now < endTime)
        {
            _testClient.CloseOrder(ticket, closeLots);
            Thread.Sleep(2000);
            now = DateTime.UtcNow;
            // need to loop because the ticket will change after modification. 
            bool found = false;
            foreach (var x in _testClient.OpenOrders)
            {
                lots = (double)_testClient.OpenOrders[x.Key]["lots"];
                if (Math.Abs(lotsBefore - closeLots - lots) < 0.001)
                {
                    found = true;
                    break;
                }
            }
            if (found)
                break;
        }
        Assert.IsTrue(lots > 0);
        Assert.IsTrue(Math.Abs(lotsBefore - closeLots - lots) < 0.001);
    }


    /*Tests subscribing to a symbol, opening, modifying, closing 
		and partial closing of orders. 

		Combined to one test function because these tests have to be 
		executed in the correct order. 
		*/
    [TestMethod]
    public void TestOpenModifyCloseOrder()
    {
        if (!_testClient.CheckIfMarketIsOpen())
            Assert.Inconclusive("Market is closed for the weekend so can't open, modify & close an order. Try again when it's open.");

        if (!_testClient.CheckIfMarketIsOpen())
            Assert.Inconclusive("Market is closed for the weekend so can't open test orders. Try again when it's open.");

        if (!_testClient.CloseAllOrders())
            Assert.Fail("Could not close orders in testOpenModifyCloseOrder().");

        TestSubcribeSymbolsMarketData();

        if (!OpenTestOrders())
            Assert.Fail("openOrders() returned false.");


        if (!ModifyTestOrders())
            Assert.Fail("modifyOrders() returned false.");

        TestCloseOrder();

        TestCloseOrderPartial();

        if (!_testClient.CloseAllOrders())
            Assert.Fail("Could not close orders after testOpenModifyCloseOrder().");
    }


    /*Tests to close all open orders. 

		First it will try to open multiple orders. 
		*/
    [TestMethod]
    public void TestCloseAllOrders()
    {
        if (!_testClient.CheckIfMarketIsOpen())
            Assert.Inconclusive("Market is closed for the weekend so can't close all orders. Try again when it's open.");

        if (!openMultipleOrders())
            Assert.Fail("Could not open all orders in testCloseAllOrders().");

        Assert.IsTrue(_testClient.CloseAllOrders());
    }


    /*Tests to close all orders with a given symbol. 
		
		First it will try to open multiple orders. 
		*/
    [TestMethod]
    public void TestCloseOrdersBySymbol()
    {
        if (!_testClient.CheckIfMarketIsOpen())
            Assert.Inconclusive("Market is closed for the weekend so can't close orders by symbol. Try again when it's open.");

        if (!openMultipleOrders())
            Assert.Fail("Could not open all orders in testCloseOrdersBySymbol().");

        _testClient.CloseOrdersBySymbol(_symbol);

        DateTime now = DateTime.UtcNow;
        DateTime endTime = DateTime.UtcNow + new TimeSpan(0, 0, 5);
        while (now < endTime)
        {
            Thread.Sleep(1000);
            now = DateTime.UtcNow;
            if (_testClient.OpenOrders.Count == 0)
                break;
            _testClient.CloseOrdersBySymbol(_symbol);
        }
        Assert.AreEqual(_testClient.OpenOrders.Count, 0);
    }


    /*Tests to close all orders with a given magic number. 

		First it will try to open multiple orders. 
		*/
    [TestMethod]
    public void TestCloseOrdersByMagic()
    {
        if (!_testClient.CheckIfMarketIsOpen())
            Assert.Inconclusive("Market is closed for the weekend so can't close orders by magic. Try again when it's open.");

        if (!openMultipleOrders())
            Assert.Fail("Could not open all orders in closeOrdersByMagic().");

        _testClient.CloseOrdersByMagic(_magicNumber);

        DateTime now = DateTime.UtcNow;
        DateTime endTime = DateTime.UtcNow + new TimeSpan(0, 0, 5);
        while (now < endTime)
        {
            Thread.Sleep(1000);
            now = DateTime.UtcNow;
            if (_testClient.OpenOrders.Count == 0)
                break;
            _testClient.CloseOrdersByMagic(_magicNumber);
        }
        Assert.AreEqual(_testClient.OpenOrders.Count, 0);
    }


    /*Tests the subscribeSymbolsBarData() function. 
	    */
    [TestMethod]
    public void TestSubscribeSymbolsBarData()
    {
        if (!_testClient.CheckIfMarketIsOpen())
            Assert.Inconclusive("Market is closed for the weekend so can't subscribe to bar data. Try again when it's open.");

        if (!_testClient.CheckIfMarketIsOpen())
            Assert.Inconclusive("Market is closed for the weekend so can't subscribe to bar data. Try again when it's open.");

        string timeFrame = "M1";

        string[,] symbols = new string[,] { { _symbol, timeFrame } };


        _testClient.SubscribeSymbolsBarData(symbols);

        JObject jo = new JObject();
        DateTime now = DateTime.UtcNow;
        DateTime endTime = DateTime.UtcNow + new TimeSpan(0, 0, 5);
        while (now < endTime)
        {
            Thread.Sleep(100);
            now = DateTime.UtcNow;
            try
            {
                jo = (JObject)_testClient.BarData[_symbol + "_" + timeFrame];
                // print(jo);
                if (jo.Count > 0)
                    break;
            }
            catch
            {
            }
        }
        Assert.IsTrue(jo.Count > 0);
    }

    /*Tests the getHistoricData() function. 
		*/
    [TestMethod]
    public void TestGetHistoricData()
    {
        if (!_testClient.CheckIfMarketIsOpen())
            Assert.Inconclusive("Market is closed for the weekend so can't get historic data. Try again when it's open.");

        string timeFrame = "D1";

        long end = DateTimeOffset.Now.ToUnixTimeSeconds();
        long start = end - 30 * 24 * 60 * 60;  // last 30 days
        _testClient.GetHistoricData(_symbol, timeFrame, start, end);

        JObject jo = new JObject();
        DateTime now = DateTime.UtcNow;
        DateTime endTime = DateTime.UtcNow + new TimeSpan(0, 0, 5);
        while (now < endTime)
        {
            Thread.Sleep(100);
            now = DateTime.UtcNow;
            try
            {
                // print(symbol + "_" + timeFrame);
                // print(dwx.HistoricData);
                jo = (JObject)_testClient.HistoricData[_symbol + "_" + timeFrame];
                // print(jo);
                if (jo.Count > 0)
                    break;
            }
            catch
            {
            }
        }
        Assert.IsTrue(jo.Count > 0);
    }
}