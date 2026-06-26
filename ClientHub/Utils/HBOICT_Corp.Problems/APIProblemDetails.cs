namespace WinterRose.Web.Problems;

/// <summary>
/// Describes the details of a problem that occurred during an API request,
/// including the type of problem and any associated issues.
/// </summary>
public class APIProblemDetails
{
    /// <summary>
    /// The type of issue that occurred during the API request. 
    /// This property is required and should provide a clear indication of the nature of the problem.
    /// </summary>
    public required string? Type { get; set; }
    /// <summary>
    /// Gets or sets the issue associated with the current context.
    /// This can be a description string or a more complex object that provides additional details about the issue.
    /// </summary>
    public required object Issue { get; set; }
}