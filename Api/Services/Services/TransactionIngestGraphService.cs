using Api.CustomException;
using Api.Data;
using Api.Models;
using Api.Models.Data;
using Api.Services.Interfaces;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Structure;
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
            _connector = _graphService.connect(ObservatoryId);
            // _graphService.RunIndexQuery();
            _Client = ElasticClient(ObservatoryId);
            _g = _connector.traversal();
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
    public async Task<bool> IngestTransactionInGraph(TransactionIngestData data)
    {
        try
        {
            connect(data.ObservatoryId);
            _g.Tx().Begin();

            _banks = _context.Banks.ToList();
            var debitCustomerResponse = IndexSingleCustomerAndAccount(data.DebitCustomer, data.DebitAccount);
            var creditCustomerResponse = IndexSingleCustomerAndAccount(data.CreditCustomer, data.CreditAccount);
            var accountEdgeIndexed = AddAccountEdge(data.DebitAccount.AccountId, data.CreditAccount.AccountId,
             data.Transaction.TransactionDate, data.Transaction.Amount);
            var transactionIndexed = await AddTransaction(data.Transaction, data.DebitAccount, data.CreditAccount);
            if (data.Device != null)
            {
                var deviceIndexed = AddDevice(data.Device, data.Transaction, data.DebitCustomer);
            }

            await _g.Tx().CommitAsync();
            if (debitCustomerResponse && creditCustomerResponse && accountEdgeIndexed && transactionIndexed)
            {
                MarkAccountAsIndexed(data.DebitAccount);
                MarkAccountAsIndexed(data.CreditAccount);
                MarkCustomerAsIndexed(data.DebitCustomer);
                MarkCustomerAsIndexed(data.CreditCustomer);
                if (data.Device != null)
                    MarkDeviceIndexed(data.Device);

                var transactionDocumentQuery = _Client.Search<TransactionDocument>(s =>
                 s.Size(1).Query(q => q.Bool(b =>
                  b.Must(
                    m => m.Match(ma => ma.Field(f => f.PlatformId).Query(data.Transaction.PlatformId)),
                       m => m.Match(ma => ma.Field(f => f.Type).Query("Transaction")))
                     )));
                var transactionUpdateDocument = transactionDocumentQuery.Hits.First();
                var response = _Client.Update<TransactionDocument, object>(transactionUpdateDocument.Id, t => t.Doc(
                         new
                         {
                             Indexed = true
                         }
                  ));
            }
            return true;
        }
        catch (Exception Exception)
        {
            await _g.Tx().RollbackAsync();
            throw new ValidateErrorException("Unable to Add Transaction");
            // return false;
        }
        finally
        {
            _connector.Client().Dispose();
        }
    }
    private async Task<bool> AddUserAccountEdge(CustomerDocument Customer, AccountDocument Account)
    {
        // var g = _connector.traversal();
        try
        {
            _g.Tx().Begin();
            var DebitEdgeExists = _g.V().HasLabel(JanusService.CustomerNode).Has("CustomerId", Customer.CustomerId)
              .OutE("Owns")
              .Where(__.InV().Has(JanusService.AccountNode, "AccountId", Account.AccountId))
              .HasNext();

            if (!DebitEdgeExists)
            {
                _g.V().HasLabel(JanusService.CustomerNode).Has("CustomerId", Customer.CustomerId)
                .As("C").V().HasLabel(JanusService.AccountNode).Has("AccountId", Account.AccountId)
                .AddE("Owns").From("C").Next();
            }
            await _g.Tx().CommitAsync();
            return true;
        }
        catch (Exception Exception)
        {
            await _g.Tx().RollbackAsync();
            return false;
        }
    }
    private bool AddAccountEdge(string DebitAccountId, string CreditAccountId, DateTime TransactionDate, float Amount)
    {
        try
        {

            var relationship = _g.V()
            .HasLabel(JanusService.AccountNode)
            .Has("AccountId", DebitAccountId)
            .BothE("Transfered")
            .Where(__.OtherV().HasLabel(JanusService.AccountNode)
            .Has("AccountId", CreditAccountId))
            .Id();
            if (relationship.HasNext())
            {
                var EdgeId = relationship.Next();
                var LastEMEA = _g.E(EdgeId).Values<double>("LastEMEA").Next();
                var LastTransactionDate = DateTime.Parse(_g.E(EdgeId).Values<string>("LastTransactionDate").Next());
                var LastWeight = _g.E(EdgeId).Values<object>("LastWeight").Next();
                var TransactionCount = _g.E(EdgeId).Values<int>("TransactionCount").Next();
                var EMEA = _FrequencyCalculator.Calculate(LastEMEA, TransactionDate, LastTransactionDate);
                var Weight = _WeightCalculator.Calculate(EMEA, Amount, LastTransactionDate);

                var edge = _g.E(EdgeId);
                edge = edge.Property("LastEMEA", EMEA);
                edge = edge.Property("LastTransactionDate", TransactionDate);
                edge = edge.Property("LastWeight", Weight);
                edge = edge.Property("TransactionCount", TransactionCount + 1);
                edge.Iterate();
            }
            else
            {

                var EMEA = _FrequencyCalculator.Calculate();
                var Weight = _WeightCalculator.Calculate(EMEA, Amount);
                _g.V().HasLabel(JanusService.AccountNode).Has("AccountId", DebitAccountId)
                      .As("D").V().HasLabel(JanusService.AccountNode).Has("AccountId", CreditAccountId)
                      .AddE("Transfered").From("D")
                      .Property("LastEMEA", EMEA)
                      .Property("LastTransactionDate", TransactionDate)
                      .Property("LastWeight", Weight)
                      .Property("TransactionCount", 1)
                      .Iterate();
            }
            return true;
        }
        catch (Exception Exception)
        {
            return false;
        }
    }

    private async Task<bool> AddTransaction(TransactionDocument Transaction, AccountDocument DebitAccount, AccountDocument CreditAccount)
    {
        try
        {

          _g.AddV(JanusService.TransactionNode)
             .Property("PlatformId", Transaction.PlatformId)
             .Property("TransactionId", Transaction.TransactionId)
             .Property("Amount", Transaction.Amount)
             .Property("TransactionDate", Transaction.TransactionDate)
             .Property("Timestamp", DateTime.UtcNow)
             .Property("Type", TransactionType.Withdrawal)
             .Property("Currency", Transaction.Currency)
             .Property("Description", Transaction.Description)
             .Property("ObservatoryId", Transaction.ObservatoryId)
             .As("TransactionNode").V().HasLabel(JanusService.AccountNode)
             .Has("AccountId", DebitAccount.AccountId).As("DebitAccountNode")
             .V().HasLabel(JanusService.AccountNode).Has("AccountId", CreditAccount.AccountId).As("CreditAccountNode")
             .AddE("SENT").From("DebitAccountNode").To("TransactionNode").Property("CreatedAt", Transaction.CreatedAt)
             .AddE("RECEIVED").From("TransactionNode").To("CreditAccountNode").Property("CreatedAt", Transaction.CreatedAt)
             .Iterate();
            //     _g.V().HasLabel(JanusService.AccountNode).Has("AccountId", DebitAccount.AccountId).As("A1")
            //    .V().HasLabel(JanusService.TransactionNode).Has("PlatformId", Transaction.PlatformId).AddE("SENT")
            //    .From("A1").Property("CreatedAt", Transaction.CreatedAt).Iterate();

            //     _g.V().HasLabel(JanusService.TransactionNode).Has("PlatformId", Transaction.PlatformId)
            //     .As("T1").V().HasLabel(JanusService.AccountNode).Has("AccountId", CreditAccount.AccountId)
            //     .AddE("RECEIVED").From("T1").Property("CreatedAt", Transaction.CreatedAt).Iterate();
            return true;
        }
        catch (Exception exception)
        {
            return false;
        }
    }
    private bool AddDevice(DeviceDocument TransactionProfile, TransactionDocument Transaction, CustomerDocument Customer)
    {

        try
        {
            if (!TransactionProfile.Indexed)
            {
                _g.AddV(JanusService.DeviceNode)
                    .Property("ProfileId", TransactionProfile.ProfileId)
                        .Property("DeviceId", TransactionProfile.DeviceId)
            .Property("DeviceType", TransactionProfile.DeviceType)
            .Property("IpAddress", TransactionProfile.IpAddress)
            .Property("Timestamp", DateTime.UtcNow);
            }

            var DeviceEdgeExist = _g.V().HasLabel(JanusService.CustomerNode).Has("CustomerId", Customer.CustomerId)
                .OutE("USED_DEVICE")
                .Where(__.InV().Has(JanusService.DeviceNode, "ProfileId", TransactionProfile.ProfileId))
                .HasNext();

            if (!DeviceEdgeExist)
            {
                _g.V().HasLabel(JanusService.CustomerNode).Has("CustomerId", Customer.CustomerId).As("c")
                    .V().Has(JanusService.DeviceNode, "ProfileId", TransactionProfile.ProfileId).AddE("USED_DEVICE")
                    .From("c").Property("CreatedAt", DateTime.UtcNow).Iterate();
            }

            _g.V().HasLabel(JanusService.TransactionNode).Has("PlatformId", Transaction.PlatformId)
                .As("t1").V().Has(JanusService.DeviceNode, "ProfileId", TransactionProfile.ProfileId)
                .AddE("EXECUTED_ON").From("t1").Property("CreatedAt", Transaction.CreatedAt).Iterate();
            return true;
        }
        catch (Exception Exception)
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
                    sh => sh.Match(m => m.Field(f => f.Type).Query("Transaction"))
                ))
                ));
            if (CountQuery.Count <= 0)
            {
                return true;
            }
            var totalBatches = CountQuery.Count / BatchSize;
            for (var i = 0; i <= totalBatches; i++)
            {
                _g.Tx().Begin();
                var from = i * BatchSize;
                var Response = _Client.Search<TransactionDocument>(c =>
                c.From(from).Size(BatchSize).Query(q =>
                      q.Bool(b => b.Must(
                    sh => sh.Term(m => m.Field(f => f.Indexed).Value(false)),
                    sh => sh.Match(m => m.Field(f => f.Type).Query("Transaction"))
                ))
                ));
                if (!Response.IsValid)
                {
                    throw new ValidateErrorException("Invalid search");
                }
                await AddTransactionsBatch(Response);
                await _g.Tx().CommitAsync();
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
            var transactionIndexed = await AddTransaction(document, DebitAccountDocument, CreditAccountDocument);
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
                             m => m.Match(ma => ma.Field(f => f.Type).Query("Account")))
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
                             m => m.Match(ma => ma.Field(f => f.Type).Query("Customer")))
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
                               m => m.Match(ma => ma.Field(f => f.Type).Query("Device")))
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
        var traversal = _g.V();
        if (accountDocument.Indexed)
        {
            return true;
        }
        if (!customerDocument.Indexed)
        {
            traversal = _g.AddV(JanusService.CustomerNode)
                                   .Property("CustomerId", customerDocument.CustomerId)
                                   .Property("Name", customerDocument.FullName)
                                   .Property("Phone", customerDocument.Phone)
                                   .Property("Email", customerDocument.Email).As("C1" + customerDocument.CustomerId);
        }
        else
        {
            traversal = traversal.HasLabel(JanusService.CustomerNode).Has("CustomerId", customerDocument.CustomerId).As("C1" + customerDocument.CustomerId);
        }
        if (!accountDocument.Indexed)
        {
            var bank = _banks.Where(b => b.Id == accountDocument.BankId).First();
            traversal.AddV(JanusService.AccountNode)
                                 .Property("AccountId", accountDocument.AccountId)
                                 .Property("AccountNumber", accountDocument.AccountNumber)
                                 .Property("BankCode", bank.Code)
                                 .Property("Country", bank.Country)
                                 .Property("Balance", accountDocument?.AccountBalance).AddE("Owns")
                                 .From("C1" + customerDocument.CustomerId).Iterate();
        }
        return true;

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