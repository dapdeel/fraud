using System.Transactions;
using Api.Data;
using Api.CustomException;
using Api.Models;
using Api.Services.Interfaces;
using Gremlin.Net.Process.Traversal;
using Api.Models.Data;

public class TransferService : ITransferService
{
    IGraphService _graphService;
    private readonly ApplicationDbContext _context;

    private FrequencyCalculator _FrequencyCalculator;
    private ITransactionIngestGraphService _TransactionGraphService;
    private WeightCalculator _WeightCalculator;

    public TransferService(IGraphService graphService, ApplicationDbContext context, ITransactionIngestGraphService TransactionGraphService)
    {
        _graphService = graphService;
        _TransactionGraphService = TransactionGraphService;
        _FrequencyCalculator = new FrequencyCalculator();
        _WeightCalculator = new WeightCalculator();

        _context = context;
    }
    public async Task<Api.Models.Transaction> Ingest(TransactionTransferRequest request)
    {
        List<string> errors = ValidateTransactionTransfer(request);
        if (errors.Count > 0)
        {
            throw new ValidateErrorException(string.Join(", ", errors));
        }
        try
        {

            var DebitCustomer = await AddCustomer(request.DebitCustomer);
            var DebitAccount = await AddAccount(request.DebitCustomer.Account, DebitCustomer);

            var CreditCustomer = await AddCustomer(request.CreditCustomer);
            var CreditAccount = await AddAccount(request.CreditCustomer.Account, CreditCustomer);

            var transaction = await AddTransaction(request, DebitAccount, CreditAccount);

            var TransactionData = new TransactionIngestData
            {
                DebitCustomer = DebitCustomer,
                DebitAccount = DebitAccount,
                CreditCustomer = CreditCustomer,
                CreditAccount = CreditAccount,
                Transaction = transaction
            };
            if (request.DebitCustomer.Device != null && request.DebitCustomer.Device?.DeviceId != null)
            {
                var TransactionProfile = await AddDevice(request.DebitCustomer.Device, DebitCustomer, transaction);
                TransactionData.TransactionProfile = TransactionProfile;
            }

            var successfullyIndexed = await _TransactionGraphService.IngestTransactionInGraph(TransactionData);
            transaction.Indexed = successfullyIndexed;
            _context.Update(transaction);
            _context.SaveChanges();
            return transaction;

        }
        catch (Exception Exception)
        {
            throw new ValidateErrorException("There were issues in completing the Transaction " + Exception.Message);
        }
    }
    private async Task<bool> IngestTransactionInGraph(TransactionIngestData data)
    {
        var connector = _graphService.connect();
        var g = connector.traversal();
        try
        {
            g.Tx().Begin();
        }
        catch (Exception Exception)
        {
            return false;
        }
        finally
        {
            connector.Client().Dispose();
        }
        return true;
    }
    private async Task<TransactionCustomer> AddCustomer(CustomerRequest customerRequest)
    {
        try
        {
            var Customer = _context.TransactionCustomers
            .Where(tc => tc.Email == customerRequest.Email || tc.Phone == customerRequest.Phone)
            .FirstOrDefault();

            if (Customer == null)
            {
                Customer = new TransactionCustomer
                {
                    CustomerId = Guid.NewGuid().ToString(),
                    FullName = customerRequest.Name,
                    Email = customerRequest.Email,
                    Phone = customerRequest.Phone,
                };
                _context.TransactionCustomers.Add(Customer);
                _context.SaveChanges();
            }
            else
            {
                Customer.Phone = customerRequest.Phone;
                Customer.Email = customerRequest.Email;
                Customer.FullName = customerRequest.Name;
                _context.TransactionCustomers.Update(Customer);
                _context.SaveChanges();

            }
            return Customer;
        }
        catch (Exception Exception)
        {
            throw new ValidateErrorException("There were issues in completing the Transaction " + Exception.Message);
        }
    }
    private async Task<bool> AddAccountEdge(object? DebitAccountNode, object? CreditAccountNode, DateTime TransactionDate, float Amount)
    {
        var connector = _graphService.connect();
        var g = connector.traversal();
        if (DebitAccountNode == null || CreditAccountNode == null)
        {
            throw new ValidateErrorException("There were issues in Adding Relationship to the Transaction ");
        }
        try
        {
            g.Tx().Begin();
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
            throw new ValidateErrorException("There were issues in completing the Transaction " + Exception.Message);
        }
        finally
        {
            connector.Client().Dispose();
        }
    }
    private async Task<TransactionAccount> AddAccount(AccountRequest accountRequest, TransactionCustomer customer)
    {
        try
        {
            var Bank = _context.Banks.Where(b => b.Code == accountRequest.BankCode).First();

            var Account = _context.TransactionAccounts
            .Where(tc => tc.AccountNumber == accountRequest.AccountNumber && tc.BankId == Bank.Id)
            .FirstOrDefault();

            if (Account == null)
            {
                Account = new TransactionAccount
                {
                    AccountNumber = accountRequest.AccountNumber,
                    AccountBalance = accountRequest.Balance,
                    AccountId = Guid.NewGuid().ToString(),
                    BankId = Bank.Id,
                    AccountType = accountRequest.AccountType,
                    CustomerId = customer.Id

                };
                _context.TransactionAccounts.Add(Account);
                _context.SaveChanges();

            }
            else
            {
                Account.AccountBalance = accountRequest.Balance;
                _context.TransactionAccounts.Update(Account);
                _context.SaveChanges();
            }

            return Account;
        }
        catch (Exception Exception)
        {
            throw new ValidateErrorException(Exception.Message);
        }
    }

