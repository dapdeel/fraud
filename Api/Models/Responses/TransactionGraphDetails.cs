using Gremlin.Net.Structure;
using Nest;

public class TransactionGraphDetails
{
    public required IList<Edge> Edges { get; set; }
    public IDictionary<dynamic, dynamic>? Node { get; set; }

}
