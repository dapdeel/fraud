using Api.DTOs;
using Api.Entity;
using Api.Models;
using Api.Models.Responses;

namespace Api.Services.Interfaces
{
    public interface IObservatoryService : IBaseService
    {
        Task<Observatory?> Add(ObservatoryRequest request, string userId);
        Task<Observatory?> Get(int id, string userId);


        Task AcceptInvite(int userObservatoryId, string userId);
        Task RejectInvite(int userObservatoryId, string userId);
        Task Invite(InvitationRequest request, string inviterUserId);
        Task<UserObservatoryStatus> CheckUserObservatoryStatus(string userId);
        Task<List<Observatory>> GetObservatoriesByUserId( string userId);
        Task<List<Observatory>> GetInvitedObservatoriesByUserId(string userId);
        Task<Observatory> SwitchCurrentObservatory(string userId, int observatoryId);
        Task<Observatory?> GetCurrentObservatory(string userId);
        Task UpdateTransactionRules(int observatoryId, TransactionRules rulesDto);
        Task<TransactionRules?> GetTransactionRules(int observatoryId);
    }
}
