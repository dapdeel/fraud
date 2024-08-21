using System.ComponentModel.DataAnnotations;
using Api.CustomException;
using Api.Data;
using Api.Models;
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
    private Observatory? _observatory;
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
        string[] Hosts = _graphConfig.Host.Split(",");
        Random random = new Random();

        // Get a random index from the array
        int randomIndex = random.Next(Hosts.Length);

        var Connector = new JanusGraphConnector(Hosts[randomIndex]);
        return Connector;
    }
    public string GetHost(Observatory observatory)
    {
        var host = _graphConfig.Host;
        if (!observatory.UseDefault)
        {
            host = observatory.GraphHost;
        }
        if (string.IsNullOrEmpty(host))
        {
            throw new ValidateErrorException("Invalid Host");
        }
        string[] Hosts = host.Split(",");
        Random random = new Random();

        // Get a random index from the array
        int randomIndex = random.Next(Hosts.Length);
        return Hosts[randomIndex];
    }
    public JanusGraphConnector connect(int ObservatoryId)
    {
        _observatory = _context.Observatories.Find(ObservatoryId);
        if (_observatory == null)
        {
            throw new ValidateErrorException("No Observatory Found, Please contact your support officer");
        }

        var Connector = new JanusGraphConnector(GetHost(_observatory));

        return Connector;
    }


}
