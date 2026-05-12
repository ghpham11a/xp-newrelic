using dotnet_api.Dto;
using dotnet_api.Exceptions;
using dotnet_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_api.Controllers;

[ApiController]
[Route("api/items")]
public class ItemController : ControllerBase
{
    private readonly IItemService _itemService;

    public ItemController(IItemService itemService)
    {
        _itemService = itemService;
    }

    [HttpGet]
    public ActionResult<List<ItemResponse>> List()
    {
        return _itemService.FindAll().Select(ItemResponse.From).ToList();
    }

    [HttpGet("{id}")]
    public ActionResult<ItemResponse> Get(string id)
    {
        return ItemResponse.From(_itemService.FindById(id));
    }

    [HttpPost]
    public ActionResult<ItemResponse> Create([FromBody] CreateItemRequest request)
    {
        var item = _itemService.Create(request.Name, request.Description);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ItemResponse.From(item));
    }

    [HttpPut("{id}")]
    public ActionResult<ItemResponse> Update(string id, [FromBody] UpdateItemRequest request)
    {
        return ItemResponse.From(_itemService.Update(id, request.Name, request.Description));
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(string id)
    {
        _itemService.Delete(id);
        return NoContent();
    }
}
