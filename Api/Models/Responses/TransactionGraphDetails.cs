using Gremlin.Net.Structure;
using Nest;

public class TransactionGraphDetails
{
    public required IList<Edge> Edges { get; set; }
    public IDictionary<dynamic, dynamic>? Node { get; set; }

}

public class TransactionGraphEdgeDetails
{
    public required IList<IDictionary<string,object>> Edges { get; set; }
    public IDictionary<dynamic, dynamic>? Node { get; set; }

}
