using NewRelic.Api.Agent;
using IAgent = NewRelic.Api.Agent.IAgent;

namespace dotnet_api.Middleware;

/// <summary>
/// ASP.NET Core middleware that captures HTTP request and response bodies,
/// sanitizes them via <see cref="BodySanitizer"/>, and attaches the cleaned
/// strings to the current New Relic transaction as custom attributes.
/// </summary>
public class NewRelicBodyCaptureMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>New Relic truncates custom attributes at 4096 characters.</summary>
    private const int MaxAttrLength = 4096;

    public NewRelicBodyCaptureMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Enable request body buffering so we can read it after the controller runs
        context.Request.EnableBuffering();

        // Swap out the response body stream with a MemoryStream so we can read it
        var originalResponseBody = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
        }
        finally
        {
            // Capture request body
            context.Request.Body.Position = 0;
            using var requestReader = new StreamReader(context.Request.Body);
            var requestBody = await requestReader.ReadToEndAsync();
            RecordBody("request.body", requestBody);

            // Capture response body
            responseBodyStream.Position = 0;
            using var responseReader = new StreamReader(responseBodyStream);
            var responseBody = await responseReader.ReadToEndAsync();
            RecordBody("response.body", responseBody);

            // Copy the buffered response back to the original stream
            responseBodyStream.Position = 0;
            await responseBodyStream.CopyToAsync(originalResponseBody);
            context.Response.Body = originalResponseBody;
        }
    }

    private static void RecordBody(string attribute, string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            return;

        var sanitized = BodySanitizer.Sanitize(raw);
        if (sanitized.Length > MaxAttrLength)
            sanitized = sanitized[..MaxAttrLength];

        NewRelic.Api.Agent.NewRelic.GetAgent().CurrentTransaction
            .AddCustomAttribute(attribute, sanitized);
    }
}
