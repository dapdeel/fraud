using Api.Services.Interfaces;
using Nest;

public class ElasticSearchService : IElasticSearchService
{
    private IConfiguration _configuration;
    private string? DefaultHost;

    public ElasticSearchService(IConfiguration configuration)
    {
        _configuration = configuration;
        DefaultHost = _configuration.GetValue<string>("ElasticSearchHost");
    }
    public ElasticClient connect()
    {
        var settings = new ConnectionSettings(new Uri(DefaultHost))
             .DefaultIndex("transactions");
        var client = new ElasticClient(settings);
        return client;
    }

    public ElasticClient connect(string Host)
    {
        var settings = new ConnectionSettings(new Uri(Host))
            .DisableDirectStreaming(false)
             .DefaultIndex("transactions");
        var client = new ElasticClient(settings);
        return client;
    }


}

public static class NodeData
{
    public const string Transaction = "Transaction";
    public const string Account = "Account";
    public const string Device = "Device";
    public const string Customer = "Customer";
}

public static class EdgeData
{
    public const string Owns = "Owns";
    public const string Transfered = "Transfered";
    public const string Used = "Used";
    public const string Sent = "Sent";
    public const string Received = "Received";
    public const string UsedOn = "UsedOn";
    public const string ExecutedOn = "ExecutedOn";
}

public static class DocumentType {
     public const string Node = "Node";
    public const string Edge = "Edge";
}