using Gremlin.Net.Process.Traversal;

namespace Api.Services.Interfaces;
public interface IGraphService
{
   public abstract Gremlin.Net.Process.Traversal.GraphTraversalSource connect();
}