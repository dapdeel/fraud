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
        g.AddV(JanusService.CustomerNode)
            .Property("CustomerId", request.DebitCustomer.Email)
            .Property("Name", request.DebitCustomer.Name)
            .Property("Phone", request.DebitCustomer.Phone);

        g.AddV(JanusService.AccountNode)
        .Property("AccountNumber", request.DebitCustomer.Account.AccountNumber)
        .Property("BankCode", request.DebitCustomer.Account.BankCode)
        .Property("Country", request.DebitCustomer.Account.Country)
        .Property("Balance", request.DebitCustomer.Account.Balance);



        throw new NotImplementedException();
    }
    private TransactionCustomer AddCustomer(CustomerRequest customerRequest)
    {
        var Customer = _context.TransactionCustomers
        .Where(tc => tc.Email == customerRequest.Email || tc.Phone == customerRequest.Phone)
        .FirstOrDefault();
        if (Customer == null)
        {               // Customer = new TransactionCustomer{

            // };
        }
        return Customer;
    }
    public List<string> ValidateTransactionTransfer(TransactionTransferRequest request)
    {
        var errors = new List<string>();

        return errors;
    }
}