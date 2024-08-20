using Gremlin.Net.Driver;
using Gremlin.Net.Process.Traversal;
using Nest;

namespace Api.Services.Interfaces;
public interface IElasticSearchService
{
   public abstract ElasticClient connect();
   public abstract ElasticClient connect(string Host);
}