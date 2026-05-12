namespace dotnet_api.Exceptions;

public class ItemNotFoundException : Exception
{
    public ItemNotFoundException(string id)
        : base($"Item with id '{id}' not found") { }
}
