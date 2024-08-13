using Api.CustomException;
using Api.Data;
using Api.Models;
using Api.Models.Data;
using Api.Services.Interfaces;
using Gremlin.Net.Process.Traversal;

public class TransactionGraphService : ITransactionGraphService
{
    private IGraphService _graphService;
    private readonly ApplicationDbContext _context;
    private GraphTraversalSource _g;
    private JanusGraphConnector _connector;
    private FrequencyCalculator _FrequencyCalculator;
    private WeightCalculator _WeightCalculator;

    public TransactionGraphService(IGraphService graphService, ApplicationDbContext context)
    {
        _graphService = graphService;
        _FrequencyCalculator = new FrequencyCalculator();
        _WeightCalculator = new WeightCalculator();
        _context = context;
    }
    private bool initTraversal()
    {
        try
        {
            _connector = _graphService.connect();
            _g = _connector.traversal();
            return true;
        }
        catch (Exception exception)
        {
            return false;
        }
    }
    public async Task<bool> IngestTransactionInGraph(TransactionIngestData data)
    {
        initTraversal();
        try
        {
            _g.Tx().Begin();
            var DebitCustomerAdded = AddCustomer(data.DebitCustomer);
            var CreditCustomerAdded = AddCustomer(data.CreditCustomer);
            var DebitAccountAdded = AddAccount(data.DebitAccount);
            var CreditAccountAdded = AddAccount(data.CreditAccount);

            var DebitEdgeExists = _g.V().HasLabel(JanusService.CustomerNode).Has("CustomerId", data.DebitCustomer.CustomerId)
              .OutE("Owns")
              .Where(__.InV().Has(JanusService.AccountNode, "Id", data.DebitAccount.AccountId))
              .HasNext();

            if (!DebitEdgeExists)
            {
                _g.V().HasLabel(JanusService.CustomerNode).Has("CustomerId", data.DebitCustomer.CustomerId)
                .As("C").V().HasLabel(JanusService.AccountNode).Has("Id", data.DebitAccount.AccountId)
                .AddE("Owns").From("C").Next();
            }

            var CreditEdgeExists = _g.V().HasLabel(JanusService.CustomerNode).Has("CustomerId", data.CreditCustomer.CustomerId)
               .OutE("Owns")
               .Where(__.InV().Has(JanusService.AccountNode, "Id", data.CreditAccount.AccountId))
               .HasNext();

            if (!CreditEdgeExists)
            {
                _g.V().HasLabel(JanusService.CustomerNode).Has("CustomerId", data.CreditCustomer.CustomerId)
                .As("C").V().HasLabel(JanusService.AccountNode).Has("Id", data.CreditAccount.Id)
                .AddE("Owns").From("C").Next();
            }
            _g.AddV(JanusService.TransactionNode)
             .Property("Id", data.Transaction.PlatformId)
             .Property("TransactionId", data.Transaction.TransactionId)
             .Property("Amount", data.Transaction.Amount)
             .Property("TransactionDate", data.Transaction.TransactionDate)
             .Property("Timestamp", DateTime.UtcNow)
             .Property("Type", TransactionType.Withdrawal)
             .Property("Currency", data.Transaction.Currency)
             .Property("Description", data.Transaction.Description)
             .Property("ObservatoryId", data.Transaction.ObservatoryId)
             .Next();

            _g.V().Has(JanusService.AccountNode, "Id", data.DebitAccount.AccountId).As("A1")
           .V().Has(JanusService.TransactionNode, "Id", data.Transaction.PlatformId).AddE("SENT")
           .From("A1").Property("CreatedAt", data.Transaction.CreatedAt).Next();

            _g.V().Has(JanusService.TransactionNode, "Id", data.Transaction.PlatformId)
            .As("T1").V().Has(JanusService.AccountNode, "Id", data.CreditAccount.AccountId)
            .AddE("RECEIVED").From("T1").Property("CreatedAt", data.Transaction.CreatedAt).Next();

            var DebitAccountNodeId = _g.V()
                     .HasLabel(JanusService.AccountNode)
                    .Has("Id", data.DebitAccount.AccountId).Id().Next();
            var CreditAccountNodeId = _g.V()
                     .HasLabel(JanusService.AccountNode)
                    .Has("Id", data.CreditAccount.AccountId).Id().Next();
            await AddAccountEdge(DebitAccountNode: DebitAccountNodeId, CreditAccountNode: CreditAccountNodeId,
                                TransactionDate: data.Transaction.TransactionDate, Amount: data.Transaction.Amount);
            if (data.TransactionProfile != null)
            {
                _g.AddV(JanusService.DeviceNode)
                    .Property("Id", data.TransactionProfile.ProfileId)
                        .Property("DeviceId", data.TransactionProfile.DeviceId)
            .Property("DeviceType", data.TransactionProfile.DeviceType)
            .Property("IpAddress", data.TransactionProfile.IpAddress)
            .Property("Timestamp", DateTime.UtcNow)
            .Next();

                var DeviceEdgeExist = _g.V().HasLabel(JanusService.CustomerNode).Has("CustomerId", data.DebitCustomer.CustomerId)
                    .OutE("USED_DEVICE")
                    .Where(__.InV().Has(JanusService.DeviceNode, "Id", data.TransactionProfile.ProfileId))
                    .HasNext();

                if (!DeviceEdgeExist)
                {
                    _g.V().Has(JanusService.CustomerNode, "CustomerId", data.DebitCustomer.CustomerId).As("c")
                        .V().Has(JanusService.DeviceNode, "Id", data.TransactionProfile.ProfileId).AddE("USED_DEVICE")
                        .From("c").Property("CreatedAt", DateTime.UtcNow).Next();
                }

                _g.V().Has(JanusService.TransactionNode, "Id", data.Transaction.PlatformId)
                    .As("t1").V().Has(JanusService.DeviceNode, "Id", data.TransactionProfile.ProfileId)
                    .AddE("EXECUTED_ON").From("t1").Property("CreatedAt", data.Transaction.CreatedAt)
                    .Next();
            }
            await _g.Tx().CommitAsync();
        }
        catch (Exception Exception)
        {
            await _g.Tx().RollbackAsync();
            return false;
        }
        finally
        {
            _connector.Client().Dispose();
        }
        return true;
    }
    private async Task<bool> AddAccountEdge(object? DebitAccountNode, object? CreditAccountNode, DateTime TransactionDate, float Amount)
    {

        if (DebitAccountNode == null || CreditAccountNode == null)
        {
            throw new ValidateErrorException("There were issues in Adding Relationship to the Transaction ");
        }

        var relationship = _g.V(DebitAccountNode).BothE("Transfered").Where(__.OtherV().HasId(CreditAccountNode)).Id();
        if (relationship.HasNext())
        {
            var EdgeId = relationship.Next();
            var LastEMEA = _g.E(EdgeId).Values<double>("LastEMEA").Next();
            var LastTransactionDate = DateTime.Parse(_g.E(EdgeId).Values<string>("LastTransactionDate").Next());
            var LastWeight = _g.E(EdgeId).Values<double>("LastWeight").Next();
            var TransactionCount = _g.E(EdgeId).Values<int>("TransactionCount").Next();
            var EMEA = _FrequencyCalculator.Calculate(LastEMEA, TransactionDate, LastTransactionDate);
            var Weight = _WeightCalculator.Calculate(EMEA, Amount, LastTransactionDate);

            var edge = _g.E(EdgeId);
            edge = edge.Property("LastEMEA", EMEA);
            edge = edge.Property("LastTransactionDate", TransactionDate);
            edge = edge.Property("LastWeight", Weight);
            edge = edge.Property("TransactionCount", TransactionCount + 1);
            edge.Next();
        }
        else
        {

            var EMEA = _FrequencyCalculator.Calculate();
            var Weight = _WeightCalculator.Calculate(EMEA, Amount);
            _g.V(DebitAccountNode).HasLabel(JanusService.AccountNode)
                  .As("D").V(CreditAccountNode).HasLabel(JanusService.AccountNode)
                  .AddE("Transfered").From("D")
                  .Property("LastEMEA", EMEA)
                  .Property("LastTransactionDate", TransactionDate)
                  .Property("LastWeight", Weight)
                  .Property("TransactionCount", 1)
                  .Next();
        }
        await _g.Tx().CommitAsync();
        return true;
    }

