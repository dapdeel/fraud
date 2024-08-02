using System.Transactions;
using Api.Data;
using Api.Exception;
using Api.Models;
using Api.Services.Interfaces;
using Gremlin.Net.Process.Traversal;

public class TransferService : ITransferService
{
    IGraphService _graphService;
    private readonly ApplicationDbContext _context;
    public TransferService(IGraphService graphService, ApplicationDbContext context)
    {
        _graphService = graphService;
        _context = context;
    }
    public async Task<Api.Models.Transaction> Ingest(TransactionTransferRequest request)
    {
        List<string> errors = ValidateTransactionTransfer(request);
        if (errors.Count > 0)
        {
            throw new ValidateErrorException(string.Join(", ", errors));
        }
        var connector = _graphService.connect();
        var g = connector.traversal();
        try
        {
            g.Tx().Begin();
            var DebitCustomer = await AddCustomer(request.DebitCustomer);
            var DebitAccount = await AddAccount(request.DebitCustomer.Account, DebitCustomer);

            var DebitEdgeExists = g.V().HasLabel(JanusService.CustomerNode).Has("CustomerId", DebitCustomer.CustomerId)
                 .OutE("Owns")
                 .Where(__.InV().Has(JanusService.AccountNode, "Id", DebitAccount.Id))
                 .HasNext();

            if (!DebitEdgeExists)
            {
                g.V().HasLabel(JanusService.CustomerNode).Has("CustomerId", DebitCustomer.CustomerId)
                .As("C").V().HasLabel(JanusService.AccountNode).Has("Id", DebitAccount.Id)
                .AddE("Owns").From("C").Next();
            }

            var CreditCustomer = await AddCustomer(request.CreditCustomer);
            var CreditAccount = await AddAccount(request.CreditCustomer.Account, CreditCustomer);

            var CreditEdgeExists = g.V().HasLabel(JanusService.CustomerNode).Has("CustomerId", CreditCustomer.CustomerId)
                .OutE("Owns")
                .Where(__.InV().Has(JanusService.AccountNode, "Id", CreditAccount.Id))
                .HasNext();

            if (!CreditEdgeExists)
            {
                g.V().HasLabel(JanusService.CustomerNode).Has("CustomerId", CreditCustomer.CustomerId)
                .As("C").V().HasLabel(JanusService.AccountNode).Has("Id", CreditAccount.Id)
                .AddE("Owns").From("C").Next();
            }
            await g.Tx().CommitAsync();

            var transaction = await AddTransaction(request, DebitAccount, CreditAccount);
            if (request.DebitCustomer.Device != null && request.DebitCustomer.Device?.DeviceId != null)
            {
                await AddDevice(request.DebitCustomer.Device, DebitCustomer, transaction);
            }

        }
        catch (Exception Exception)
        {
            throw new ValidateErrorException("Unable to complete transactions " + Exception.Message);
        }
        finally
        {
            connector.Client().Dispose();
        }

        return null;
    }
    private async Task<TransactionCustomer> AddCustomer(CustomerRequest customerRequest)
    {
        var connector = _graphService.connect();
        var g = connector.traversal();

        try
        {
            g.Tx().Begin();
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
                g.AddV(JanusService.CustomerNode)
                .Property("CustomerId", Customer.CustomerId)
                .Property("Name", Customer.FullName)
                .Property("Phone", Customer.Phone)
                .Property("Email", Customer.Email).Next();

            }
            else
            {
                Customer.Phone = customerRequest.Phone;
                Customer.Email = customerRequest.Email;
                Customer.FullName = customerRequest.Name;
                _context.TransactionCustomers.Update(Customer);
                _context.SaveChanges();
                var customerGraphId = g.V()
                .HasLabel(JanusService.CustomerNode)
                .Has("CustomerId", Customer.CustomerId).Id();
                if (customerGraphId.HasNext())
                {
                    g.V(customerGraphId.Next()).Property("Phone", Customer.Phone)
                    .Property("Email", Customer.Email)
                    .Property("Name", Customer.FullName);
                }

            }
            await g.Tx().CommitAsync();
            return Customer;
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
        var connector = _graphService.connect();
        var g = connector.traversal();
        try
        {
            g.Tx().Begin();
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
                    BankId = Bank.Id,
                    AccountType = accountRequest.AccountType,
                    CustomerId = customer.Id

                };
                _context.TransactionAccounts.Add(Account);
                _context.SaveChanges();

                g.AddV(JanusService.AccountNode)
                .Property("Id", Account.Id)
                .Property("AccountNumber", Account.AccountNumber)
                .Property("BankCode", Account?.Bank?.Code)
                .Property("Country", Account?.Bank?.Country)
                .Property("Balance", Account?.AccountBalance).Next();

            }
            else
            {
                Account.AccountBalance = accountRequest.Balance;
                _context.TransactionAccounts.Update(Account);
                _context.SaveChanges();
                var accountGraphId = g.V()
                     .HasLabel(JanusService.AccountNode)
                    .Has("Id", Account.Id).Id().Next();

                g.V(accountGraphId).Property("Balance", Account.AccountBalance);
                await g.Tx().CommitAsync();
            }

