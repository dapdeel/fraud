using System.ComponentModel.DataAnnotations;
using Api.CustomException;
using Api.Data;
using Api.Services.Interfaces;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
using JanusGraph.Net.IO.GraphSON;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static Gremlin.Net.Process.Traversal.AnonymousTraversalSource;

public class JanusService : IGraphService
{
    public static readonly string CustomerNode = "Customer";
    public static readonly string AccountNode = "Account";
    public static readonly string TransactionNode = "Transaction";

    public static readonly string DeviceNode = "Device";

    private GraphConfig _graphConfig;
    private ApplicationDbContext _context;
    public JanusService(IOptions<GraphConfig> configuration, ApplicationDbContext context)
    {
        _graphConfig = configuration.Value;
        _context = context;
    }
    public JanusGraphConnector connect()
    {
        string[] Hosts = ["1111","22222"];
        
        var Connector = new JanusGraphConnector(_graphConfig.Host);
        return Connector;
    }
    public JanusGraphConnector connect(int ObservatoryId)
    {
        var observatory = _context.Observatories.Find(ObservatoryId);
        if (observatory == null)
        {
            throw new ValidateErrorException("No Observatory Found, Please contact your support officer");
        }
        var host = _graphConfig.Host;
        if (!observatory.UseDefault)
        {
            host = observatory.GraphHost;
        }
        var Connector = new JanusGraphConnector(host);
        return Connector;
    }

}
