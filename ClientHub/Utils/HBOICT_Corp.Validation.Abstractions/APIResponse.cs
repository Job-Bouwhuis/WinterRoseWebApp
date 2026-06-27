#nullable enable
using System.Collections.Generic;

namespace WinterRose.Web;

/// <summary>
/// Defines a standard structure for API responses, 
/// encapsulating the response data and any validation issues that may have occurred during the request processing.
/// </summary>
public class APIResponse
{
    public object? Data { get; private set; }
    public Dictionary<string, List<object>>? Issues { get; private set; }

    private APIResponse() { } // serialization

    /// <param name="data"></param>
    /// <param name="issues"></param>
    public APIResponse(object? data, Dictionary<string, List<object>>? issues)
    {
        Data = data;
        Issues = issues;
    }
}

