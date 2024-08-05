using Api.Data;
using Api.DTOs;
using Api.Exception;
using Api.Interfaces;
using Api.Models;
using Api.Models.Responses;
using Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
public class ObservatoryService : IObservatoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;

    public ObservatoryService(ApplicationDbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    public async Task<Observatory?> Add(ObservatoryRequest request, string userId)
    {
        var errors = ValidateObservatory(request);
        if (errors.Count > 0)
        {
            throw new ValidateErrorException(string.Join(", ", errors));
        }
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new ValidateErrorException("Could not Find User");
        }
        Observatory observatory = new Observatory
        {
            BankId = request.BankId,
            Currency = request.Currency,
            FrequencyCount = request.FrequencyCount,
            FrequencyTimer = request.FrequencyTimer,
            IsSetup = false,
            UseDefault = request.UseDefault,
            Live = false,
            Name = request.Name,
            RiskAmount = request.RiskAmount,
            OddHourStartTime = request.OddHourStartTime,
            OddHourStopTime = request.OddHourStopTime
        };

        _context.Observatories.Add(observatory);
        await _context.SaveChangesAsync();
        UserObservatory userObservatory = new UserObservatory
        {
            ObservatoryId = observatory.Id,
            UserId = user.Id,
            Role = Role.Admin,
            Status = Status.Member,
        };
        _context.UserObservatories.Add(userObservatory);
        await _context.SaveChangesAsync();
        return observatory;
    }



    public async Task Invite(InvitationRequest request, string inviterUserId)
    {
    
        await EnsureUserIsAdmin(inviterUserId, request.ObservatoryId);
        await _authService.EnsureUserExists(request.UserId);
        await EnsureObservatoryExists(request.ObservatoryId);
        await EnsureUserNotAlreadyInvited(request.UserId, request.ObservatoryId);

        UserObservatory userObservatory = new UserObservatory
        {
            ObservatoryId = request.ObservatoryId,
            UserId = request.UserId,
            Role = Role.Member,
            Status = Status.Invited,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserObservatories.Add(userObservatory);
        await _context.SaveChangesAsync();
    }



    public async Task AcceptInvite(int observatoryId, string userId)
    {
              var userObservatory = await _context.UserObservatories
            .Include(uo => uo.Observatory)
            .FirstOrDefaultAsync(uo => uo.ObservatoryId == observatoryId && uo.UserId == userId);

        if (userObservatory == null)
        {
            throw new ValidateErrorException("Invitation not found or you do not have permission to accept this invitation.");
        }
        if (userObservatory.Status == Status.Member)
        {
            throw new ValidateErrorException("User is already a member of the observatory.");
        }

        userObservatory.Status = Status.Member;
        userObservatory.UpdatedAt = DateTime.UtcNow;

        _context.UserObservatories.Update(userObservatory);
        await _context.SaveChangesAsync();
    }

    public async Task RejectInvite(int userObservatoryId, string userId)
    {
        var userObservatory = await _context.UserObservatories
            .FirstOrDefaultAsync(uo => uo.Id == userObservatoryId && uo.UserId == userId);

        if (userObservatory == null)
        {
            throw new ValidateErrorException("Invitation not found or you do not have permission to reject this invitation.");
        }


        userObservatory.Status = Status.Rejected;
        userObservatory.UpdatedAt = DateTime.UtcNow;

        _context.UserObservatories.Update(userObservatory);
        await _context.SaveChangesAsync();
    }



    private List<string> ValidateObservatory(ObservatoryRequest request)
    {
        var errors = new List<string>();
        var bank = _context.Banks.Where(b => b.Id == request.BankId).Count();
        if (bank <= 0)
        {
            errors.Add("Invalid bank");
        }
        return errors;
    }

    public async Task<UserObservatoryStatus> CheckUserObservatoryStatus(string userId)
    {
     
        var userObservatories = await _context.UserObservatories
            .Where(uo => uo.UserId == userId)
            .Include(uo => uo.Observatory)
            .ToListAsync();

        var isMember = userObservatories.Any(uo => uo.Status == Status.Member);
        var pendingInvites = userObservatories
            .Where(uo => uo.Status == Status.Invited)
            .Select(uo => new InvitationResponse
            {
                ObservatoryId = uo.ObservatoryId,
                ObservatoryName = uo.Observatory.Name,
                InvitedAt = uo.CreatedAt
            })
            .ToList();

        var status = (isMember || pendingInvites.Any()) ? 1 : 0;

        return new UserObservatoryStatus
        {
            Status = status,
            PendingInvites = pendingInvites.Any() ? pendingInvites : null
        };
    }


    public List<string> Errors()
    {
        throw new NotImplementedException();
    }

    public async Task<Observatory?> Get(int id, string userId)
    {
        var UserObservatory = _context.UserObservatories
        .Where(uo => uo.ObservatoryId == id && uo.UserId == userId && uo.Status == Status.Member)
        .FirstOrDefault();
        var Observatory = await _context.Observatories.FindAsync(id);
        return Observatory;

    }



    // Helper Methods

    private async Task EnsureUserIsAdmin(string userId, int observatoryId)
    {
        var userObservatory = await _context.UserObservatories
            .FirstOrDefaultAsync(uo => uo.ObservatoryId == observatoryId && uo.UserId == userId);

        if (userObservatory == null)
        {
            throw new ValidateErrorException("You are not a member of this observatory.");
        }

        if (userObservatory.Role != Role.Admin)
        {
            throw new ValidateErrorException("You are not authorized to invite users to this observatory. Only admins can invite users.");
        }
    }

  

    private async Task EnsureObservatoryExists(int observatoryId)
    {
        var observatory = await _context.Observatories.FindAsync(observatoryId);
        if (observatory == null)
        {
            throw new ValidateErrorException("Observatory not found");
        }
    }

    private async Task EnsureUserNotAlreadyInvited(string userId, int observatoryId)
    {
        var existingUserObservatory = await _context.UserObservatories
            .FirstOrDefaultAsync(uo => uo.ObservatoryId == observatoryId && uo.UserId == userId);

        if (existingUserObservatory == null)
        {
            return;
        }

        if (existingUserObservatory.Status == Status.Member)
        {
            throw new ValidateErrorException("User is already a member of the observatory.");
        }

        if (existingUserObservatory.Status == Status.Invited)
        {
            throw new ValidateErrorException("User is already invited to the observatory.");
        }
    }


}
