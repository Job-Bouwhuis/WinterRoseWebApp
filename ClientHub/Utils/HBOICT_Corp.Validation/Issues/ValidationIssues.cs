using System.Numerics;

namespace WinterRose.Web.Validation.Issues;

/// <summary>
/// A static factory class that provides methods to create common validation issues with predefined codes, messages, and severities.
/// </summary>
public static class ValidationIssues
{
    /// <summary>
    /// Creates a validation issue indicating that an email address is required.
    /// </summary>
    /// <param name="field">The name of the field to associate with the validation issue. Defaults to "email".</param>
    /// <param name="severity">The severity level to assign to the validation issue. Defaults to ValidationSeverity.Error.</param>
    /// <returns>A ValidationIssue representing a missing required email address for the specified field.</returns>
    public static ValidationIssue EmailRequired(string field = "email", ValidationSeverity severity = ValidationSeverity.Error)
        => new ValidationIssue(
            "EMAIL_REQUIRED",
            severity,
            "Email is required",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that the provided email address is not valid.
    /// </summary>
    /// <param name="field">The name of the field to associate with the validation issue. Defaults to "email".</param>
    /// <param name="severity">The severity level to assign to the validation issue. Defaults to ValidationSeverity.Error.</param>
    /// <returns>A ValidationIssue representing an invalid email address for the specified field.</returns>
    public static ValidationIssue NotAnEmail(string field = "email", ValidationSeverity severity = ValidationSeverity.Error)
        => new ValidationIssue(
            "NOT_AN_EMAIL",
            severity,
            "Email is not valid",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that a password is required.
    /// </summary>
    /// <param name="field">The name of the field associated with the password requirement. Defaults to "password".</param>
    /// <param name="severity">The severity level to assign to the validation issue. Defaults to ValidationSeverity.Error.</param>
    /// <returns>A ValidationIssue representing a required password validation error for the specified field and severity.</returns>
    public static ValidationIssue PasswordRequired(string field = "password", ValidationSeverity severity = ValidationSeverity.Error)
        => new ValidationIssue(
            "PASSWORD_REQUIRED",
            severity,
            "Password is required",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that a password does not meet the minimum length requirement.
    /// </summary>
    /// <param name="field">The name of the field associated with the password. Defaults to "password".</param>
    /// <param name="minLength">The minimum number of characters required for the password. Must be a positive integer. Defaults to 8.</param>
    /// <param name="severity">The severity level assigned to the validation issue. Defaults to ValidationSeverity.Error.</param>
    /// <returns>A ValidationIssue representing a password that is too short, including the specified field, minimum length, and
    /// severity.</returns>
    public static ValidationIssue PasswordTooShort(string field = "password", int minLength = 8, ValidationSeverity severity = ValidationSeverity.Error)
        => new ValidationIssue(
            "PASSWORD_TOO_SHORT",
            severity,
            $"Password must be at least {minLength} characters long",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that a password is too weak.
    /// </summary>
    /// <param name="field">The name of the field associated with the password. Defaults to "password".</param>
    /// <param name="severity">The severity level to assign to the validation issue. Defaults to ValidationSeverity.Warning.</param>
    /// <returns>A ValidationIssue representing a weak password validation error for the specified field and severity.</returns>
    public static ValidationIssue PasswordTooWeak(string field = "password", ValidationSeverity severity = ValidationSeverity.Warning)
        => new ValidationIssue(
            "PASSWORD_TOO_WEAK",
            severity,
            "Password is too weak",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that a required field is missing or empty.
    /// </summary>
    /// <param name="field">The name of the field that is required.</param>
    /// <param name="severity">The severity level to assign to the validation issue. Defaults to ValidationSeverity.Error.</param>
    /// <returns>A ValidationIssue representing a required field validation error for the specified field and severity.</returns>
    public static ValidationIssue Required(string field, ValidationSeverity severity = ValidationSeverity.Error)
        => new ValidationIssue(
            "REQUIRED",
            severity,
            $"{field} is required",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that a field cannot be null.
    /// </summary>
    /// <param name="field">The name of the field that cannot be null.</param>
    /// <param name="severity">The severity level to assign to the validation issue. Defaults to ValidationSeverity.Error.</param>
    /// <returns>A ValidationIssue representing a null not allowed validation error for the specified field and severity.</returns>
    public static ValidationIssue NullNotAllowed(string field, ValidationSeverity severity = ValidationSeverity.Error)
        => new ValidationIssue(
            "NULL_NOT_ALLOWED",
            severity,
            $"{field} cannot be null",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that a specified field does not meet the minimum length requirement.
    /// </summary>
    /// <param name="field">The name of the field being validated. Cannot be null or empty.</param>
    /// <param name="minLength">The minimum number of characters required for the field. Must be greater than zero.</param>
    /// <param name="severity">The severity level to assign to the validation issue. The default is ValidationSeverity.Error.</param>
    /// <returns>A ValidationIssue representing a failure due to the field being shorter than the specified minimum length.</returns>
    public static ValidationIssue TooShort(string field, int minLength, ValidationSeverity severity = ValidationSeverity.Error)
        => new ValidationIssue(
            "TOO_SHORT",
            severity,
            $"{field} must be at least {minLength} characters long",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that a specified field exceeds the maximum length requirement.
    /// </summary>
    /// <param name="field">The name of the field being validated. Cannot be null or empty.</param>
    /// <param name="maxLength">The maximum number of characters allowed for the field. Must be greater than zero.</param>
    /// <param name="severity">The severity level to assign to the validation issue. The default is ValidationSeverity.Error.</param>
    /// <returns>A ValidationIssue representing a failure due to the field exceeding the specified maximum length.</returns>
    public static ValidationIssue TooLong(string field, int maxLength, ValidationSeverity severity = ValidationSeverity.Error)
        => new ValidationIssue(
            "TOO_LONG",
            severity,
            $"{field} must be at most {maxLength} characters long",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that a field's value is outside the specified range.
    /// </summary>
    /// <param name="field">The name of the field being validated. Cannot be null or empty.</param>
    /// <param name="min">The minimum allowed value for the field.</param>
    /// <param name="max">The maximum allowed value for the field.</param>
    /// <param name="severity">The severity level to assign to the validation issue. The default is ValidationSeverity.Error.</param>
    /// <returns>A ValidationIssue representing an out-of-range validation error for the specified field.</returns>
    public static ValidationIssue OutOfRange<TSelf>(string field, INumber<TSelf> min, INumber<TSelf> max, ValidationSeverity severity = ValidationSeverity.Error) 
        where TSelf : INumber<TSelf>
        => new ValidationIssue(
            "OUT_OF_RANGE",
            severity,
            $"{field} must be between {min} and {max}",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that a specified field must have a positive value.
    /// </summary>
    /// <param name="field">The name of the field to validate. Cannot be null or empty.</param>
    /// <param name="severity">The severity level to assign to the validation issue. The default is ValidationSeverity.Error.</param>
    /// <returns>A ValidationIssue representing the requirement that the specified field must be positive.</returns>
    public static ValidationIssue MustBePositive(string field, ValidationSeverity severity = ValidationSeverity.Error)
        => new ValidationIssue(
            "MUST_BE_POSITIVE",
            severity,
            $"{field} must be a positive value",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that a field's value does not match the expected format.
    /// </summary>
    /// <param name="field">The name of the field that failed format validation. Cannot be null or empty.</param>
    /// <param name="expectedFormat">A description of the format that the field value is expected to match. Cannot be null or empty.</param>
    /// <param name="severity">The severity level to assign to the validation issue. The default is ValidationSeverity.Error.</param>
    /// <returns>A ValidationIssue representing the invalid format error for the specified field.</returns>
    public static ValidationIssue InvalidFormat(string field, string expectedFormat, ValidationSeverity severity = ValidationSeverity.Error)
        => new ValidationIssue(
            "INVALID_FORMAT",
            severity,
            $"{field} has an invalid format (expected: {expectedFormat})",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that a required collection field is empty.
    /// </summary>
    /// <param name="field">The name of the field that must contain at least one item. Cannot be null or empty.</param>
    /// <param name="severity">The severity level to assign to the validation issue. The default is ValidationSeverity.Error.</param>
    /// <returns>A ValidationIssue representing an error for an empty collection field.</returns>
    public static ValidationIssue EmptyCollection(string field, ValidationSeverity severity = ValidationSeverity.Error)
        => new ValidationIssue(
            "EMPTY_COLLECTION",
            severity,
            $"{field} must contain at least one item",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that a collection field contains more items than the specified maximum.
    /// </summary>
    /// <param name="field">The name of the field that has too many items. Cannot be null or empty.</param>
    /// <param name="max">The maximum number of items allowed in the collection.</param>
    /// <param name="severity">The severity level to assign to the validation issue. The default is ValidationSeverity.Error.</param>
    /// <returns>A ValidationIssue representing an error for a collection field with too many items.</returns>
    public static ValidationIssue TooManyItems(string field, int max, ValidationSeverity severity = ValidationSeverity.Error)
        => new ValidationIssue(
            "TOO_MANY_ITEMS",
            severity,
            $"{field} cannot contain more than {max} items",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that a field's value must be unique, but a duplicate was found.
    /// </summary>
    /// <param name="field">The name of the field that must be unique. Cannot be null or empty.</param>
    /// <param name="severity">The severity level to assign to the validation issue. The default is ValidationSeverity.Error.</param>
    /// <returns>A ValidationIssue representing an error for a non-unique field.</returns>
    public static ValidationIssue NotUnique(string field, ValidationSeverity severity = ValidationSeverity.Error)
        => new ValidationIssue(
            "NOT_UNIQUE",
            severity,
            $"{field} must be unique",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that a specified field is deprecated.
    /// </summary>
    /// <param name="field">The name of the field that is marked as deprecated. Cannot be null or empty.</param>
    /// <param name="severity">The severity level to assign to the deprecation issue. The default is Warning.</param>
    /// <returns>A ValidationIssue representing the deprecation of the specified field.</returns>
    public static ValidationIssue DeprecatedField(string field, ValidationSeverity severity = ValidationSeverity.Warning)
        => new ValidationIssue(
            "DEPRECATED_FIELD",
            severity,
            $"{field} is deprecated",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that the specified field contains no data.
    /// </summary>
    /// <param name="field">The name of the field that is missing data. Cannot be null or empty.</param>
    /// <param name="expectedSeverity">The severity level to assign to the validation issue.</param>
    /// <returns>A ValidationIssue representing the absence of data for the specified field, with the given severity.</returns>
    internal static ValidationIssue NoData(string field, ValidationSeverity expectedSeverity)
        => new ValidationIssue(
            "NO_DATA",
            expectedSeverity,
            $"{field} has no data",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that the specified field was of the wrong type
    /// </summary>
    /// <returns></returns>
    internal static ValidationIssue WrongType(Type expected, Type actual, ValidationSeverity expectedSeverity = ValidationSeverity.Error)
        => new ValidationIssue(
            "WRONG_TYPE",
            expectedSeverity,
            $"The data was of type {actual.FullName} while {expected.FullName} was exected!",
            "global");

    /// <summary>
    /// Creates a validation issue indicating that the specified field was not a guid
    /// </summary>
    /// <param name="field"></param>
    /// <param name="expectedSeverity"></param>
    /// <returns></returns>
    internal static ValidationIssue InvalidGuid(string field, ValidationSeverity expectedSeverity = ValidationSeverity.Error)
        => new ValidationIssue(
            "NOT_A_GUID",
            expectedSeverity,
            $"{field} is not a valid Guid value",
            field);
    internal static ValidationIssue MustBeEven(string field, ValidationSeverity severity) 
        => new ValidationIssue(
            "MUST_BE_EVEN",
            severity,
            $"{field} must be an even number",
            field
        );

    internal static ValidationIssue MustBeOdd(string field, ValidationSeverity severity) 
        => new ValidationIssue(
            "MUST_BE_ODD",
            severity,
            $"{field} must be an odd number",
            field
        );

    internal static ValidationIssue MustBeGreaterThan<T>(string field, INumber<T> minValue, ValidationSeverity severity)
        where T : INumber<T>
        => new ValidationIssue(
            "MUST_BE_GREATER_THAN",
            severity,
            $"{field} must be greater than {minValue}",
            field
        );

    internal static ValidationIssue MustBeLessThan<T>(string field, INumber<T> maxValue, ValidationSeverity severity)
                where T : INumber<T>
        => new ValidationIssue(
            "MUST_BE_LESS_THAN",
            severity,
            $"{field} must be less than {maxValue}",
            field
        );

    internal static ValidationIssue MustBeInThePast(string field, ValidationSeverity severity)
        => new ValidationIssue(
            "MUST_BE_IN_THE_PAST",
            severity,
            $"{field} must be a date/time in the past",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that a date or time field value must be in the future.
    /// </summary>
    /// <param name="field"></param>
    /// <param name="severity"></param>
    /// <returns></returns>
    internal static ValidationIssue MustBeInTheFuture(string field, ValidationSeverity severity)
        => new ValidationIssue(
            "MUST_BE_IN_THE_FUTURE",
            severity,
            $"{field} must be a date/time in the future",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that a date or time field value is outside the specified range.
    /// </summary>
    /// <param name="field">The name of the field being validated. Used to identify the source of the validation issue.</param>
    /// <param name="begin">The inclusive lower bound of the valid date or time range.</param>
    /// <param name="end">The inclusive upper bound of the valid date or time range.</param>
    /// <param name="severity">The severity level to assign to the validation issue.</param>
    /// <returns>A ValidationIssue representing a failure when the field value is not between the specified begin and end values.</returns>
    internal static ValidationIssue MustBeBetween(string field, DateTime begin, DateTime end, ValidationSeverity severity)
        => new ValidationIssue(
            "TIME_OUT_OF_RANGE",
            severity,
            $"{field} must be between {begin:d-t} and {end:d-t}",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that the specified field contains whitespace.
    /// </summary>
    /// <param name="field"></param>
    /// <param name="severity"></param>
    /// <returns></returns>
    internal static ValidationIssue WhitespaceNotAllowed(string field, ValidationSeverity severity) 
        => new ValidationIssue(
            "WHITESPACE_NOT_ALLOWED",
            severity,
            $"{field} cannot contain whitespace",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that the specified field contains a password found in a list of common
    /// passwords, which is not allowed.
    /// </summary>
    /// <param name="field">The name of the field whose value was identified as a common password.</param>
    /// <returns>A ValidationIssue representing an error for use of a common password in the specified field.</returns>
    internal static ValidationIssue CommonPassword(string field)
        => new ValidationIssue(
            "COMMON_PASSWORD",
            ValidationSeverity.Error,
            $"Value of '{field}' was found in a list of common passwords and is thus illegal to use",
            field);

    /// <summary>
    /// Creates a validation issue indicating that the specified field's value must be before the given date or time.
    /// </summary>
    /// <param name="field"></param>
    /// <param name="other"></param>
    /// <param name="severity"></param>
    /// <returns></returns>
    internal static ValidationIssue MustBeBefore(string field, DateTime other, ValidationSeverity severity) 
        => new ValidationIssue(
            "MUST_BE_BEFORE",
            severity,
            $"{field} must be before {other:d-t}",
            field
        );
    /// <summary>
    /// Creates a validation issue indicating that the specified field's value must be after the given date or time.
    /// </summary>
    /// <param name="field"></param>
    /// <param name="other"></param>
    /// <param name="severity"></param>
    /// <returns></returns>
    internal static ValidationIssue MustBeAfter(string field, DateTime other, ValidationSeverity severity)
        => new ValidationIssue(
            "MUST_BE_AFTER",
            severity,
            $"{field} must be after {other:d-t}",
            field
        );
    /// <summary>
    /// Defines a validation issue indicating that the field's may not be the default value for its type
    /// </summary>
    /// <param name="field"></param>
    /// <param name="severity"></param>
    /// <returns></returns>
    internal static ValidationIssue DefaultNotAllowed(string field, ValidationSeverity severity) 
        => new ValidationIssue(
            "DEFAULT_NOT_ALLOWED",
            severity,
            $"{field} cannot have the default value",
            field
        );
   
    /// <summary>
    /// Checks if password contains atleast 1 uppercase letter, altleast 1 number, atleast 1 special character and is atleast 8 characters long
    /// </summary>
    /// <param name="field"></param>
    /// <param name="severity"></param>
    /// <returns></returns>
    internal static ValidationIssue IsNotComplexPassword(string field, ValidationSeverity severity) 
        => new ValidationIssue(
            "IS_NOT_COMPLEX_PASSWORD",
            severity,
            $"{field} must contain atleast 1 uppercase letter,atleast 1 number,atleast 1 special character and be atleast 8 characters long",
            field
        );

    /// <summary>
    /// Creates a validation issue indicating that the specified field's value does not have the required length.
    /// </summary>
    /// <param name="field"></param>
    /// <param name="length"></param>
    /// <param name="severity"></param>
    /// <returns></returns>
    internal static ValidationIssue InvalidLength(string field, int length, ValidationSeverity severity) 
        => new ValidationIssue(
            "INVALID_LENGTH",
            severity,
            $"{field} must be exactly {length} characters long",
            field
        );
}