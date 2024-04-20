namespace TradingAPIs.MetaTrader.MTXConnect;

public class MqlParam : Tuple<string, Type>
{
    private readonly IMetaTraderBridgeClient? _client;

    public string Name => Item1;
    public Type Type => Item2;
    public dynamic Value 
    { 
        get 
        {
            if (_client == null)
                throw new NullReferenceException("Client is null. Cannot get value from MetaTrader.");

            Console.WriteLine($"Getting current {Name} {Type} value from MetaTrader...");

            return true; 
        } 
        set { throw new NotImplementedException(); } 
    }

    public MqlParam(string paramMqlName, Type paramType, IMetaTraderBridgeClient? client = null) : base(item1: paramMqlName, item2: paramType)
    {
        _client = client;
    }
}