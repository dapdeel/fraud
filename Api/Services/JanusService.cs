using Api.Services.Interfaces;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
using static Gremlin.Net.Process.Traversal.AnonymousTraversalSource;

public class JanusService : IGraphService
{

    private GraphConfig _graphConfig;
    private readonly IConfiguration _configuration;
    public JanusService(IConfiguration configuration)
    {
        _configuration = configuration;
        _graphConfig = _configuration.GetValue<GraphConfig>("Graph");
    }
    public Gremlin.Net.Process.Traversal.GraphTraversalSource connect()
    {
        try{
        var Client = new GremlinClient(new GremlinServer("52.19.2.233", 8182));
        var g = Traversal().WithRemote(new DriverRemoteConnection(Client));
        return g;
        }catch(Exception excep){
            return null;
        }
    }
}