    private bool AddCustomer(TransactionCustomer Customer)
    {
        var customerGraphId = _g.V()
            .HasLabel(JanusService.CustomerNode)
            .Has("CustomerId", Customer.CustomerId).Id();
        if (customerGraphId.HasNext())
        {
            _g.V(customerGraphId.Next()).Property("Phone", Customer.Phone)
            .Property("Email", Customer.Email)
            .Property("Name", Customer.FullName);
        }
        else
        {
            _g.AddV(JanusService.CustomerNode)
                .Property("CustomerId", Customer.CustomerId)
                .Property("Name", Customer.FullName)
                .Property("Phone", Customer.Phone)
                .Property("Email", Customer.Email).Next();
        }
        return true;
    }
    private bool AddAccount(TransactionAccount Account)
    {
        var accountGraph = _g.V()
                .HasLabel(JanusService.AccountNode)
               .Has("AccountNumber", Account.AccountNumber)
               .Has("BankCode", Account?.Bank?.Code)
               .Id();
        if (accountGraph.HasNext())
        {
            _g.V(accountGraph.Next()).Property("Balance", Account.AccountBalance);
        }
        else
        {
            _g.AddV(JanusService.AccountNode)
                .Property("Id", Account.AccountId)
                .Property("AccountNumber", Account.AccountNumber)
                .Property("BankCode", Account?.Bank?.Code)
                .Property("Country", Account?.Bank?.Country)
                .Property("Balance", Account?.AccountBalance).Next();
        }
        return true;
    }

}