    private async Task<Api.Models.Transaction> AddTransaction(TransactionTransferRequest request, TransactionAccount debitAccount, TransactionAccount creditAccount)
    {
        try
        {
            var transaction = new Api.Models.Transaction
            {
                Amount = request.Transaction.Amount,
                ObservatoryId = request.ObservatoryId,
                PlatformId = Guid.NewGuid().ToString(),
                TransactionId = request.Transaction.TransactionId,
                CreditAccountId = creditAccount.Id,
                DebitAccountId = debitAccount.Id,
                Currency = request.Transaction.Currency,
                Description = request.Transaction.Description,
                TransactionType = TransactionType.Transfer,
                TransactionDate = request.Transaction.TransactionDate.ToUniversalTime(),
            };
            _context.Transactions.Add(transaction);
            _context.SaveChanges();
            //     g.AddV(JanusService.TransactionNode)
            //         .Property("Id", transaction.Id)
            //         .Property("TransactionId", transaction.TransactionId)
            //         .Property("Amount", transaction.Amount)
            //         .Property("TransactionDate", transaction.TransactionDate)
            //         .Property("Timestamp", DateTime.UtcNow)
            //         .Property("Type", TransactionType.Withdrawal)
            //         .Property("Currency", transaction.Currency)
            //         .Property("Description", transaction.Description)
            //         .Property("ObservatoryId", transaction.ObservatoryId)
            //         .Next();

            //     g.V().Has(JanusService.AccountNode, "Id", debitAccount.Id).As("A1")
            //    .V().Has(JanusService.TransactionNode, "Id", transaction.Id).AddE("SENT")
            //    .From("A1").Property("CreatedAt", transaction.CreatedAt).Next();

            //     g.V().Has(JanusService.TransactionNode, "Id", transaction.Id)
            //     .As("T1").V().Has(JanusService.AccountNode, "Id", creditAccount.Id)
            //     .AddE("RECEIVED").From("T1").Property("CreatedAt", transaction.CreatedAt).Next();

            // var DebitAccountNodeId = g.V()
            //          .HasLabel(JanusService.AccountNode)
            //         .Has("Id", debitAccount.Id).Id().Next();
            // var CreditAccountNodeId = g.V()
            //          .HasLabel(JanusService.AccountNode)
            //         .Has("Id", creditAccount.Id).Id().Next();

            // var added = await AddAccountEdge(DebitAccountNodeId, CreditAccountNodeId, transaction.TransactionDate, transaction.Amount);

            return transaction;
        }
        catch (Exception Exception)
        {
            throw new ValidateErrorException("There were issues in completing the Transaction " + Exception.Message);
        }

    }
    private async Task<Api.Models.TransactionProfile> AddDevice(DeviceRequest request, TransactionCustomer customer, Api.Models.Transaction transaction)
    {
        try
        {
            var profile = new Api.Models.TransactionProfile
            {
                CustomerId = customer.Id,
                ProfileId = Guid.NewGuid().ToString(),
                DeviceId = request.DeviceId,
                DeviceType = request.DeviceType,
                IpAddress = request.IpAddress
            };
            _context.TransactionProfiles.Add(profile);
            _context.SaveChanges();
            return profile;
        }
        catch (Exception Exception)
        {
            throw new ValidateErrorException("There were issues in completing the Transaction " + Exception.Message);
        }
    }
    public List<string> ValidateTransactionTransfer(TransactionTransferRequest request)
    {
        var errors = new List<string>();
        var debitBank = _context.Banks.Where(b => b.Code == request.DebitCustomer.Account.BankCode && b.Country == request.DebitCustomer.Account.Country).FirstOrDefault();
        if (debitBank == null)
        {
            errors.Add("Invalid Debit Account Bank Supplied");
        }
        var creditBank = _context.Banks.Where(b => b.Code == request.CreditCustomer.Account.BankCode && b.Country == request.CreditCustomer.Account.Country).FirstOrDefault();
        if (creditBank == null)
        {
            errors.Add("Invalid Credit Account Bank Supplied");
        }
        if (request.ObservatoryId <= 0)
        {
            errors.Add("Please Specify what observatory you are monitoring");
        }

        return errors;
    }
}