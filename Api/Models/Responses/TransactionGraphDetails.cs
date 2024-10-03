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


public class TransactionTraceResult
{
    public required IList<IDictionary<string, object>> Edges { get; set; } = new List<IDictionary<string, object>>();
    public TransactionNode? Node { get; set; } 
}



public class TransactionNode
{
    public string PlatformId { get; set; }
    public float Amount { get; set; }
    public string? Currency { get; set; }
    public string? Description { get; set; }
    public string TransactionType { get; set; }
    public DateTime TransactionDate { get; set; }
    public string TransactionId { get; set; }
    public string DebitAccountId { get; set; }
    public string CreditAccountId { get; set; }
    public string? DeviceDocumentId { get; set; }
    public int ObservatoryId { get; set; }
    public string ObservatoryTag { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Document { get; set; }
}
