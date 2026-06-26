namespace WinterRose.Web;

/// <summary>
/// Defines a standard structure for API responses, 
/// encapsulating the response data and any validation issues that may have occurred during the request processing.
/// </summary>
/// <param name="data"></param>
/// <param name="issues"></param>
public class APIResponse(object? data, Dictionary<string, List<object>>? issues)
{
    public object? Data => data;
    public Dictionary<string, List<object>>? Issues => issues;
}

