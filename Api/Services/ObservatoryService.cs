using Api.Data;
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

    public async Task<Observatory?> Add(ObservatoryRequest request, string UserId)
    {
        var errors = ValidateObservatory(request);
        if (errors.Count > 0)
        {
            throw new CustomServiceException(string.Join(", ", errors));
        }
        var User = await _context.Users.FindAsync(UserId);
        if (User == null)
        {
            throw new CustomServiceException("Could not Find User");
        }
        Observatory Observatory = new Observatory
        {
            BankId = request.BankId,
            Currency = request.Currency,
            FrequencyCount = request.FrequencyCount,
            FrequencyTimer = request.FrequencyTimer,
            IsSetup = false,
            Live = false,
            Name = request.Name,
            RiskAmount = request.RiskAmount,
            OddHourStartTime = request.OddHourStartTime,
            OddHourStopTime = request.OddHourStopTime
        };

        _context.Observatories.Add(Observatory);
        await _context.SaveChangesAsync();
        UserObservatory userObservatory = new UserObservatory
        {
            ObservatoryId = Observatory.Id,
            UserId = User.Id,
            Role = Role.Admin,
            Status = Status.Member,
        };
        _context.UserObservatories.Add(userObservatory);
        await _context.SaveChangesAsync();
        return Observatory;
    }
    private List<string> ValidateObservatory(ObservatoryRequest request)
    {
        var errors = new List<string>();
        var bank = _context.Banks.Where(b => b.Id == request.BankId).Count();
        if (bank <= 0)
        {
            errors.Add("Invalid Bank");
        }
        return errors;
    }

    public List<string> Errors()
    {
        throw new NotImplementedException();
    }
}