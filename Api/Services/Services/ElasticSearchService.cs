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