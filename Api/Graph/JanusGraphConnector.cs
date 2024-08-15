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

    public GremlinClient Client()
    {
        return _client;
    }
}