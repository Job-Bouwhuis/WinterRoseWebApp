using WinterRose.Web.Validation.Issues;
using WinterRose.Web.Problems;

namespace WinterRose.Web.Validation;

/// <summary>
/// Represents the context of a validation operation
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="value"></param>
public class ValidationContext<T>(T value, HashSet<string> fieldsPresent) : IValidationContext
{
    /// <summary>
    /// Gets the current value stored in the container.
    /// </summary>
    public T Value => value;
    object IValidationContext.Value => Value!;

    public IReadOnlyList<ValidationIssue> Issues => issues;
    private readonly List<ValidationIssue> issues = [];

    public bool HasErrors => issues.Any(x => x.Severity == ValidationSeverity.Error);
    public bool HasWarnings => issues.Any(x => x.Severity == ValidationSeverity.Warning);

    public IReadOnlyCollection<string> FieldsPresent => fieldsPresent;


    /// <inheritdoc cref="Validation{T}.HasField"/>
    public bool FieldExists(string field) => FieldsPresent.Contains(field);

    public void ThrowIfInvalid()
    {
        if (HasErrors)
            throw ToApiProblem();
    }

    /// <summary>
    /// Builds a dictionary of validation warnings, grouped by field, if any warnings are present in the context.
    /// </summary>
    /// <returns> </returns>
    public Dictionary<string, List<object>>? BuildWarnings()
    {
        if (Issues.Count == 0)
            return null;

        return Issues
            .Where(issue => issue.Severity == ValidationSeverity.Warning)
            .GroupBy(issue => issue.Field ?? "general")
            .ToDictionary(
                group => group.Key,
                group => group.Select(issue => (object)new
                {
                    issue.Code,
                    issue.Message,
                    Severity = issue.Severity.ToString()
                }).ToList());
    }

    /// <summary>
    /// Creates an API-compatible exception that represents the current set of validation issues.
    /// </summary>
    /// <remarks>The returned exception includes all validation issues grouped by their associated field.
    /// Issues without a specific field are grouped under "global". This method is intended to facilitate consistent
    /// error responses in API scenarios.</remarks>
    /// <returns>An <see cref="ApiProblem"/> instance containing details about the validation errors.</returns>
    private Exception ToApiProblem()
    {
        Dictionary<string, List<ValidationIssue>> issues = [];

        foreach (var issue in this.issues)
        {
            string field = issue.Field ?? "global";
            if (!issues.TryGetValue(field, out List<ValidationIssue>? list))
                list = issues[field] = [];

            list.Add(issue);
        }


        APIProblemDetails validationIssues = new APIProblemDetails
        {
            Type = "validation_issues",
            Issue = issues
        };

        return new ApiProblem(
            error: "VALIDATION_FAILED",
            message: "One or more values failed to meet validation criteria",
            details: validationIssues
        );
    }

    public void AddIssue(ValidationIssue issue) => issues.Add(issue);

    public ValidationContext<NewT> Cast<NewT>()
    {
        if (Value is not NewT castedValue)
        {
            if (Value is not CustomValidationValue<NewT> c)
                throw new InvalidCastException($"Cannot cast value of type {typeof(T)} to {typeof(NewT)}.");
            if (c.Value is not NewT innerCastedValue)
                throw new InvalidCastException($"Cannot cast inner value of type {typeof(T)} to {typeof(NewT)}.");
            castedValue = innerCastedValue;
        }
        return new ValidationContext<NewT>(castedValue, (HashSet<string>)FieldsPresent);
    }
}
