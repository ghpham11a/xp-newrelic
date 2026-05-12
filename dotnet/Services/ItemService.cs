using System.Diagnostics.Metrics;
using dotnet_api.Exceptions;
using dotnet_api.Models;

namespace dotnet_api.Services;

public class ItemService : IItemService
{
    private readonly List<Item> _items = new();
    private readonly object _lock = new();

    // Custom metrics via System.Diagnostics.Metrics (picked up by New Relic agent)
    private static readonly Meter Meter = new("dotnet_api.Items", "1.0.0");
    private static readonly Counter<long> CreatedCounter = Meter.CreateCounter<long>("items.created", description: "Items created");
    private static readonly Counter<long> UpdatedCounter = Meter.CreateCounter<long>("items.updated", description: "Items updated");
    private static readonly Counter<long> DeletedCounter = Meter.CreateCounter<long>("items.deleted", description: "Items deleted");
    private static readonly Counter<long> NotFoundCounter = Meter.CreateCounter<long>("items.not_found", description: "Item lookups that returned 404");
    private static readonly Histogram<double> NameLengthHistogram = Meter.CreateHistogram<double>("items.name_length", unit: "characters", description: "Distribution of item name lengths");
    private static readonly Histogram<double> PayloadSizeHistogram = Meter.CreateHistogram<double>("items.create_payload_size", unit: "bytes", description: "Estimated payload size of created items");

    private int _itemCount;

    public ItemService()
    {
        Meter.CreateObservableGauge("items.count", () => _itemCount, description: "Current number of items in the store");
    }

    public List<Item> FindAll()
    {
        lock (_lock)
        {
            return _items.ToList();
        }
    }

    public Item FindById(string id)
    {
        lock (_lock)
        {
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item == null)
            {
                NotFoundCounter.Add(1);
                throw new ItemNotFoundException(id);
            }
            return item;
        }
    }

    public Item Create(string name, string? description)
    {
        var item = new Item { Name = name, Description = description };
        lock (_lock)
        {
            _items.Add(item);
            _itemCount = _items.Count;
        }

        CreatedCounter.Add(1);
        NameLengthHistogram.Record(name?.Length ?? 0);
        PayloadSizeHistogram.Record(EstimateSize(item));

        return item;
    }

    public Item Update(string id, string name, string? description)
    {
        lock (_lock)
        {
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item == null)
            {
                NotFoundCounter.Add(1);
                throw new ItemNotFoundException(id);
            }

            item.Name = name;
            item.Description = description;

            UpdatedCounter.Add(1);
            NameLengthHistogram.Record(name?.Length ?? 0);

            return item;
        }
    }

    public void Delete(string id)
    {
        lock (_lock)
        {
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item == null)
            {
                NotFoundCounter.Add(1);
                throw new ItemNotFoundException(id);
            }

            _items.Remove(item);
            _itemCount = _items.Count;
        }

        DeletedCounter.Add(1);
    }

    private static double EstimateSize(Item item)
    {
        double size = 0;
        if (item.Name != null) size += item.Name.Length;
        if (item.Description != null) size += item.Description.Length;
        if (item.Id != null) size += item.Id.Length;
        return size * 2;
    }
}
