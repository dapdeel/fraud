using Api.Data;
using Api.DTOs;
using Api.CustomException;
using Api.Models;
using Api.Models.Responses;
using Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Api.Interfaces;
using Microsoft.AspNetCore.Identity;
using Api.Entity;
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
            ObservatoryTag = Guid.NewGuid().ToString(),
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
            ObservatoryTag = Guid.NewGuid().ToString(),
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
            ObservatoryTag = Guid.NewGuid().ToString(),
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

    public async Task<List<Observatory>> GetInvitedObservatoriesByUserId(string userId)
    {
        var invitedObservatories = await _context.UserObservatories
            .Where(uo => uo.UserId == userId && uo.Status == Status.Invited)
            .OrderByDescending(uo => uo.CreatedAt)
            .Select(uo => uo.Observatory)
            .ToListAsync();

        return invitedObservatories;
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

    public async Task<Observatory?> Get(string id, string userId)
    {
        /*var userObservatory = _context.UserObservatories
            .Where(uo => uo.ObservatoryTag == id && uo.UserId == userId && uo.Status == Status.Member)
            .FirstOrDefault();
        if (userObservatory == null)
        {
            throw new ValidateErrorException("You are not a member of this observatory.");
        }*/
        var observatory = await _context.Observatories
            .Include(o => o.TransactionRules)
            .FirstOrDefaultAsync(o => o.ObservatoryTag == id);
        return observatory;
    }


    public async Task<List<Observatory>> GetObservatoriesByUserId(string userId)
    {
        var observatories = await _context.UserObservatories
            .Where(uo => uo.UserId == userId && uo.Status == Status.Member)
            .Select(uo => uo.Observatory)
            .ToListAsync();

        return observatories;
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


    public async Task<Observatory> SwitchCurrentObservatory(string userId, int observatoryId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new ValidateErrorException("Could not find user");
        }

        var observatory = await _context.Observatories.FindAsync(observatoryId);
        if (observatory == null)
        {
            throw new ValidateErrorException("Observatory not found");
        }

        var userObservatory = await _context.UserObservatories
            .FirstOrDefaultAsync(uo => uo.UserId == userId && uo.ObservatoryId == observatoryId);
        if (userObservatory == null)
        {
            throw new ValidateErrorException("User is not associated with this observatory");
        }

        user.CurrentObservatoryId = observatoryId;
        await _context.SaveChangesAsync();

        return observatory; 
    }




    public async Task<Observatory?> GetCurrentObservatory(string userId)
    {

        var user = await _context.Users.FindAsync(userId);

        if (user == null || user.CurrentObservatoryId == null)
        {
            throw new ValidateErrorException("User or current observatory not found");
        }

        var observatory = await _context.Observatories
            .FirstOrDefaultAsync(o => o.Id == user.CurrentObservatoryId);

        return observatory;
    }

    public async Task<TransactionRules?> GetTransactionRules(int observatoryId)
    {
        var observatory = await _context.Observatories
            .Include(o => o.TransactionRules)
            .FirstOrDefaultAsync(o => o.Id == observatoryId);

        if (observatory == null)
        {
            throw new ValidateErrorException("Observatory not found.");
        }

        if (observatory.TransactionRules == null)
        {
            var defaultRules = new TransactionRules
            {
                ObservatoryId = observatoryId,
                AlertFrequencyMinutes = 30,
                RiskAppetiteAmount = 100000,
                AllowSuspiciousAccounts = true,
                BlockFraudulentAccounts = false,
                AlertFraudulentAccounts = true,
                AlertHighRiskTransactions = true
            };

            _context.TransactionRules.Add(defaultRules);
            await _context.SaveChangesAsync();

            return defaultRules;
        }

        return observatory.TransactionRules;
    }


    public async Task UpdateTransactionRules(int observatoryId, TransactionRules rulesDto)
    {
        var observatory = await _context.Observatories
            .Include(o => o.TransactionRules)
            .FirstOrDefaultAsync(o => o.Id == observatoryId);

        if (observatory == null)
        {
            throw new ValidateErrorException("Observatory not found.");
        }

        var rules = observatory.TransactionRules ?? new TransactionRules { ObservatoryId = observatoryId };

        rules.AlertFrequencyMinutes = rulesDto.AlertFrequencyMinutes;
        rules.RiskAppetiteAmount = rulesDto.RiskAppetiteAmount;
        rules.AllowSuspiciousAccounts = rulesDto.AllowSuspiciousAccounts;
        rules.BlockFraudulentAccounts = rulesDto.BlockFraudulentAccounts;
        rules.AlertFraudulentAccounts = rulesDto.AlertFraudulentAccounts;
        rules.AlertHighRiskTransactions = rulesDto.AlertHighRiskTransactions;

        if (observatory.TransactionRules == null)
        {
            _context.TransactionRules.Add(rules);
        }
        else
        {
            _context.TransactionRules.Update(rules);
        }

        await _context.SaveChangesAsync();
    }






}
