using Api.DTOs;
using Api.Models;

namespace Api.Interfaces
{
    public interface IAccountService
    {
        AccountDocument? GetByAccountNumberAndBankId(string AccountNumber, int bankId, Observatory observatory);

        AccountWithDetailsDto? GetAccountDetails(string AccountNumber, int BankId);
        List<AccountWithDetailsDto> GetAccountsByPage(int pageNumber, int batch);
        long GetAccountCount();
        List<AccountRelationshipResult> GetAccountRelationshipScore(string creditAccountId, string debitAccountId);



    }
}
