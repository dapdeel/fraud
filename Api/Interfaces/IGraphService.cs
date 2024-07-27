using Gremlin.Net.Process.Traversal;

namespace Api.Services.Interfaces;
public interface IGraphService
{
   public abstract GraphTraversalSource connect();
}