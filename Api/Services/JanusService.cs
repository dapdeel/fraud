using Api.Services.Interfaces;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
using JanusGraph.Net.IO.GraphSON;
using Microsoft.Extensions.Options;
using static Gremlin.Net.Process.Traversal.AnonymousTraversalSource;

public class JanusService : IGraphService
{
    public static readonly string CustomerNode = "Customer";
    public static readonly string AccountNode = "Account";
    public static readonly string TransactionNode = "Transaction";

    public static readonly string DeviceNode = "Device";

    private GraphConfig _graphConfig;
    public JanusService(IOptions<GraphConfig> configuration)
    {
        _graphConfig = configuration.Value;
    }
    public Gremlin.Net.Process.Traversal.GraphTraversalSource connect()
    {
        var serializer = new JanusGraphGraphSONMessageSerializer();
        var Client = new GremlinClient(new GremlinServer(_graphConfig.Host, 8182), serializer);
        var g = Traversal().WithRemote(new DriverRemoteConnection(Client));
        return g;
    }

}
