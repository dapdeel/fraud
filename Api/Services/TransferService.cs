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
        var DebitCustomer = AddCustomer(request.DebitCustomer);
        var DebitAccount = AddAccount(request.DebitCustomer.Account, DebitCustomer);

        var CreditCustomer = AddCustomer(request.CreditCustomer);
        var CreditAccount = AddAccount(request.CreditCustomer.Account, CreditCustomer);

        
        throw new NotImplementedException();
    }
    private TransactionCustomer AddCustomer(CustomerRequest customerRequest)
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
            var g = _graphService.connect();

            g.AddV(JanusService.CustomerNode)
            .Property("CustomerId", Customer.CustomerId)
            .Property("Name", Customer.FullName)
            .Property("Phone", Customer.Phone)
            .Property("Email", Customer.Email);

        }
        Customer.Phone = customerRequest.Phone;
        Customer.Email = customerRequest.Email;
        Customer.FullName = customerRequest.Name;
        _context.TransactionCustomers.Update(Customer);
        _context.SaveChanges();
        return Customer;
    }
    private TransactionAccount AddAccount(AccountRequest accountRequest, TransactionCustomer customer)
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
                BankId = Bank.Id,
                AccountType = accountRequest.AccountType,
                CustomerId = customer.Id

            };
            _context.TransactionAccounts.Add(Account);
            var g = _graphService.connect();

            g.AddV(JanusService.AccountNode)
            .Property("AccountNumber", Account.AccountNumber)
            .Property("BankCode", Account?.Bank?.Code)
            .Property("Country", Account?.Bank?.Country)
            .Property("Balance", Account.AccountBalance);

        }
        Account.AccountBalance = accountRequest.Balance;
        _context.TransactionAccounts.Update(Account);
        _context.SaveChanges();
        return Account;
    }
    public List<string> ValidateTransactionTransfer(TransactionTransferRequest request)
    {
        var errors = new List<string>();

        return errors;
    }
}