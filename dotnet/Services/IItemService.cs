using dotnet_api.Models;

namespace dotnet_api.Services;

public interface IItemService
{
    List<Item> FindAll();
    Item FindById(string id);
    Item Create(string name, string? description);
    Item Update(string id, string name, string? description);
    void Delete(string id);
}