            return Account;
        }
        catch (Exception Exception)
        {
            await g.Tx().RollbackAsync();
            throw new ValidateErrorException(Exception.Message);
        }
        finally
        {
            connector.Client().Dispose();
        }
    }

    private async Task<Api.Models.Transaction> AddTransaction(TransactionTransferRequest request, TransactionAccount debitAccount, TransactionAccount creditAccount)
    {

        var connector = _graphService.connect();
        var g = connector.traversal();
        try
        {
            var transaction = new Api.Models.Transaction
            {
                Amount = request.Transaction.Amount,
                ObservatoryId = request.ObservatoryId,
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
            g.AddV(JanusService.TransactionNode)
                .Property("Id", transaction.Id)
                .Property("TransactionId", transaction.TransactionId)
                .Property("Amount", transaction.Amount)
                .Property("TransactionDate", transaction.TransactionDate)
                .Property("Timestamp", DateTime.UtcNow)
                .Property("Type", TransactionType.Withdrawal)
                .Property("Currency", transaction.Currency)
                .Property("Description", transaction.Description)
                .Property("ObservatoryId", transaction.ObservatoryId)
                .Next();
            g.Tx().Begin();
            g.V().Has(JanusService.AccountNode, "Id", debitAccount.Id).As("A1")
           .V().Has(JanusService.TransactionNode, "Id", transaction.Id).AddE("SENT")
           .From("A1").Property("CreatedAt", transaction.CreatedAt).Next();

            g.V().Has(JanusService.TransactionNode, "Id", transaction.Id)
            .As("T1").V().Has(JanusService.AccountNode, "Id", creditAccount.Id)
            .AddE("RECEIVED").From("T1").Property("CreatedAt", transaction.CreatedAt).Next();
            await g.Tx().CommitAsync();
            transaction.Indexed = true;
            _context.Update(transaction);
            _context.SaveChanges();
            return transaction;
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
    private async Task<Api.Models.TransactionProfile> AddDevice(DeviceRequest request, TransactionCustomer customer, Api.Models.Transaction transaction)
    {
        var connector = _graphService.connect();
        var g = connector.traversal();
        try
        {
            g.Tx().Begin();
            var profile = new Api.Models.TransactionProfile
            {
                CustomerId = customer.Id,
                DeviceId = request.DeviceId,
                DeviceType = request.DeviceType,
                IpAddress = request.IpAddress
            };
            _context.TransactionProfiles.Add(profile);
            g.AddV(JanusService.DeviceNode)
                .Property("Id", profile.Id)
                .Property("DeviceId", profile.DeviceId)
                .Property("DeviceType", profile.DeviceType)
                .Property("IpAddress", profile.IpAddress)
                .Property("Timestamp", DateTime.UtcNow)
                .Next();

            var DeviceEdgeExist = g.V().HasLabel(JanusService.CustomerNode).Has("CustomerId", customer.CustomerId)
                .OutE("USED_DEVICE")
                .Where(__.InV().Has(JanusService.DeviceNode, "Id", profile.Id))
                .HasNext();

            if (!DeviceEdgeExist)
            {
                g.V().Has(JanusService.CustomerNode, "CustomerId", customer.CustomerId).As("c")
                    .V().Has(JanusService.DeviceNode, "Id", profile.Id).AddE("USED_DEVICE")
                    .From("c").Property("CreatedAt", DateTime.UtcNow).Next();
            }

            g.V().Has(JanusService.TransactionNode, "Id", transaction.Id)
                .As("t1").V().Has(JanusService.DeviceNode, "Id", profile.Id)
                .AddE("EXECUTED_ON").From("t1").Property("CreatedAt", transaction.CreatedAt)
                .Next();

            await g.Tx().CommitAsync();
            _context.SaveChanges();
            return profile;
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