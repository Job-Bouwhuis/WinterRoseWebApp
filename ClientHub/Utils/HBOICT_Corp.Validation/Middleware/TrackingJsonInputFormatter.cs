using WinterRose.Web.Problems;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace WinterRose.Web.Validation.Middleware;

public class TrackingJsonInputFormatter : SystemTextJsonInputFormatter
{
    public TrackingJsonInputFormatter(
        IOptions<JsonOptions> options,
        ILogger<TrackingJsonInputFormatter> logger)
        : base(options.Value, logger)
    {
    }

    public TrackingJsonInputFormatter(JsonOptions options, ILogger<SystemTextJsonInputFormatter> logger) : base(options,
        logger)
    {
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(
        InputFormatterContext context)
    {
        var request = context.HttpContext.Request;

        request.EnableBuffering();

        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        request.Body.Position = 0;

        JsonDocument json;
        try
        {
            json = JsonDocument.Parse(body, new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            });
        }
        catch (Exception ex) when (ex.GetType().Name == "JsonReaderException")
        {
            throw new ApiProblem(
                "MALFORMED_BODY",
                "The JSON body could not be parsed");
        }


        var presentFields = json.RootElement
            .EnumerateObject()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (context.HttpContext.Items.TryGetValue("present_fields", out object? res) && res is HashSet<string> fields)
        {
            foreach (string field in presentFields)
                fields.Add(field);
        }
        else
            context.HttpContext.Items["present_fields"] = presentFields;

        return await base.ReadRequestBodyAsync(context);
    }
}

public class TrackingQueryMiddleware
{
    private readonly RequestDelegate next;

    public TrackingQueryMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var presentQueryFields = context.Request.Query.Keys
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (context.Items.TryGetValue("present_fields", out object? res) && res is HashSet<string> fields)
        {
            foreach (string field in presentQueryFields)
                fields.Add(field);
        }
        else
            context.Items["present_fields"] = presentQueryFields;

        await next(context);
    }
}