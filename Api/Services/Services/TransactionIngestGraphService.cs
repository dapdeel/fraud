using Api.CustomException;
using Api.Data;
using Api.Models;
using Api.Models.Data;
using Api.Services.Interfaces;
using Gremlin.Net.Process.Traversal;

public class TransactionIngestGraphService : ITransactionIngestGraphService
{
    private IGraphService _graphService;
    private readonly ApplicationDbContext _context;
    private JanusGraphConnector _connector;
    private FrequencyCalculator _FrequencyCalculator;
    private WeightCalculator _WeightCalculator;

    public TransactionIngestGraphService(IGraphService graphService, ApplicationDbContext context)
    {
        _graphService = graphService;
        _FrequencyCalculator = new FrequencyCalculator();
        _WeightCalculator = new WeightCalculator();
        _context = context;
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
    public async Task<bool> IngestTransactionInGraph(TransactionIngestData data)
    {
        connect();
        try
        {
            var DebitCustomerAdded = AddCustomer(data.DebitCustomer);
            var CreditCustomerAdded = AddCustomer(data.CreditCustomer);
            var DebitAccountAdded = AddAccount(data.DebitAccount);
            var CreditAccountAdded = AddAccount(data.CreditAccount);

            var UserEdge = AddUserAccountEdge(data.DebitCustomer, data.DebitAccount);
            var CreditUserEdge = AddUserAccountEdge(data.CreditCustomer, data.CreditAccount);


            var TransactionAdded = await AddTransaction(data.Transaction, data.DebitAccount, data.CreditAccount);
            var g = _connector.traversal();

            var DebitAccountNodeId = g.V()
                     .HasLabel(JanusService.AccountNode)
                    .Has("AccountId", data.DebitAccount.AccountId).Id().Next();

            var CreditAccountNodeId = g.V()
                     .HasLabel(JanusService.AccountNode)
                    .Has("AccountId", data.CreditAccount.AccountId).Id().Next();

            var AccountEdge = await AddAccountEdge(DebitAccountNode: DebitAccountNodeId, CreditAccountNode: CreditAccountNodeId,
                                 TransactionDate: data.Transaction.TransactionDate, Amount: data.Transaction.Amount);

            if (data.TransactionProfile != null)
            {
                var AddedDevice = AddDevice(data.TransactionProfile, data.Transaction, data.DebitCustomer);
            }
            if (TransactionAdded)
            {
                var transaction = _context.Transactions.Where(t => t.PlatformId == data.Transaction.PlatformId).FirstOrDefault();
                if (transaction != null)
                {
                    transaction.Indexed = true;
                    _context.Update(transaction);
                    _context.SaveChanges();
                }
            }

        }
        catch (Exception Exception)
        {
            return false;
        }
        finally
        {
            _connector.Client().Dispose();
        }
        return true;
    }
    private async Task<bool> AddUserAccountEdge(TransactionCustomer Customer, TransactionAccount Account)
    {
        var g = _connector.traversal();
        try
        {

            g.Tx().Begin();
            var DebitEdgeExists = g.V().HasLabel(JanusService.CustomerNode).Has("CustomerId", Customer.CustomerId)
              .OutE("Owns")
              .Where(__.InV().Has(JanusService.AccountNode, "AccountId", Account.AccountId))
              .HasNext();

            if (!DebitEdgeExists)
            {
                g.V().HasLabel(JanusService.CustomerNode).Has("CustomerId", Customer.CustomerId)
                .As("C").V().HasLabel(JanusService.AccountNode).Has("AccountId", Account.AccountId)
                .AddE("Owns").From("C").Next();
            }
            await g.Tx().CommitAsync();
            return true;
        }
        catch (Exception Exception)
        {
            await g.Tx().RollbackAsync();
            return false;
        }
    }
    private async Task<bool> AddAccountEdge(object? DebitAccountNode, object? CreditAccountNode, DateTime TransactionDate, float Amount)
    {
        var g = _connector.traversal();
        try
        {
            g.Tx().Begin();
            if (DebitAccountNode == null || CreditAccountNode == null)
            {
                throw new ValidateErrorException("There were issues in Adding Relationship to the Transaction ");
            }

            var relationship = g.V(DebitAccountNode).BothE("Transfered").Where(__.OtherV().HasId(CreditAccountNode)).Id();
            if (relationship.HasNext())
            {
                var EdgeId = relationship.Next();
                var LastEMEA = g.E(EdgeId).Values<double>("LastEMEA").Next();
                var LastTransactionDate = DateTime.Parse(g.E(EdgeId).Values<string>("LastTransactionDate").Next());
                var LastWeight = g.E(EdgeId).Values<double>("LastWeight").Next();
                var TransactionCount = g.E(EdgeId).Values<int>("TransactionCount").Next();
                var EMEA = _FrequencyCalculator.Calculate(LastEMEA, TransactionDate, LastTransactionDate);
                var Weight = _WeightCalculator.Calculate(EMEA, Amount, LastTransactionDate);

                var edge = g.E(EdgeId);
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
                g.V(DebitAccountNode).HasLabel(JanusService.AccountNode)
                      .As("D").V(CreditAccountNode).HasLabel(JanusService.AccountNode)
                      .AddE("Transfered").From("D")
                      .Property("LastEMEA", EMEA)
                      .Property("LastTransactionDate", TransactionDate)
                      .Property("LastWeight", Weight)
                      .Property("TransactionCount", 1)
                      .Next();
            }
            await g.Tx().CommitAsync();
            return true;
        }
        catch (Exception Exception)
        {
            await g.Tx().RollbackAsync();
            return false;
        }
    }

    private async Task<bool> AddTransaction(Transaction Transaction, TransactionAccount DebitAccount, TransactionAccount CreditAccount)
    {
        var g = _connector.traversal();
        try
        {
            g.Tx().Begin();
            g.AddV(JanusService.TransactionNode)
             .Property("PlatformId", Transaction.PlatformId)
             .Property("TransactionId", Transaction.TransactionId)
             .Property("Amount", Transaction.Amount)
             .Property("TransactionDate", Transaction.TransactionDate)
             .Property("Timestamp", DateTime.UtcNow)
             .Property("Type", TransactionType.Withdrawal)
             .Property("Currency", Transaction.Currency)
             .Property("Description", Transaction.Description)
             .Property("ObservatoryId", Transaction.ObservatoryId).Next();

            g.V().Has(JanusService.AccountNode, "AccountId", DebitAccount.AccountId).As("A1")
           .V().Has(JanusService.TransactionNode, "PlatformId", Transaction.PlatformId).AddE("SENT")
           .From("A1").Property("CreatedAt", Transaction.CreatedAt).Next();

            g.V().Has(JanusService.TransactionNode, "PlatformId", Transaction.PlatformId)
            .As("T1").V().Has(JanusService.AccountNode, "AccountId", CreditAccount.AccountId)
            .AddE("RECEIVED").From("T1").Property("CreatedAt", Transaction.CreatedAt).Next();

            await g.Tx().CommitAsync();
            return true;
        }
        catch (Exception exception)
        {
            await g.Tx().RollbackAsync();
            return false;
        }
    }
    private async Task<bool> AddCustomer(TransactionCustomer Customer)
    {
        var g = _connector.traversal();
        try
        {
            g.Tx().Begin();
            var customerGraphId = g.V()
                .HasLabel(JanusService.CustomerNode)
                .Has("CustomerId", Customer.CustomerId).Id();
            if (customerGraphId.HasNext())
            {
                var customerNode = customerGraphId.Next();
                g.V(customerNode).Property("Phone", Customer.Phone)
                .Property("Email", Customer.Email)
                .Property("Name", Customer.FullName).Next();
            }
            else
            {

                g.AddV(JanusService.CustomerNode)
                    .Property("CustomerId", Customer.CustomerId)
                    .Property("Name", Customer.FullName)
                    .Property("Phone", Customer.Phone)
                    .Property("Email", Customer.Email).Next();
            }
            await g.Tx().CommitAsync();
            return true;
        }
        catch (Exception exception)
        {
            await g.Tx().RollbackAsync();
            return false;
        }

    }
    private async Task<bool> AddAccount(TransactionAccount Account)
    {
        var g = _connector.traversal();
        try
        {

            g.Tx().Begin();
            var bank = _context.Banks.First(b => b.Id == Account.BankId);
            var accountGraph = g.V()
                    .HasLabel(JanusService.AccountNode)
                   .Has("AccountNumber", Account.AccountNumber)
                   .Has("BankCode", bank.Code)
                   .Id();
            if (accountGraph.HasNext())
            {
                var accountNode = accountGraph.Next();
                g.V(accountNode).Property("Balance", Account.AccountBalance).Next();
            }
            else
            {
                g.AddV(JanusService.AccountNode)
                    .Property("AccountId", Account.AccountId)
                    .Property("AccountNumber", Account.AccountNumber)
                    .Property("BankCode", bank?.Code)
                    .Property("Country", bank?.Country)
                    .Property("Balance", Account?.AccountBalance).Next();
            }
            await g.Tx().CommitAsync();
            return true;
        }
        catch (Exception exception)
        {
            await g.Tx().RollbackAsync();
            return false;
        }

    }

    private async Task<bool> AddDevice(TransactionProfile TransactionProfile, Transaction Transaction, TransactionCustomer Customer)
    {
        var g = _connector.traversal();
        try
        {
            g.Tx().Begin();
            g.AddV(JanusService.DeviceNode)
                .Property("ProfileId", TransactionProfile.ProfileId)
                    .Property("DeviceId", TransactionProfile.DeviceId)
        .Property("DeviceType", TransactionProfile.DeviceType)
        .Property("IpAddress", TransactionProfile.IpAddress)
        .Property("Timestamp", DateTime.UtcNow);

            var DeviceEdgeExist = g.V().HasLabel(JanusService.CustomerNode).Has("CustomerId", Customer.CustomerId)
                .OutE("USED_DEVICE")
                .Where(__.InV().Has(JanusService.DeviceNode, "ProfileId", TransactionProfile.ProfileId))
                .HasNext();

            if (!DeviceEdgeExist)
            {
                g.V().Has(JanusService.CustomerNode, "CustomerId", Customer.CustomerId).As("c")
                    .V().Has(JanusService.DeviceNode, "ProfileId", TransactionProfile.ProfileId).AddE("USED_DEVICE")
                    .From("c").Property("CreatedAt", DateTime.UtcNow);
            }

            g.V().Has(JanusService.TransactionNode, "PlatformId", Transaction.PlatformId)
                .As("t1").V().Has(JanusService.DeviceNode, "ProfileId", TransactionProfile.ProfileId)
                .AddE("EXECUTED_ON").From("t1").Property("CreatedAt", Transaction.CreatedAt);

            await g.Tx().CommitAsync();
            return true;
        }
        catch (Exception Exception)
        {
            await g.Tx().RollbackAsync();
            return false;
        }
    }
}