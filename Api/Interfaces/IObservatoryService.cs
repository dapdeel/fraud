using Api.DTOs;
using Api.Models;

namespace Api.Services.Interfaces;
public interface IObservatoryService : IBaseService{

    public abstract  Task<Observatory?> Add(ObservatoryRequest request, string UserId);
    Task AcceptInvite(int userObservatoryId);

    Task Invite(InvitationRequest request);


}