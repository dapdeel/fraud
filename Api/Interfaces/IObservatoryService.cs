using Api.Models;

namespace Api.Services.Interfaces;
public interface IObservatoryService : IBaseService{

    public abstract  Task<Observatory?> Add(ObservatoryRequest request, string UserId);

}