

namespace WinterRose.Web.Problems;

/// <summary>
/// A standard exception class for representing API problems in a structured way. 
/// It extends the base Exception class and includes additional properties for error details.
/// </summary>
public class ApiProblem : Exception
{
    const string ERROR = "UNKNOWN_ERROR";
    const string ERROR_MESSAGE = "An error occurred while processing the request.";

    public string Error { get; }
    public List<APIProblemDetails> Details { get; } = [];

    public ApiProblem(string error, string message) : base(message)
        => Error = error;
    
    public ApiProblem(string error, string message, Exception inner) : base(message, inner)
        => Error = error;

    public ApiProblem(string error)
        : this(error, ERROR_MESSAGE)
    {
        Details = [new APIProblemDetails()
            {
                Type = null,
                Issue = error
            }];
    }
    
    public ApiProblem(string error, Exception inner)
        : this(ERROR, ERROR_MESSAGE, inner)
    {
    }

    public ApiProblem(string error, string message, params List<APIProblemDetails> details)
        : this(error, message) => Details = details;
}

public static class APIProblemExtensions
{
    extension(ApiProblem)
    {
        public static void ThrowIfNull(object? var, string error, string message)
        {
            if (var is null)
                throw new ApiProblem(error, message);
        }
    }
}