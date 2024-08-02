using Api.DTOs;
using Api.Entity;
using Api.Models;

namespace Api.Services.Interfaces
{
    public interface IObservatoryService : IBaseService
    {
        Task<Observatory?> Add(ObservatoryRequest request, string userId);
        Task<Observatory?> Get(int id, string userId);
        Task AcceptInvite(int userObservatoryId, string userId);
        Task RejectInvite(int userObservatoryId, string userId);
        Task Invite(InvitationRequest request);


        Task<UserObservatoryStatus> CheckUserObservatoryStatus(string userId);
    }
}
