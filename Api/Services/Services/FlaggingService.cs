using Api.CustomException;
using Api.Data;
using Api.DTOs;
using Api.Entity;
using Api.Interfaces;
using Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Api.Services.Services
{
    public class FlaggingService : IFlaggingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IElasticSearchService _elasticSearchService;
        private readonly IAccountService _accountService;
        private readonly ITransactionTracingGraphService _transactionTracingGraphService;

        public FlaggingService(
            ApplicationDbContext context,
            IElasticSearchService elasticSearchService,
            IAccountService accountService,
            ITransactionTracingGraphService transactionTracingGraphService
        )
        {
            _context = context;
            _elasticSearchService = elasticSearchService;
            _accountService = accountService;
            _transactionTracingGraphService = transactionTracingGraphService;
        }

        public async Task<bool> MarkTransactionAsSuspicious(string observatoryTag, string transactionId)
        {
            var transaction = _transactionTracingGraphService.GetTransactionToFlag(observatoryTag, transactionId);

            var alreadyFlagged = _context.SuspiciousTransactions
                .Any(t => t.TransactionId == transactionId && t.ObservatoryTag == observatoryTag);

            if (alreadyFlagged)
            {
                throw new ValidateErrorException("Transaction is flagged as suspicious.");
            }
            var blacklistedTransaction = new BlacklistedTransaction
            {
                TransactionId = transaction.TransactionId,
                PlatformId = transaction.PlatformId,
                ObservatoryTag = transaction.observatoryTag,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                Description = transaction.Description,
                TransactionDate = transaction.TransactionDate,
                DebitAccountId = transaction.DebitAccountId,
                CreditAccountId = transaction.CreditAccountId,
                DeviceDocumentId = transaction.DeviceDocumentId,
                ObservatoryId = transaction.ObservatoryId,
                FlaggedDate = DateTime.UtcNow, 
            };

            _context.BlacklistedTransactions.Add(blacklistedTransaction);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while flagging the transaction.", ex);
            }

            return true;
        }



        public async Task<bool> MarkTransactionAsBlacklisted(string observatoryTag, string transactionId)
        {
            var transaction = _transactionTracingGraphService.GetTransactionToFlag(observatoryTag, transactionId);

            var alreadyFlagged = _context.BlacklistedTransactions
                .Any(t => t.TransactionId == transactionId && t.ObservatoryTag == observatoryTag);

            if (alreadyFlagged)
            {
                throw new ValidateErrorException("Transaction is already blacklisted.");
            }
            var blacklistedTransaction = new BlacklistedTransaction
            {
                TransactionId = transaction.TransactionId,
                PlatformId = transaction.PlatformId,
                ObservatoryTag = transaction.observatoryTag,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                Description = transaction.Description,
                TransactionDate = transaction.TransactionDate,
                DebitAccountId = transaction.DebitAccountId,
                CreditAccountId = transaction.CreditAccountId,
                DeviceDocumentId = transaction.DeviceDocumentId,
                ObservatoryId = transaction.ObservatoryId,
                FlaggedDate = DateTime.UtcNow,
            };

            _context.BlacklistedTransactions.Add(blacklistedTransaction);
            await _context.SaveChangesAsync();

            return true;
        }


        public async Task<bool> MarkAccountAsBlacklistedAsync(string accountNumber, int bankId)
        {
            var account = _accountService.GetAccountDetails(accountNumber, bankId);

            if (account == null)
            {
                throw new ValidateErrorException("This account does not exist, kindly try again.");
            }
            var alreadyFlagged = _context.BlacklistedAccounts
                .Any(a => a.AccountNumber == accountNumber && a.BankId == bankId);

            if (alreadyFlagged)
            {
                throw new ValidateErrorException("Account is already blacklisted.");
            }

            var blacklistedAccount = new BlacklistedAccount
            {
                AccountNumber = account.AccountNumber,
                AccountId = account.AccountId,
                ObservatoryTag= account.ObservatoryTag,
                AccountBalance = account.AccountBalance,
                FullName = account.FullName,
                Phone = account.Phone,
                Email = account.Email,
                BankId = account.BankId,
                FlaggedDate = DateTime.UtcNow,
            };

            await _context.BlacklistedAccounts.AddAsync(blacklistedAccount);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> MarkAccountAsSuspicious(string accountNumber, int bankId)
        {
            var account = _accountService.GetAccountDetails(accountNumber, bankId);

            if (account == null)
            {
                throw new ValidateErrorException("This account does not exist, kindly try again.");
            }
            var alreadyFlagged = _context.SuspiciousAccounts
                .Any(a => a.AccountNumber == accountNumber && a.BankId == bankId);

            if (alreadyFlagged)
            {
                throw new ValidateErrorException("Account is already flagged as suspicious.");
            }

            var suspiciousAccount = new SuspiciousAccount
            {
                AccountNumber = account.AccountNumber,
                AccountId = account.AccountId,
                ObservatoryTag = account.ObservatoryTag,
                AccountBalance = account.AccountBalance,
                FullName = account.FullName,
                Phone = account.Phone,
                Email = account.Email,
                BankId = account.BankId,
                FlaggedDate = DateTime.UtcNow,
            };

            _context.SuspiciousAccounts.Add(suspiciousAccount);
            await _context.SaveChangesAsync();

            return true;
        }

        public List<BlacklistedTransaction> GetBlacklistedTransactions(string observatoryTag)
        {
            try
            {
                var blacklistedTransactions = _context.BlacklistedTransactions
                    .Where(t => t.ObservatoryTag == observatoryTag)
                    .ToList();

                return blacklistedTransactions;
            }
            catch (Exception ex)
            {
                throw new ValidateErrorException($"An error occurred while retrieving blacklisted transactions: {ex.Message}");
            }
        }

        public List<SuspiciousTransaction> GetSuspiciousTransactions(string observatoryTag)
        {
            try
            {
                var suspiciousTransactions = _context.SuspiciousTransactions
                    .Where(t => t.ObservatoryTag == observatoryTag)
                    .ToList();

                return suspiciousTransactions;
            }
            catch (Exception ex)
            {
                throw new ValidateErrorException($"An error occurred while retrieving suspicious transactions: {ex.Message}");
            }
        }

        public List<BlacklistedAccount> GetBlacklistedAccounts(string observatoryTag)
        {
            try
            {
                var blacklistedAccounts = _context.BlacklistedAccounts
                    .Where(a => a.ObservatoryTag == observatoryTag)
                    .ToList();

                return blacklistedAccounts;
            }
            catch (Exception ex)
            {
                throw new ValidateErrorException($"An error occurred while retrieving blacklisted accounts: {ex.Message}");
            }
        }

        public List<SuspiciousAccount> GetSuspiciousAccounts(string observatoryTag)
        {
            try
            {
                var suspiciousAccounts = _context.SuspiciousAccounts
                    .Where(a => a.ObservatoryTag == observatoryTag)
                    .ToList();

                return suspiciousAccounts;
            }
            catch (Exception ex)
            {
                throw new ValidateErrorException($"An error occurred while retrieving suspicious accounts: {ex.Message}");
            }
        }


    }



}
