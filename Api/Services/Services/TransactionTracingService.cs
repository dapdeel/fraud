using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services.TransactionTracing
{
    public class TransactionService : ITransactionTracingService
    {
        private readonly ApplicationDbContext _context;
        private ITransactionTracingGraphService _transactionTracingGraphService;

        public TransactionService(ApplicationDbContext context, ITransactionTracingGraphService TransactionTracingGraphService)
        {
            _context = context;
            _transactionTracingGraphService = TransactionTracingGraphService;
        }

        public async Task<int> GetTotalTransactionsLast30Days(string accountNumber)
        {
            return await _context.Transactions
                .CountAsync(t => (t.DebitAccount.AccountNumber == accountNumber || t.CreditAccount.AccountNumber == accountNumber) &&
                                 t.TransactionDate >= DateTime.UtcNow.AddDays(-30));
        }

        public async Task<decimal> GetTotalAmountLast30Days(string accountNumber)
        {
            return await _context.Transactions
                .Where(t => (t.DebitAccount.AccountNumber == accountNumber || t.CreditAccount.AccountNumber == accountNumber) &&
                             t.TransactionDate >= DateTime.UtcNow.AddDays(-30))
                .SumAsync(t => (decimal)t.Amount);
        }

        public async Task<int> GetTransactionsLastHour(string accountNumber)
        {
            return await _context.Transactions
                .CountAsync(t => (t.DebitAccount.AccountNumber == accountNumber || t.CreditAccount.AccountNumber == accountNumber) &&
                                 t.TransactionDate >= DateTime.UtcNow.AddHours(-1));
        }

        public async Task<List<Api.Models.Transaction>> GetAllTransactions(string accountNumber)
        {
            return await _context.Transactions
                .Where(t => t.DebitAccount.AccountNumber == accountNumber || t.CreditAccount.AccountNumber == accountNumber)
                .ToListAsync();
        }

        public async Task<List<string>> GetSentAccounts(string accountNumber, DateTime startDate, DateTime endDate)
        {
            var startDateUtc = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            var endDateUtc = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            return await _context.Transactions
                .Where(t => t.DebitAccount.AccountNumber == accountNumber &&
                            t.TransactionDate >= startDateUtc && t.TransactionDate <= endDateUtc)
                .Select(t => t.CreditAccount.AccountNumber)
                .Distinct()
                .ToListAsync();
        }


        public async Task<Api.Models.Transaction> GetTransactionById(int transactionId)
        {
            return await _context.Transactions.FindAsync(transactionId);
        }
        public TransactionGraphDetails GetTransactionById(int observatoryId, string transactionId)
        {
            var response = _transactionTracingGraphService.GetTransaction(observatoryId, transactionId);
            return response;
        }

        public async Task<List<Api.Models.Transaction>> GetTransactionsByCustomerId(string customerId)
        {
            return await _context.Transactions
                .Where(t => t.DebitAccount.TransactionCustomer.CustomerId == customerId ||
                             t.CreditAccount.TransactionCustomer.CustomerId == customerId)
                .ToListAsync();
        }



    }
}
