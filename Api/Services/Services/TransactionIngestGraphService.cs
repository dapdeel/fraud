using Api.CustomException;
using Api.Data;
using Api.Models;
using Api.Models.Data;
using Api.Services.Interfaces;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Structure;
using Hangfire;
using Microsoft.AspNetCore.CookiePolicy;
using Nest;

public class TransactionIngestGraphService : ITransactionIngestGraphService
{
    private IGraphService _graphService;
    private readonly ApplicationDbContext _context;
    private GraphTraversalSource _g;
    private GraphTraversal<Vertex, Vertex> Traversal;
    private JanusGraphConnector _connector;
    private FrequencyCalculator _FrequencyCalculator;
    private WeightCalculator _WeightCalculator;
    private IElasticSearchService _ElasticSearchService;
    private ElasticClient _Client;
    private List<Bank> _banks;

    private int BatchSize = 500;

    public TransactionIngestGraphService(IGraphService graphService, ApplicationDbContext context, IElasticSearchService ElasticSearchService)
    {
        _graphService = graphService;
        _ElasticSearchService = ElasticSearchService;
        _FrequencyCalculator = new FrequencyCalculator();
        _WeightCalculator = new WeightCalculator();
        _context = context;
    }
    private bool connect(int ObservatoryId)
    {
        try
        {
            // _connector = _graphService.connect(ObservatoryId);
            // // _graphService.RunIndexQuery();
            _Client = ElasticClient(ObservatoryId);
            // _g = _connector.traversal();
            return true;
        }
        catch (Exception exception)
        {
            throw new ValidateErrorException("Could not connect tor Graph, Please check connection or contact Admin " + exception.Message);
        }
    }
    private ElasticClient ElasticClient(int ObservatoryId, bool Refresh = false)
    {
        var Observatory = _context.Observatories.Find(ObservatoryId);
        if (Observatory == null || Observatory.UseDefault)
        {
            _Client = _ElasticSearchService.connect();
            return _Client;
        }
        var Host = Observatory.ElasticSearchHost;
        if (Host == null)
        {
            throw new ValidateErrorException("Unable to connect to Elastic Search");
        }
        _Client = _ElasticSearchService.connect(Host);
        return _Client;

    }
    [Queue("graphingestqueue")]
    public async Task<bool> IngestTransactionInGraph(TransactionIngestData data)
    {
        try
        {
            connect(data.ObservatoryId);
            _banks = _context.Banks.ToList();
            var debitCustomerResponse = IndexSingleCustomerAndAccount(data.DebitCustomer, data.DebitAccount);
            var creditCustomerResponse = IndexSingleCustomerAndAccount(data.CreditCustomer, data.CreditAccount);
            var accountEdgeIndexed = AddAccountEdge(data.DebitAccount.AccountId, data.CreditAccount.AccountId,
             data.Transaction.TransactionDate, data.Transaction.Amount);
            var transactionIndexed = AddTransaction(data.Transaction, data.DebitAccount, data.CreditAccount);
            if (data.Device != null)
            {
                var deviceIndexed = AddDevice(data.Device, data.Transaction, data.DebitCustomer);
            }
            if (debitCustomerResponse && creditCustomerResponse && accountEdgeIndexed && transactionIndexed)
            {
                var refreshResponse = _Client.Indices.Refresh("transactions");

                var transactionDocumentQuery = _Client.Search<TransactionDocument>(s =>
                 s.Size(1).Query(q => q.Bool(b =>
                    b.Filter(
                        f => f.Bool(b => b.Should(sh => sh.MatchPhrase(m => m.Field(f => f.Document).Query(NodeData.Transaction)))),
                        f => f.Bool(b => b.Should(sh => sh.MatchPhrase(m => m.Field(f => f.PlatformId).Query(data.Transaction.PlatformId))))
                        )
                     )));
               
                if (transactionDocumentQuery.Hits.Count <= 0)
                {
                    BackgroundJob.Enqueue(() => UpdateIndexedTransaction(data.ObservatoryId, data.Transaction.PlatformId));
                }
                else
                {
                     var transactionUpdateDocument = transactionDocumentQuery.Hits.First();
                    Console.WriteLine("IDIS" + transactionUpdateDocument.Id);
                    var response = _Client.Update<TransactionDocument, object>(transactionUpdateDocument.Id, t => t.Doc(
                             new
                             {
                                 indexed = true
                             }
                      ));
                    return response.IsValid;
                }
            }
            return false;
        }
        catch (Exception Exception)
        {
            Console.WriteLine("lasaexception1" + Exception.InnerException.ToString() + " " + Exception.Message);
            throw new ValidateErrorException("There were issues in add this index");
        }
    }

