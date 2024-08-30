using Api.CustomException;
using Api.Data;
using Api.Interfaces;
using Api.Models;
using Api.Services.Interfaces;
using Gremlin.Net.Driver;
using Gremlin.Net.Process.Traversal;
using Nest;

public class TransactionTracingGraphService : ITransactionTracingGraphService
{
    private readonly IGraphService _graphService;
    private readonly ApplicationDbContext _context;
    private JanusGraphConnector? _connector;
    private readonly IElasticSearchService _elasticSearchService;
    private readonly IAccountService _accountService;
    public TransactionTracingGraphService(IGraphService graphService,
    ApplicationDbContext context, IElasticSearchService elasticSearchService,
    IAccountService accountService
    )
    {
        _graphService = graphService;
        _context = context;
        _accountService = accountService;
        _elasticSearchService = elasticSearchService;
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

    public TransactionGraphDetails GetTransactionAsync(int ObservatoryId, string TransactionId)
    {

        var elasticClient = _elasticSearchService.connect();
        var searchResponse = elasticClient.Search<TransactionDocument>(s => s
     .Query(q => q
         .Bool(b => b
             .Filter(f => f
                 .Term(t => t.Field("indexed").Value(true))
                 && f.Term(t => t.Field("observatoryId").Value(ObservatoryId))
                 && f.Term(t => t.Field("transactionId.keyword").Value(TransactionId))
             )
         )
     )
 );

        if (!searchResponse.IsValid || searchResponse.Documents.Count == 0)
        {
            throw new ValidateErrorException("This Transaction Does not exist, Kindly try again");
        }
        var document = searchResponse.Documents.First();
        var platformId = document.PlatformId;

        var connected = connect(ObservatoryId);
        if (!connected || _connector == null)
        {
            throw new ValidateErrorException("Unable to Connect to Graph Transaction");
        }

        var g = _connector.traversal();
        var transactionNode = g.V().HasLabel(JanusService.TransactionNode)
            .Has("PlatformId", platformId)
            .Id();

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
        var query = g.V(NodeId).ValueMap<dynamic, dynamic>();
        if (!query.HasNext())
        {
            throw new ValidateErrorException("Invalid Node");
        }
        var NodeDetails = query.Next();
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
            var elasticClient = Observatory.UseDefault ?
                    _elasticSearchService.connect() :
                    _elasticSearchService.connect(Observatory.ElasticSearchHost);
            var accountDocument = _accountService.GetByAccountNumberAndBankId(AccountNumber, Bank.Id, Observatory);
            if (accountDocument == null)
            {
                throw new ValidateErrorException("Invalid Account");
            }

            var searchResponse = elasticClient.Search<TransactionDocument>(s => s
  .Query(q => q
      .Bool(b => b
          .Filter(f => f
              .Bool(bb => bb
                  .Filter(ff => ff
                      .Bool(bbb => bbb
                          .Should(sh => sh
                              .Term(t => t
                                  .Field("type.keyword")
                                  .Value("Transaction")
                              )
                          )
                          .MinimumShouldMatch(1)
                      ),
                      fff => fff
                      .Bool(bbbb => bbbb
                          .Should(sh => sh
                              .Match(m => m
                                  .Field("indexed")
                                  .Query("true")
                              )
                          )
                          .MinimumShouldMatch(1)
                      ),
                      ffff => ffff
                      .Bool(bbbbb => bbbbb
                          .Should(sh => sh
                              .Term(t => t
                                  .Field("creditAccountId.keyword")
                                  .Value(accountDocument?.AccountId)
                              )
                          )
                          .MinimumShouldMatch(1)
                      ),
                      ffff => ffff
                      .Bool(bbbbb => bbbbb
                          .Should(sh => sh
                              .DateRange(t => t
                                  .Field("transactionDate")
                                  .GreaterThanOrEquals(Date)
                              )
                          )
                          .MinimumShouldMatch(1)
                      ),
                       ffff => ffff
                      .Bool(bbbbb => bbbbb
                          .Should(sh => sh
                              .Match(t => t
                                  .Field("observatoryId")
                                  .Query(Observatory.Id.ToString())
                              )
                          )
                          .MinimumShouldMatch(1)
                      )
                  )
              )
          )
      )
  )
);

            var transactions = searchResponse.Documents;
            if (transactions.Count <= 0)
            {
                throw new ValidateErrorException("No Transactions found");
            }
            var g = _connector.traversal();
            //     var transactionNodes = g.V().HasLabel(JanusService.TransactionNode);
            //     foreach (var transaction in transactions)
            //     {
            //         if (transaction.PlatformId != null)
            //             transactionNodes = transactionNodes.Or().Has("PlatformId", transaction.PlatformId);
            //     }

            //     var result = transactionNodes.Where(__.InE("SENT").OutV().Has("AccountNumber", AccountNumber)
            //  .Has("BankCode", BankCode).Has("Country", CountryCode))
            //  .ToList();
            foreach (var transaction in transactions)
            {
                var result = g.V().HasLabel(JanusService.TransactionNode).Has("PlatformId", transaction.PlatformId).Id();
                if (!result.HasNext())
                {
                    continue;
                }
                var vertex = result.Next();
                var NodeDetails = g.V(vertex).ValueMap<dynamic, dynamic>().Next();
                var Edges = g.V(vertex).OutE("SENT").As("E").BothV().As("V").Select<object>("E", "V").ToList();
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




    /* public List<TransactionGraphEdgeDetails> Trace(DateTime date, string accountNumber, string bankCode, string countryCode)
     {
         var bank = _context.Banks.First(b => b.Code == bankCode && b.Country == countryCode);
         var validObservatories = _context.Observatories
             .Where(o => o.ObservatoryType == ObservatoryType.Swtich || (o.ObservatoryType == ObservatoryType.Bank && o.BankId == bank.Id))
             .ToList();

         var data = new List<TransactionGraphEdgeDetails>();

         foreach (var observatory in validObservatories)
         {
             ElasticClient elasticClient = observatory.UseDefault ?
                 _elasticSearchService.connect() :
                 _elasticSearchService.connect(observatory.ElasticSearchHost);

             var searchResponse = elasticClient.Search<TransactionDocument>(s => s
                 .Query(q => q
                     .Bool(b => b
                         .Filter(f => f
                             .Term(t => t.Field("observatoryId").Value(observatory.Id))
                             && f.DateRange(dr => dr
                                 .Field("transactionDate")
                                 .GreaterThanOrEquals(date)
                             )
                             && f.Term(t => t.Field("accountNumber.keyword").Value(accountNumber))
                             && f.Term(t => t.Field("bankCode.keyword").Value(bankCode))
                             && f.Term(t => t.Field("country.keyword").Value(countryCode))
                         )
                     )
                 )
             );

             if (!searchResponse.IsValid || searchResponse.Documents.Count == 0)
             {
                 continue;
             }

             // Connect to the graph database
             var connected = connect(observatory.Id);
             if (!connected || _connector == null)
             {
                 throw new ValidateErrorException("Unable to Connect to Graph Transaction");
             }

             var g = _connector.traversal();

             foreach (var document in searchResponse.Documents)
             {
                 var platformId = document.PlatformId;

                 var transactionNode = g.V()
                     .HasLabel(JanusService.TransactionNode)
                     .Has("PlatformId", platformId)
                     .Id();

                 if (!transactionNode.HasNext())
                 {
                     continue;
                 }

                 var nodeId = transactionNode.Next();
                 var nodeDetails = g.V(nodeId).ValueMap<dynamic, dynamic>().Next();

                 IList<IDictionary<string, object>> edges = g.V(nodeId).BothE().ToList().Select(edge =>
      (IDictionary<string, object>)new Dictionary<string, object>
      {
             {"id", edge.Id.ToString()},
             {"label", edge.Label},
             {"properties", edge.Properties.ToDictionary(p => p.Key, p => (object)p.Value)}
      }
  ).ToList();


                 var response = new TransactionGraphEdgeDetails
                 {
                     Edges = edges,
                     Node = nodeDetails
                 };

                 data.Add(response);
             }
         }

         return data;
     }*/



    private bool connect(int ObservatoryId)
    {
        try
        {
            _connector = _graphService.connect(ObservatoryId);
            return true;
        }
        catch (Exception exception)
        {
            throw new ValidateErrorException("Could not connect to Graph, Please check connection or contact Admin " + exception.Message);
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
            throw new ValidateErrorException("Could not connect to Graph, Please check connection or contact Admin " + exception.Message);
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
