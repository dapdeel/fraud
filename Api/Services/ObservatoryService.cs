using Api.Data;
using Api.DTOs;
using Api.Exception;
using Api.Models;
using Api.Services.Interfaces;
public class ObservatoryService : IObservatoryService
{
    private readonly ApplicationDbContext _context;

    public ObservatoryService(ApplicationDbContext context)
    {
        _context = context;
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

    public async Task Invite(InvitationRequest request)
    {
        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
        {
            throw new CustomServiceException("User not found");
        }

        var observatory = await _context.Observatories.FindAsync(request.ObservatoryId);
        if (observatory == null)
        {
            throw new CustomServiceException("Observatory not found");
        }

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

    public async Task AcceptInvite(int userObservatoryId)
    {
        var userObservatory = await _context.UserObservatories.FindAsync(userObservatoryId);
        if (userObservatory == null)
        {
            throw new CustomServiceException("Invitation not found");
        }

        if (userObservatory.Status == Status.Member)
        {
            throw new CustomServiceException("User is already a member");
        }

        userObservatory.Status = Status.Member;
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

    public List<string> Errors()
    {
        throw new NotImplementedException();
    }

    public async Task<Observatory?> Get(int id, string userId)
    {
        var UserObservatory = _context.UserObservatories
        .Where(uo => uo.ObservatoryId == id && uo.UserId == userId && uo.Status == Status.Member)
        .FirstOrDefault();
        if (UserObservatory == null)
        {
            throw new ValidateErrorException("You do not have access to this Observatory, please contact your admin");
        }
        var Observatory = await _context.Observatories.FindAsync(id);
        return Observatory;

    }
}