    [Queue("graphtransactionupdatequeue")]
    public bool UpdateIndexedTransaction(int ObservatoryId, string PlatformId)
    {
        connect(ObservatoryId);
        var transactionDocumentQuery = _Client.Search<TransactionDocument>(s =>
                        s.Size(1).Query(q => q.Bool(b =>
                           b.Filter(
                               f => f.Bool(b => b.Should(sh => sh.MatchPhrase(m => m.Field(f => f.Document).Query(NodeData.Transaction)))),
                               f => f.Bool(b => b.Should(sh => sh.MatchPhrase(m => m.Field(f => f.PlatformId).Query(PlatformId))))
                               )
                            )));
        var transactionUpdateDocument = transactionDocumentQuery.Hits.FirstOrDefault();
        if (transactionUpdateDocument == null)
        {
            return false;
        }
        var response = _Client.Update<TransactionDocument, object>(transactionUpdateDocument.Id, t => t.Doc(
                            new
                            {
                                indexed = true
                            }
                     ));
        return response.IsValid;
    }

    public bool IndexPendingTransactions()
    {
        var observatories = _context.Observatories.Where(o => o.HasConnected == true).ToList();
        foreach (var observatory in observatories)
        {
            connect(observatory.Id);
            var searchResponse = _Client.Search<TransactionDocument>(s =>
                           s.Size(1000).Query(q => q.Bool(b =>
                              b.Filter(
                                  f => f.Bool(b => b.Should(sh => sh.MatchPhrase(m => m.Field(f => f.Document).Query(NodeData.Transaction)))),
                                  f => f.Bool(b => b.Should(sh => sh.Term(m => m.Field(f => f.Indexed).Value(false)))),
                                  f => f.Bool(b => b.Should(sh => sh.MatchPhrase(m => m.Field(f => f.Type).Query(DocumentType.Node))))
                                  )
               )));
            if (searchResponse.IsValid && searchResponse.Hits.Any())
            {

                foreach (var hit in searchResponse.Hits)
                {
                    // Update the document in Elasticsearch
                    var updateResponse = _Client.Update<TransactionDocument, object>(hit.Id, t => t.Doc(
                            new
                            {
                                indexed = true
                            }
                     )
                    );

                    if (updateResponse.IsValid)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
    private bool AddAccountEdge(string DebitAccountId, string CreditAccountId, DateTime TransactionDate, float Amount)
    {
        try
        {

            var searchResponse = _Client.Search<TransferedEgdeDocument>(s => s.Size(1).Query(q => q.Bool(b =>
                b.Filter(f =>
                f.Bool(b => b.Should(sh => sh.MatchPhrase(m => m.Field(f => f.From).Query(DebitAccountId)))),
                f => f.Bool(b => b.Should(sh => sh.MatchPhrase(m => m.Field(f => f.To).Query(CreditAccountId)))
                ))
            )));

            if (searchResponse.Hits.Count <= 0)
            {
                var EMEA = _FrequencyCalculator.Calculate();
                var Weight = _WeightCalculator.Calculate(EMEA, Amount);
                var Document = new TransferedEgdeDocument
                {
                    Document = EdgeData.Transfered,
                    EMEA = EMEA,
                    From = DebitAccountId,
                    To = CreditAccountId,
                    EdgeId = Guid.NewGuid().ToString(),
                    Weight = Weight,
                    LastTransactionDate = TransactionDate,
                    TransactionCount = 1,
                    Type = DocumentType.Node,
                    CreatedAt = DateTime.Now
                };
                var response = _Client.IndexDocument(Document);
                return response.IsValid;
            }
            else
            {
                var updateDocument = searchResponse.Hits.First();
                var searchDocument = searchResponse.Documents.First();

                var LastEMEA = searchDocument.EMEA;
                var LastTransactionDate = searchDocument.LastTransactionDate;
                var LastWeight = searchDocument.Weight;
                var TransactionCount = searchDocument.TransactionCount;
                var EMEA = _FrequencyCalculator.Calculate(LastEMEA, TransactionDate, LastTransactionDate);
                var Weight = _WeightCalculator.Calculate(EMEA, Amount, LastTransactionDate);
                var latestTransactionCount = TransactionCount + 1;
                var response = _Client.Update<TransferedEgdeDocument, object>(updateDocument.Id, t => t.Doc(
                new
                {
                    EMEA,
                    Weight,
                    LastTransactionDate = TransactionDate,
                    TransactionCount = latestTransactionCount
                }
                ));
                Console.WriteLine("Boss Boss");
                return response.IsValid;
            }
        }
        catch (Exception Exception)
        {
            throw new ValidateErrorException(Exception.Message);
        }
    }

    private bool AddTransaction(TransactionDocument Transaction, AccountDocument DebitAccount, AccountDocument CreditAccount)
    {
        try
        {
            var sentDocument = new SentEdgeDocument
            {
                Document = EdgeData.Sent,
                From = DebitAccount.AccountId,
                EdgeId = Guid.NewGuid().ToString(),
                To = Transaction.PlatformId,
                Type = DocumentType.Edge,
                CreatedAt = DateTime.Now
            };
            var sentDocumentResponse = _Client.IndexDocument(sentDocument);
            var receivedDocument = new RecievedEdgeDocument
            {
                Document = EdgeData.Received,
                From = Transaction.PlatformId,
                EdgeId = Guid.NewGuid().ToString(),
                To = CreditAccount.AccountId,
                Type = DocumentType.Edge,
                CreatedAt = DateTime.Now
            };
            var receivedDocumentResponse = _Client.IndexDocument(receivedDocument);
            return sentDocumentResponse.IsValid && receivedDocumentResponse.IsValid;
        }
        catch (Exception exception)
        {
            throw new ValidateErrorException(exception.Message);
        }
    }
    private bool AddDevice(DeviceDocument TransactionProfile, TransactionDocument Transaction, CustomerDocument Customer)
    {

        try
        {


            if (!TransactionProfile.Indexed)
            {
                var searchResponse = _Client.Search<UsedDeviceEdgeDocument>(s => s.Size(1).Query(q => q.Bool(b => b.Must(
                        m => m.Term(t => t.Field(f => f.From).Value(Customer.CustomerId)),
                    m => m.Term(t => t.Field(f => f.To).Value(TransactionProfile.ProfileId)),
                    m => m.Term(t => t.Field(f => f.Document).Value(NodeData.Device))
                    ))));
                if (searchResponse.Documents.Count() <= 0)
                {

                    var UsedDeviceEdgeDocument = new UsedDeviceEdgeDocument
                    {
                        Document = EdgeData.Used,
                        EdgeId = Guid.NewGuid().ToString(),
                        From = Customer.CustomerId,
                        To = TransactionProfile.ProfileId,
                        Type = DocumentType.Edge,
                        CreatedAt = DateTime.Now
                    };
                    _Client.IndexDocument(UsedDeviceEdgeDocument);
                    MarkDeviceIndexed(TransactionProfile);
                }
            }

            var ExecutedOnEdgeDocument = new ExecutedOnEdgeDocument
            {
                Document = EdgeData.ExecutedOn,
                EdgeId = Guid.NewGuid().ToString(),
                From = Transaction.TransactionId,
                To = TransactionProfile.ProfileId,
                Type = DocumentType.Edge,
                CreatedAt = DateTime.Now
            };
            var response = _Client.IndexDocument(ExecutedOnEdgeDocument);
            return response.IsValid;
        }
        catch (Exception)
        {
            return false;
        }
    }
    private async Task<bool> AddTransactions()
    {
        try
        {
            var CountQuery = _Client.Count<TransactionDocument>(c =>
            c.Query(q =>
                q.Bool(b => b.Must(
                    sh => sh.Term(m => m.Field(f => f.Indexed).Value(false)),
                    sh => sh.Match(m => m.Field(f => f.Document).Query(NodeData.Transaction))
                ))
                ));
            if (CountQuery.Count <= 0)
            {
                return true;
            }
            var totalBatches = CountQuery.Count / BatchSize;
            for (var i = 0; i <= totalBatches; i++)
            {
                var from = i * BatchSize;
                var Response = _Client.Search<TransactionDocument>(c =>
                c.From(from).Size(BatchSize).Query(q =>
                      q.Bool(b => b.Must(
                    sh => sh.Term(m => m.Field(f => f.Indexed).Value(false)),
                    sh => sh.Match(m => m.Field(f => f.Document).Query(NodeData.Transaction))
                ))
                ));
                if (!Response.IsValid)
                {
                    throw new ValidateErrorException("Invalid search");
                }
                await AddTransactionsBatch(Response);
            }
            return true;
        }
        catch (Exception exception)
        {
            await _g.Tx().RollbackAsync();
            throw new ValidateErrorException("Error Adding to Graph!!! " + exception.Message);
        }
    }

    private async Task<bool> AddTransactionsBatch(ISearchResponse<TransactionDocument> responses)
    {
        foreach (var hit in responses.Hits)
        {
            var id = hit.Id;
            var document = hit.Source;
            var debitAccountResponse = _Client.Search<AccountDocument>(a =>
                            a.Size(1)
                            .Query(q =>
                            q.Bool(b =>
                            b.Must(sh =>
                                    sh.Match(sh => sh.Field(f => f.AccountId).Query(document.DebitAccountId)),
                                    sh => sh.Match(sh => sh.Field(f => f.Type).Query("Account"))
                                ))
                            ));
            var creditAccountResponse = _Client.Search<AccountDocument>(a =>
                                a.Size(1)
                                .Query(q =>
                                q.Bool(b =>
                                b.Must(sh =>
                                        sh.Match(sh => sh.Field(f => f.AccountId).Query(document.CreditAccountId)),
                                        sh => sh.Match(sh => sh.Field(f => f.Type).Query("Account"))
                                    ))
                                ));
            var DebitAccountDocument = debitAccountResponse.Documents.First();
            var debitCustomerResponse = _Client.Search<CustomerDocument>(a =>
                                a.Size(1)
                                .Query(q =>
                                q.Bool(b =>
                                b.Must(sh =>
                                        sh.Match(sh => sh.Field(f => f.CustomerId).Query(DebitAccountDocument.CustomerId)),
                                        sh => sh.Match(sh => sh.Field(f => f.Type).Query("Customer"))
                                    ))
                                ));

            var DebitCustomerDocument = debitCustomerResponse.Documents.First();
            var CreditAccountDocument = creditAccountResponse.Documents.First();
            var creditCustomerResponse = _Client.Search<CustomerDocument>(a =>
                               a.Size(1)
                               .Query(q =>
                               q.Bool(b =>
                               b.Must(sh =>
                                       sh.Match(sh => sh.Field(f => f.CustomerId).Query(CreditAccountDocument.CustomerId)),
                                       sh => sh.Match(sh => sh.Field(f => f.Type).Query("Customer"))
                                   ))
                               ));
            var CreditCustomerDocument = creditCustomerResponse.Documents.First();
            var debitCustomerIndexResponse = IndexSingleCustomerAndAccount(DebitCustomerDocument, DebitAccountDocument);
            var creditCustomerIndexResponse = IndexSingleCustomerAndAccount(CreditCustomerDocument, CreditAccountDocument);
            var accountEdgeIndexed = AddAccountEdge(DebitAccountDocument.AccountId, CreditAccountDocument.AccountId,
            document.TransactionDate, document.Amount);
            var transactionIndexed = AddTransaction(document, DebitAccountDocument, CreditAccountDocument);
            var deviceResponse = _Client.Search<DeviceDocument>(a =>
                             a.Size(1)
                             .Query(q =>
                             q.Bool(b =>
                             b.Must(sh =>
                                     sh.Match(sh => sh.Field(f => f.ProfileId).Query(document.DeviceDocumentId)),
                                     sh => sh.Match(sh => sh.Field(f => f.Type).Query("Device"))
                                 ))
                             ));
            if (deviceResponse.Documents.Count > 0)
            {
                var DeviceDocument = deviceResponse.Documents.First();
                var deviceIndexed = AddDevice(DeviceDocument, document, DebitCustomerDocument);
                MarkDeviceIndexed(DeviceDocument);
            }

            if (debitCustomerIndexResponse && creditCustomerIndexResponse &&
            accountEdgeIndexed && transactionIndexed)
            {
                MarkAccountAsIndexed(DebitAccountDocument);
                MarkAccountAsIndexed(CreditAccountDocument);
                MarkCustomerAsIndexed(DebitCustomerDocument);
                MarkCustomerAsIndexed(CreditCustomerDocument);

                _Client.Update<TransactionDocument, object>(id, t => t.Doc(
                       new
                       {
                           Indexed = true
                       }
                ));
            }
        }
        return true;
    }
    private bool MarkAccountAsIndexed(AccountDocument accountDocument)
    {
        var query = _Client.Search<AccountDocument>(s =>
                       s.Size(1).Query(q => q.Bool(b =>
                        b.Must(
                          m => m.Match(ma => ma.Field(f => f.AccountId).Query(accountDocument.AccountId)),
                             m => m.Match(ma => ma.Field(f => f.Document).Query(NodeData.Account)))
                           )));
        var updateDocument = query.Hits.First();
        var response = _Client.Update<AccountDocument, object>(updateDocument.Id, t => t.Doc(
                 new
                 {
                     Indexed = true
                 }
          ));
        return true;
    }
    private bool MarkCustomerAsIndexed(CustomerDocument customerDocument)
    {
        var query = _Client.Search<CustomerDocument>(s =>
                       s.Size(1).Query(q => q.Bool(b =>
                        b.Must(
                          m => m.Match(ma => ma.Field(f => f.CustomerId).Query(customerDocument.CustomerId)),
                             m => m.Match(ma => ma.Field(f => f.Document).Query(NodeData.Customer)))
                           )));
        var updateDocument = query.Hits.First();
        var response = _Client.Update<CustomerDocument, object>(updateDocument.Id, t => t.Doc(
                 new
                 {
                     Indexed = true
                 }
          ));
        return true;
    }
    private bool MarkDeviceIndexed(DeviceDocument deviceDocument)
    {
        var query = _Client.Search<DeviceDocument>(s =>
                         s.Size(1).Query(q => q.Bool(b =>
                          b.Must(
                            m => m.Match(ma => ma.Field(f => f.ProfileId).Query(deviceDocument.ProfileId)),
                               m => m.Match(ma => ma.Field(f => f.Document).Query(NodeData.Device)))
                             )));
        if (query.Hits.Count <= 0)
        {
            return false;
        }
        var updateDocument = query.Hits.First();
        var response = _Client.Update<DeviceDocument, object>(updateDocument.Id, t => t.Doc(
                 new
                 {
                     Indexed = true
                 }
          ));
        return true;
    }
    private bool IndexSingleCustomerAndAccount(CustomerDocument customerDocument, AccountDocument accountDocument)
    {
        if (accountDocument.Indexed)
        {
            return true;
        }
        var bank = _banks.Where(b => b.Id == accountDocument.BankId).First();
        var Document = new OwnsEdgeDocument
        {
            Document = EdgeData.Owns,
            EdgeId = Guid.NewGuid().ToString(),
            From = customerDocument.CustomerId,
            To = accountDocument.AccountId,
            Type = DocumentType.Edge,
            CreatedAt = DateTime.Now
        };
        var response = _Client.IndexDocument(Document);
        MarkCustomerAsIndexed(customerDocument);
        MarkAccountAsIndexed(accountDocument);
        return response.IsValid;
    }
    public async Task<bool> RunAnalysis(int ObservatoryId)
    {

        try
        {
            connect(ObservatoryId);
            _banks = _context.Banks.ToList();
            var response = await AddTransactions();
            return true;
        }
        catch (Exception exception)
        {
            throw new ValidateErrorException(exception.Message);
            return false;
        }
    }

}