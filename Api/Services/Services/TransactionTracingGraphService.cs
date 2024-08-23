using Api.CustomException;
using Api.Data;
using Api.Models;
using Api.Services.Interfaces;
using Gremlin.Net.Process.Traversal;

public class TransactionTracingGraphService : ITransactionTracingGraphService
{
    private IGraphService _graphService;
    private readonly ApplicationDbContext _context;
    private JanusGraphConnector? _connector;
    public TransactionTracingGraphService(IGraphService graphService, ApplicationDbContext context)
    {
        _graphService = graphService;
        _context = context;
    }
    public TransactionGraphDetails GetTransaction(int ObservatoryId, string TransactionId)
    {
        var connected = connect(ObservatoryId);
        if (!connected || _connector == null)
        {
            throw new ValidateErrorException("Unable to Connect to Graph Transaction");
        }

        var g = _connector.traversal();
        var transactionNode = g.V().HasLabel(JanusService.TransactionNode)
            .Has("ObservatoryId", ObservatoryId).Has("TransactionId", TransactionId).Id();
        if (!transactionNode.HasNext())
        {
            throw new ValidateErrorException("This Transaction Does not exist, Kindly try again");
        }

        var NodeId = transactionNode.Next();
        var NodeDetails = g.V(NodeId).ValueMap<dynamic, dynamic>().Next();
        var edges = g.V(NodeId).BothE().ToList();
        var response = new TransactionGraphDetails
        {
            Edges = edges,
            Node = NodeDetails
        };
        return response;
    }
    public TransactionGraphDetails GetNode(int ObservatoryId, int NodeId)
    {
        var connected = connect(ObservatoryId);
        if (!connected || _connector == null)
        {
            throw new ValidateErrorException("Unable to Connect to Graph Transaction");
        }

        var g = _connector.traversal();
        var NodeDetails = g.V(NodeId).ValueMap<dynamic, dynamic>().Next();
        var edges = g.V(NodeId).BothE().ToList();
        var response = new TransactionGraphDetails
        {
            Edges = edges,
            Node = NodeDetails
        };
        return response;
    }
    public List<TransactionGraphEdgeDetails> Trace(DateTime Date, string AccountNumber, string BankCode, string CountryCode)
    {
        var Bank = _context.Banks.Where(b => b.Code == BankCode && b.Country == CountryCode).First();
        var ValidObservatories = _context.Observatories
            .Where(o => o.ObservatoryType == ObservatoryType.Swtich || (o.ObservatoryType == ObservatoryType.Bank && o.BankId == Bank.Id))
            .ToList();
        var data = new List<TransactionGraphEdgeDetails>();
        foreach (var Observatory in ValidObservatories)
        {
            var connected = connect(Observatory.Id);
            if (!connected || _connector == null)
            {
                throw new ValidateErrorException("Unable to Connect to Graph Transaction");
            }

            var g = _connector.traversal();
            var transactionNodes = g.V().HasLabel(JanusService.TransactionNode)
                .Has("ObservatoryId", Observatory.Id)
                .Has("TransactionDate", P.Gte(Date))
                .Where(__.InE("SENT").OutV().Has("AccountNumber", AccountNumber)
                .Has("BankCode", BankCode).Has("Country", CountryCode))
                .ToList();

            foreach (var vertex in transactionNodes)
            {
                var NodeDetails = g.V(vertex?.Id).ValueMap<dynamic, dynamic>().Next();
                var Edges = g.V(vertex?.Id).BothE().As("E").BothV().As("V").Select<object>("E","V").ToList();
                var response = new TransactionGraphEdgeDetails
                {
                    Edges = Edges,
                    Node = NodeDetails
                };
                data.Add(response);
            }

        }

        return data;
    }



    private bool connect(int ObservatoryId)
    {
        try
        {
            _connector = _graphService.connect(ObservatoryId);
            return true;
        }
        catch (Exception exception)
        {
            throw new ValidateErrorException("Could not connect tor Graph, Please check connection or contact Admin " + exception.Message);
        }
    }

    private bool connect()
    {
        try
        {
            _connector = _graphService.connect();
            return true;
        }
        catch (Exception exception)
        {
            throw new ValidateErrorException("Could not connect tor Graph, Please check connection or contact Admin " + exception.Message);
        }
    }

    public TransactionGraphDetails NodeDetails(long NodeId)
    {
        var connected = connect();
        if (!connected || _connector == null)
        {
            throw new ValidateErrorException("Unable to Connect to Graph Transaction");
        }

        var g = _connector.traversal();

        var NodeDetails = g.V(NodeId).ValueMap<dynamic, dynamic>().Next();
        var edges = g.V(NodeId).BothE().ToList();
        var response = new TransactionGraphDetails
        {
            Edges = edges,
            Node = NodeDetails
        };
        return response;
    }

    public List<TransactionGraphDetails> GetTransactions(int ObservatoryId, DateTime TransactionDate, int pageNumber, int batch)
    {
        var connected = connect();
        if (!connected || _connector == null)
        {
            throw new ValidateErrorException("Unable to Connect to Graph Transaction");
        }
        var g = _connector.traversal();
        int from = pageNumber * batch;
        var Transactions = g.V().HasLabel(JanusService.TransactionNode)
        .Has("ObservatoryId", ObservatoryId).Has("TransactionDate", P.Gte(TransactionDate))
        .Range<dynamic>(from, batch).ToList();
        var data = new List<TransactionGraphDetails>();

        foreach (var vertex in Transactions)
        {
            var NodeDetails = g.V(vertex?.Id).ValueMap<dynamic, dynamic>().Next();
            var response = new TransactionGraphDetails
            {
                Edges = null,
                Node = NodeDetails
            };
            data.Add(response);
        }
        return data;
    }

    public long GetTransactionCount(int ObservatoryId, DateTime TransactionDate)
    {
        var connected = connect();
        if (!connected || _connector == null)
        {
            throw new ValidateErrorException("Unable to Connect to Graph Transaction");
        }
        var g = _connector.traversal();
        var totalTransactions = g.V().HasLabel(JanusService.TransactionNode).Has("ObservatoryId", ObservatoryId).Has("TransactionDate", P.Gte(TransactionDate)).Count().Next();
        return totalTransactions;
    }
}