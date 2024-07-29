using System.Transactions;
using Api.Data;
using Api.Exception;
using Api.Models;
using Api.Services.Interfaces;

public class TransferService : ITransferService
{
    IGraphService _graphService;
    ApplicationDbContext _context;
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
        var g = _graphService.connect();
        try
        {
            g.Tx().Begin();
            var DebitCustomer = await AddCustomer(request.DebitCustomer);
            var DebitAccount = await AddAccount(request.DebitCustomer.Account, DebitCustomer);


            g.V().HasLabel(JanusService.CustomerNode).Has("CustomerId", DebitCustomer.CustomerId)
            .As("C").V().HasLabel(JanusService.AccountNode).Has("Id", DebitAccount.Id)
            .AddE("Owns").From("C").Next();


            var CreditCustomer = await AddCustomer(request.CreditCustomer);
            var CreditAccount = await AddAccount(request.CreditCustomer.Account, CreditCustomer);
            g.V().HasLabel(JanusService.CustomerNode).Has("CustomerId", CreditCustomer.CustomerId)
            .As("C").V().HasLabel(JanusService.AccountNode).Has("Id", CreditAccount.Id)
            .AddE("Owns").From("C").Next();

            await g.Tx().CommitAsync();
        }
        catch (Exception Exception)
        {
            throw new ValidateErrorException("Unable to complete transactions " + Exception.Message);
            g.Tx().Begin();
        }

        return null;
    }
    private async Task<TransactionCustomer> AddCustomer(CustomerRequest customerRequest)
    {
        var g = _graphService.connect();
        g.Tx().Begin();
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
                    g.V(customerGraphId.Next()).Property("Phone", Customer.Phone)
               .Property("Email", Customer.Email)
               .Property("Name", Customer.FullName);

            }
            await g.Tx().CommitAsync();
            return Customer;
        }
        catch (Exception Exception)
        {
            await g.Tx().RollbackAsync();
            throw new ValidateErrorException("There were issues in completing the Transaction " + Exception.Message);
        }
    }
    private async Task<TransactionAccount> AddAccount(AccountRequest accountRequest, TransactionCustomer customer)
    {
        var g = _graphService.connect();
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
    }
    public List<string> ValidateTransactionTransfer(TransactionTransferRequest request)
    {
        var errors = new List<string>();

        return errors;
    }
}