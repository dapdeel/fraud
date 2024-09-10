using Api.CustomException;
using Api.Data;
using Api.Interfaces;
using Api.Models;
using Api.Services.Interfaces;
using Gremlin.Net.Structure;
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


    public TransactionGraphDetails GetTransactionAsync(int observatoryId, string transactionId)
    {
        var elasticClient = _elasticSearchService.connect();

        var transactionResponse = elasticClient.Search<TransactionDocument>(s => s
            .Query(q => q
                .Bool(b => b
                    .Must(m => m
                        && m.Term(t => t.Field(doc => doc.Indexed).Value(true))
                        && m.Term(t => t.Field(doc => doc.ObservatoryId).Value(observatoryId))
                        && m.Term(t => t.Field(doc => doc.TransactionId.Suffix("keyword")).Value(transactionId))
                    )
                )
            )
        );

        if (!transactionResponse.IsValid || transactionResponse.Documents.Count == 0)
        {
            throw new ValidateErrorException("This transaction does not exist, kindly try again.");
        }

        var transactionDocument = transactionResponse.Documents.First();
        var creditAccountId = transactionDocument.CreditAccountId;
        var debitAccountId = transactionDocument.DebitAccountId;

        var accountIds = new[] { creditAccountId, debitAccountId };
        var accountDocuments = GetAccountDocuments(accountIds, elasticClient);

        var edges = new List<Edge>();

        var nodes = CreateNodes(accountDocuments, creditAccountId, debitAccountId);

        var response = new TransactionGraphDetails
        {
            Edges = edges,
            Node = nodes
        };

        return response;
    }

    private IDictionary<dynamic, dynamic> GetAccountDocuments(string[] accountIds, IElasticClient elasticClient)
    {
        var accountDocuments = new Dictionary<dynamic, dynamic>();

        var accountResponse = elasticClient.Search<AccountDocument>(s => s
            .Query(q => q
                .Bool(b => b
                    .Must(m => m
                        && m.Term(t => t.Field(doc => doc.Indexed).Value(true))
                        && m.Terms(t => t.Field(doc => doc.AccountId.Suffix("keyword")).Terms(accountIds))
                    )
                )
            )
        );

        if (accountResponse.IsValid)
        {
            foreach (var accountDocument in accountResponse.Documents)
            {
                accountDocuments[accountDocument.AccountId] = accountDocument;
            }
        }

        return accountDocuments;
    }

    private IDictionary<dynamic, dynamic> CreateNodes(IDictionary<dynamic, dynamic> accountDocuments, string creditAccountId, string debitAccountId)
    {
        var nodes = new Dictionary<dynamic, dynamic>();

        foreach (var (accountId, accountDocument) in accountDocuments)
        {
            var label = (accountId == creditAccountId) ? "Credit" : "Debit";
            nodes[accountId] = new
            {
                ((AccountDocument)accountDocument).AccountId,
                ((AccountDocument)accountDocument).AccountNumber,
                ((AccountDocument)accountDocument).BankId,
                ((AccountDocument)accountDocument).CustomerId,
                ((AccountDocument)accountDocument).CreatedAt,
                ((AccountDocument)accountDocument).UpdatedAt,
                Label = label
            };
        }

        return nodes;
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


    public List<TransactionTraceResult> Trace(DateTime date, string accountNumber, int bankId, string countryCode)
    {
        var bank = _context.Banks
            .FirstOrDefault(b => b.Id == bankId && b.Country == countryCode);

        if (bank == null)
        {
            throw new ValidateErrorException("Invalid bank details");
        }

        var validObservatories = _context.Observatories
            .Where(o => o.ObservatoryType == ObservatoryType.Swtich || (o.ObservatoryType == ObservatoryType.Bank && o.BankId == bank.Id))
            .ToList();

        var data = new List<TransactionTraceResult>();

        foreach (var observatory in validObservatories)
        {
            var elasticClient = observatory.UseDefault
                ? _elasticSearchService.connect()
                : _elasticSearchService.connect(observatory.ElasticSearchHost);

            var accountDocument = _accountService.GetByAccountNumberAndBankId(accountNumber, bank.Id, observatory);

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
                                                .Field("document.keyword")
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
                                                .Value(accountDocument.AccountId)
                                            )
                                        )
                                        .MinimumShouldMatch(1)
                                    ),
                                    ffff => ffff
                                    .Bool(bbbbb => bbbbb
                                        .Should(sh => sh
                                            .DateRange(t => t
                                                .Field("transactionDate")
                                                .GreaterThan(date)
                                            )
                                        )
                                        .MinimumShouldMatch(1)
                                    ),
                                    ffff => ffff
                                    .Bool(bbbbb => bbbbb
                                        .Should(sh => sh
                                            .Match(t => t
                                                .Field("observatoryId")
                                                .Query(observatory.Id.ToString())
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

            var transactions = searchResponse.Documents.ToList();

            if (transactions.Count <= 0)
            {
                throw new ValidateErrorException("No Transactions found");
            }

            foreach (var transaction in transactions)
            {
                var response = new TransactionTraceResult
                {
                    Edges = new List<IDictionary<string, object>>(), 
                    Node = new TransactionNode
                    {
                        PlatformId = transaction.PlatformId,
                        Amount = transaction.Amount,
                        Currency = transaction.Currency,
                        Description = transaction.Description,
                        TransactionType = transaction.TransactionType.ToString(),
                        TransactionDate = transaction.TransactionDate,
                        TransactionId = transaction.TransactionId,
                        DebitAccountId = transaction.DebitAccountId,
                        CreditAccountId = transaction.CreditAccountId,
                        DeviceDocumentId = transaction.DeviceDocumentId,
                        ObservatoryId = transaction.ObservatoryId,
                        CreatedAt = transaction.CreatedAt,
                        UpdatedAt = transaction.UpdatedAt,
                        Document = transaction.Document
                    }
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


    public List<TransactionGraphDetails> GetTransactions(int observatoryId, DateTime transactionDate, int pageNumber, int batch)
    {
        var elasticClient = _elasticSearchService.connect();
        int from = pageNumber * batch;
        int size = batch;

        var searchResponse = elasticClient.Search<TransactionDocument>(s => s
            .Query(q => q
                .Bool(b => b
                    .Filter(f => f
                        .Term(t => t.Field("observatoryId").Value(observatoryId))
                        && f.DateRange(r => r
                            .Field("transactionDate")
                            .GreaterThanOrEquals(transactionDate)
                        )
                    )
                )
            )
            .From(from)
            .Size(size)
        );

        if (!searchResponse.IsValid)
        {
            throw new ValidateErrorException("Unable to query Elasticsearch for transactions.");
        }

        var data = new List<TransactionGraphDetails>();

        foreach (var hit in searchResponse.Hits)
        {
            var nodeDetails = ConvertToDictionary(hit.Source); 

            var response = new TransactionGraphDetails
            {
                Edges = null, 
                Node = nodeDetails
            };

            data.Add(response);
        }

        return data;
    }

    private IDictionary<dynamic, dynamic> ConvertToDictionary(TransactionDocument document)
    {
        var dictionary = new Dictionary<dynamic, dynamic>();

        dictionary["platformId"] = document.PlatformId;
        dictionary["amount"] = document.Amount;
        dictionary["description"] = document.Description;
        dictionary["transactionType"] = document.TransactionType;
        dictionary["indexed"] = document.Indexed;
        dictionary["type"] = document.Type;
        dictionary["transactionDate"] = document.TransactionDate;
        dictionary["transactionId"] = document.TransactionId;
        dictionary["debitAccountId"] = document.DebitAccountId;
        dictionary["creditAccountId"] = document.CreditAccountId;
        dictionary["observatoryId"] = document.ObservatoryId;
        dictionary["createdAt"] = document.CreatedAt;
        dictionary["updatedAt"] = document.UpdatedAt;

        return dictionary;
    }

    public long GetTransactionCount(int observatoryId, DateTime transactionDate)
    {
        var elasticClient = _elasticSearchService.connect();

        string formattedDate = transactionDate.ToString("yyyy-MM-ddTHH:mm:ssZ");

        var searchResponse = elasticClient.Count<TransactionDocument>(s => s
            .Query(q => q
                .Bool(b => b
                    .Filter(f => f
                        .Bool(bb => bb
                            .Must(
                                t => t
                                    .Term("observatoryId", observatoryId),
                                d => d
                                    .DateRange(r => r
                                        .Field("transactionDate")
                                        .GreaterThanOrEquals(formattedDate)
                                    )
                            )
                        )
                    )
                )
            )
        );

        if (!searchResponse.IsValid)
        {
            throw new ValidateErrorException("Unable to query Elasticsearch for transaction count.");
        }

        return searchResponse.Count;
    }

}
