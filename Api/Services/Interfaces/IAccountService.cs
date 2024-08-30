using Api.DTOs;
using Api.Models;

namespace Api.Interfaces
{
    public interface IAccountService
    {
        AccountDocument? GetByAccountNumberAndBankId(string AccountNumber, int bankId, Observatory observatory);
      
    }
}
