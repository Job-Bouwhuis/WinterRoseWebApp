using System.Diagnostics.CodeAnalysis;

namespace WinterRose.Web.Utils;

/// <summary>
/// Represents the result of an operation that can either succeed with a value of type TItem or fail with an error of type TError.
/// </summary>
/// <typeparam name="TItem"></typeparam>
/// <typeparam name="TError"></typeparam>
public readonly struct Result<TItem, TError>
{
    /// <summary>
    /// May be null if the result is a failure. Use <see cref="IsSuccess"/> to check if the result is successful before accessing this property.
    /// </summary>
    public TItem Item { get; }
    /// <summary>
    /// May be null if the result is a success. Use <see cref="IsFailure"/> to check if the result is a failure before accessing this property.
    /// </summary>
    public TError Error { get; }

    /// <summary>
    /// Whether or not the result is a success.
    /// </summary>
    public bool IsSuccess { get; }
    /// <summary>
    /// Whether or not this result is a failure.
    /// </summary>
    public bool IsFailure => !IsSuccess;

// Non-nullable field must contain a non-null value when exiting constructor.
#pragma warning disable CS8618
    /// <summary>
    /// Initializes a new instance of the Result class that represents a successful result with the specified item.
    /// </summary>
    /// <param name="item">The result item to encapsulate. Cannot be null.</param>
    public Result(TItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        Item = item;
        Error = default!;
        IsSuccess = true;
    }

    /// <summary>
    /// Initializes a new instance of the Result class that represents a failed operation with the specified error.
    /// </summary>
    /// <remarks>Use this constructor to create a Result instance that indicates failure. The Error property
    /// will be set to the specified error, and IsSuccess will be set to false.</remarks>
    /// <param name="error">The error information associated with the failed operation. Cannot be null.</param>
    public Result(TError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        Error = error;
        Item = default!;
        IsSuccess = false;
    }
// Non-nullable field must contain a non-null value when exiting constructor.
#pragma warning restore CS8618 

    /// <summary>
    /// Gets the item associated with a successful result.
    /// </summary>
    /// <returns>The item of type TItem if the result indicates success.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the result does not indicate success.</exception>
    public TItem GetItem()
    {
        if (!IsSuccess)
            throw new InvalidOperationException(
                "Cannot access item from a failed result.");

        return Item!;
    }

    /// <summary>
    /// Gets the error value associated with an unsuccessful result.
    /// </summary>
    /// <returns>The error value of type TError for the current result.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the result represents a successful operation.</exception>
    public TError GetError()
    {
        if (IsSuccess)
            throw new InvalidOperationException(
                "Cannot access error from a successful result.");

        return Error!;
    }

    /// <summary>
    /// Invokes the specified delegate based on whether the result represents a success or a failure, and returns the
    /// result of the invoked delegate.
    /// </summary>
    /// <remarks>This method enables pattern matching on the result, allowing callers to handle success and
    /// failure cases in a single expression.</remarks>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="success">A delegate to invoke if the result is successful. The delegate receives the success value and returns a value of
    /// type TResult.</param>
    /// <param name="failure">A delegate to invoke if the result is a failure. The delegate receives the error value and returns a value of
    /// type TResult.</param>
    /// <returns>The value returned by either the success or failure delegate, depending on the state of the result.</returns>
    public TResult Match<TResult>(
        Func<TItem, TResult> success,
        Func<TError, TResult> failure)
    {
        return IsSuccess
            ? success(Item!)
            : failure(Error!);
    }

    /// <summary>
    /// Invokes the specified action based on the result state. Executes the success action if the result represents a
    /// success; otherwise, executes the failure action.
    /// </summary>
    /// <remarks>This method provides a way to handle both success and failure cases without explicit
    /// branching. Both actions must be provided; otherwise, an exception may occur.</remarks>
    /// <param name="success">The action to invoke if the result is successful. Receives the successful item as its argument. Cannot be null.</param>
    /// <param name="failure">The action to invoke if the result is a failure. Receives the error as its argument. Cannot be null.</param>
    public void Match(
        Action<TItem> success,
        Action<TError> failure)
    {
        if (IsSuccess)
        {
            success(Item!);
            return;
        }

        failure(Error!);
    }

    /// <summary>
    /// Attempts to retrieve the item if the operation was successful.
    /// </summary>
    /// <param name="item">When this method returns, contains the retrieved item if the operation was successful; otherwise, the default
    /// value for the type.</param>
    /// <returns>true if the item was successfully retrieved; otherwise, false.</returns>
    public bool TryGetItem(out TItem? item)
    {
        item = IsSuccess
            ? Item
            : default;

        return IsSuccess;
    }

    /// <summary>
    /// Attempts to retrieve the error value if the current result represents a failure.
    /// </summary>
    /// <param name="error">When this method returns, contains the error value if the result is a failure; otherwise, the default value for
    /// the error type.</param>
    /// <returns>true if the result is a failure and an error value is available; otherwise, false.</returns>
    public bool TryGetError(out TError? error)
    {
        error = IsFailure
            ? Error
            : default;

        return IsFailure;
    }

    /// <summary>
    /// Implicitly converts a value of type TItem to a Result representing a successful result.
    /// </summary>
    /// <remarks>This operator enables implicit conversion from TItem to Result, allowing
    /// methods that return Result to return a TItem value directly. The resulting Result
    /// will represent a success state containing the specified item.</remarks>
    /// <param name="item">The value to convert to a successful result. Cannot be null if TItem is a reference type.</param>
    public static implicit operator Result<TItem, TError>(TItem item)
        => new(item);

    /// <summary>
    /// Creates a new error result from the specified error value.
    /// </summary>
    /// <remarks>This implicit conversion allows an error value to be assigned directly to a Result instance, representing a failed operation.</remarks>
    /// <param name="error">The error value to use for the result. Cannot be null if the error type is a reference type.</param>
    public static implicit operator Result<TItem, TError>(TError error)
        => new(error);
}