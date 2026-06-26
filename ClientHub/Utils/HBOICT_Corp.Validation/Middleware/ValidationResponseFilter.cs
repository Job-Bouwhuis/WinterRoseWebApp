using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WinterRose.Web.Validation.Middleware;

/// <summary>
/// Defines a common response for API endpoints, encapsulating the actual data and 
/// any validation warnings that may have occurred during request processing.
/// </summary>
public class ValidationResponseFilter : IAsyncResultFilter
{
    private readonly IValidationStore validationStore;

    public ValidationResponseFilter(IValidationStore validationStore)
    {
        this.validationStore = validationStore;
    }

    public async Task OnResultExecutionAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next)
    {
        var allWarnings = validationStore.CreateWarningsList();

        if (context.Result is ObjectResult objectResult)
        {
            objectResult.Value = new APIResponse(
                objectResult.Value,
                allWarnings
            );
        }

        await next();
    }
}