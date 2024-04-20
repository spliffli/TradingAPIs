using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingAPIs.Common.Orders;

public abstract class Order
{
    public string Symbol { get; set; }
    public OrderType Type { get; set; }
    public double Volume { get; set; }
    public double Price { get; set; }
    public double StopLoss { get; set; }
    public double TakeProfit { get; set; }
    public DateTime Expiration { get; set; }
    public string Comment { get; set; }
    public int MagicNumber { get; set; }
    public int Slippage { get; set; }
    public DateTime OpenTime { get; set; }
    public DateTime CloseTime { get; set; }
    public double Commission { get; set; }
    public double Swap { get; set; }
    public double Profit { get; set; }
    public double Balance { get; set; }
    public double Equity { get; set; }
    public double Margin { get; set; }
    public double FreeMargin { get; set; }
    public double MarginLevel { get; set; }
    public double Leverage { get; set; }
    public double AccountBalance { get; set; }
    public double AccountEquity { get; set; }
    public double AccountMargin { get; set; }
    public double AccountFreeMargin { get; set; }
    public double AccountMarginLevel { get; set; }
    public double AccountLeverage { get; set; }
    public double AccountProfit { get; set; }
    public double AccountCommission { get; set; }
    public double AccountSwap { get; set; }
    public double AccountCredit { get; set; }
    public double AccountBalanceChange { get; set; }
    public double AccountEquityChange { get; set; }
    public double AccountMarginChange { get; set; }
    public double AccountFreeMarginChange { get; set; }
    public double AccountMarginLevelChange { get; set; }
    public double AccountLeverageChange { get; set; }
    public double AccountProfitChange { get; set; }
    public double AccountCommissionChange { get; set; }
    public double AccountSwapChange { get; set; }
    public double AccountCreditChange { get; set; }
    public double AccountBalanceChangePercent { get; set; }
    public double AccountEquityChangePercent { get; set; }
    public double AccountMarginChangePercent { get;}

    public static string GenerateNewId()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString("X");
    }
}

