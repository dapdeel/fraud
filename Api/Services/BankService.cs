using System.ComponentModel.DataAnnotations;
using System.Linq;
using Api.Data;
using Api.Interfaces;
using Api.Models;

public class BankService : IBankService
{
    private readonly ApplicationDbContext _context;
    public BankService(ApplicationDbContext context)
    {
        _context = context;
    }
    public Bank Add(BankRequest bankRequest)
    {
        var existingBank = _context.Banks
        .Where(b => b.Code == bankRequest.Code && b.Country == bankRequest.Country).ToList();
        if (existingBank.Count() > 0)
        {
            throw new ValidationException("This Bank Already Exists");
        }
        var Bank = new Bank
        {
            Code = bankRequest.Code,
            Country = bankRequest.Country,
            Name = bankRequest.Name
        };
        _context.Banks.Add(Bank);
        _context.SaveChanges();
        return Bank;
    }
    public List<Bank> Add(List<BankRequest> bankRequests)
    {
        List<Bank> Banks = [];
        foreach (var bankRequest in bankRequests)
        {
            var existingBank = _context.Banks.Where(b => b.Code == bankRequest.Code && b.Country == bankRequest.Country).ToList();
            if (existingBank.Count() > 0)
            {
                continue;
            }
            var Bank = new Bank
            {
                Code = bankRequest.Code,
                Country = bankRequest.Country == null ? "NGN" : bankRequest.Country,
                Name = bankRequest.Name
            };
            _context.Banks.Add(Bank);
        }

        _context.SaveChanges();
        return Banks;
    }

    public List<Bank> All()
    {
        return _context.Banks.ToList();
    }
}