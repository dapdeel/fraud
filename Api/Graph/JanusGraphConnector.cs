using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
using Gremlin.Net.Process.Traversal;
using JanusGraph.Net.IO.GraphSON;
using static Gremlin.Net.Process.Traversal.AnonymousTraversalSource;

public class JanusGraphConnector
{
    private Gremlin.Net.Process.Traversal.GraphTraversalSource _g;
    private GremlinClient _client;


    public JanusGraphConnector(string host)
    {
        var serializer = new JanusGraphGraphSONMessageSerializer();
        _client = new GremlinClient(new GremlinServer(host, 8182), serializer);

    }
    public GraphTraversalSource traversal()
    {
        if (_g == null)
        {
            var g = Traversal().WithRemote(new DriverRemoteConnection(_client));
            _g = g;
        }
        return _g;
    }
    public async Task<bool> RunIndexQuery()
    {
        var createCustomerPropertyKeyQuery = "graph.createKeyIndex('CustomerId',Vertex.class, 'CustomerId')";
        var createAccountPropertyKeyQuery = "graph.createKeyIndex('AccountId',Vertex.class, 'AccountId')";
        var createTransactionPlatformIdPropertyKeyQuery = "graph.createKeyIndex('PlatformId',Vertex.class, 'PlatformId')";
        var createTransactionIdPropertyKeyQuery = "graph.createKeyIndex('TransactionId',Vertex.class, 'TransactionId')";
        await _client.SubmitAsync(createCustomerPropertyKeyQuery);
        await _client.SubmitAsync(createAccountPropertyKeyQuery);
        await _client.SubmitAsync(createTransactionIdPropertyKeyQuery);
        await _client.SubmitAsync(createTransactionPlatformIdPropertyKeyQuery);
        return true;
    }

    public GremlinClient Client()
    {
        return _client;
    }
}