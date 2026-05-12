using dotnet_api.Models;

namespace dotnet_api.Dto;

public class ItemResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public static ItemResponse From(Item item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Description = item.Description
    };
}
