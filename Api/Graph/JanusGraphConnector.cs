using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
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
        _g = Traversal().WithRemote(new DriverRemoteConnection(_client));
    }
    public Gremlin.Net.Process.Traversal.GraphTraversalSource traversal()
    {
        if (_g != null)
        {
            return _g;
        }
        throw new Exception("This traversal has not been initialized");
    }
    public GremlinClient Client()
    {
        return _client;
    }
}