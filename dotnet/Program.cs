using dotnet_api.Exceptions;
using dotnet_api.Dto;
using dotnet_api.Middleware;
using dotnet_api.Services;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register ItemService as a singleton (in-memory store)
builder.Services.AddSingleton<IItemService, ItemService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Global exception handler for ItemNotFoundException -> 404
app.UseExceptionHandler(error => error.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    if (exception is ItemNotFoundException)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Status = 404,
            Message = exception.Message
        });
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Status = 500,
            Message = "Internal server error"
        });
    }
}));

app.UseHttpsRedirection();

// New Relic body capture middleware (request + response bodies as custom attributes)
app.UseMiddleware<NewRelicBodyCaptureMiddleware>();

app.MapControllers();

app.Run();
