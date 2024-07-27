using Api.Services.Interfaces;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
using Microsoft.Extensions.Options;
using static Gremlin.Net.Process.Traversal.AnonymousTraversalSource;

public class JanusService : IGraphService
{

    private GraphConfig _graphConfig;
    private readonly IConfiguration _configuration;
    public JanusService(IOptions<GraphConfig> configuration)
    {
        _graphConfig = configuration.Value;
    }
    public Gremlin.Net.Process.Traversal.GraphTraversalSource connect()
    {

        var Client = new GremlinClient(new GremlinServer(_graphConfig.Host, 8182));
        var g = Traversal().WithRemote(new DriverRemoteConnection(Client));
        return g;

    }
}