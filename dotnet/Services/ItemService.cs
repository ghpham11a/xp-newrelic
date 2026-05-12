using dotnet_api.Exceptions;
using dotnet_api.Models;

namespace dotnet_api.Services;

public class ItemService : IItemService
{
    private readonly List<Item> _items = new();

    public List<Item> FindAll() => _items.ToList();

    public Item FindById(string id) =>
        _items.FirstOrDefault(i => i.Id == id)
        ?? throw new ItemNotFoundException(id);

    public Item Create(string name, string? description)
    {
        var item = new Item { Name = name, Description = description };
        _items.Add(item);
        return item;
    }

    public Item Update(string id, string name, string? description)
    {
        var item = FindById(id);
        item.Name = name;
        item.Description = description;
        return item;
    }

    public void Delete(string id)
    {
        var item = FindById(id);
        _items.Remove(item);
    }
}
