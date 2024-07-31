using Api.Models;

namespace Api.Interfaces;
public interface IBankService {
    public List<Bank> All();
    public Bank Add(BankRequest bankRequest);
}