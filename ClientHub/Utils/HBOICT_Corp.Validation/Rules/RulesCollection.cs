using WinterRose.Web.Validation.Issues;

namespace WinterRose.Web.Validation;

using WinterRose.Web.Validation.Rules;
using System.Text.RegularExpressions;

/// <summary>
/// A static class containing extension methods for defining validation rules on properties of objects of a type T.
/// </summary>
public static class RulesCollection
{
    static readonly Regex EMAIL_REGEX =
    new Regex(
        @"^(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
    );

    extension<T, Prop>(IRuleBuilder<T, Prop> builder)
    {
        /// <summary>
        /// Defines this value may not be null
        /// </summary>
        /// <param name="severity">The severity level to associate with the validation issue if the value is null. The default is
        /// ValidationSeverity.Error.</param>
        /// <returns>A <see cref="RuleBuilder{T, Prop}"/> that can be used to further configure the validation rule.</returns>
        public IRuleBuilder<T, Prop> NotNull(ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value != null,
                field => ValidationIssues.NullNotAllowed(field, severity),
                "NotNull"
            );
        }

        /// <summary>
        /// Defines this value must be a guid
        /// </summary>
        /// <param name="severity"></param>
        /// <returns></returns>
        public IRuleBuilder<T, Prop> Guid(ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => System.Guid.TryParse(value.ToString(), out _),
                field => ValidationIssues.InvalidGuid(field, severity),
                "Guid"
            );
        }

        public IRuleBuilder<T, Prop> NotDefault()
        {
            return builder.Must(
                async value => !EqualityComparer<Prop>.Default.Equals(value, default),
                field => ValidationIssues.DefaultNotAllowed(field, ValidationSeverity.Error),
                "NotDefault"
            );
        }
    }

    extension<T>(IRuleBuilder<T, string> builder)
    {
        /// <summary>
        /// Defines the string field as an email address. 
        /// The value must match a standard email format, otherwise a validation issue will be generated.
        /// </summary>
        /// <param name="severity">The severity level to associate with the validation issue if the string is not an email. The default is
        /// ValidationSeverity.Error.</param>  
        /// <returns>A <see cref="RuleBuilder{T, string}"/> that can be used to further configure the validation rule.</returns>
        public IRuleBuilder<T, string> Email(ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value != null && EMAIL_REGEX.IsMatch(value),
                field => ValidationIssues.NotAnEmail(field, severity),
                "Email"
            );
        }

        /// <summary>
        /// Specifies that the string value must have a length greater than or equal to the specified minimum number of
        /// characters.
        /// </summary>
        /// <param name="minLength">The minimum number of characters required for the string value. Must be zero or greater.</param>
        /// <param name="severity">The severity level to associate with the validation issue if the string is too short. The default is
        /// ValidationSeverity.Error.</param> 
        /// <returns>A <see cref="RuleBuilder{T, string}"/> that can be used to further configure the validation rule.</returns>
        public IRuleBuilder<T, string> MinLength(int minLength, ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value != null && value.Length >= minLength,
                field => ValidationIssues.TooShort(field, minLength, severity),
                "MinLength"
            );
        }

        /// <summary>
        /// Defines this string field must be at most <paramref name="maxLength"/> characters long. If the string is longer, a validation issue will be generated.
        /// </summary>
        /// <param name="maxLength">The maximum allowed length for the string.</param>
        /// <param name="severity">The severity level to associate with the validation issue if the string is too long. The default is ValidationSeverity.Error.</param>  
        /// <returns>A <see cref="RuleBuilder{T, string}"/> that can be used to further configure the validation rule.</returns>
        public IRuleBuilder<T, string> MaxLength(int maxLength, ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value == null || value.Length < maxLength,
                field => ValidationIssues.TooLong(field, maxLength, severity),
                "MaxLength"
            );
        }

        public IRuleBuilder<T, string> HasLength(int length, ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value != null && value.Length == length,
                field => ValidationIssues.InvalidLength(field, length, severity),
                "HasLength"
            );
        }

        /// <summary>
        /// Adds a validation rule that requires the string value to satisfy a custom predicate, indicating an expected
        /// format.
        /// </summary>
        /// <remarks>Use this method to enforce custom format requirements that are not covered by
        /// standard validators. The rule will only be applied if the value is not <see langword="null"/>.</remarks>
        /// <param name="predicate">A function that determines whether the input string matches the required format. The function should return
        /// <see langword="true"/> if the value is valid; otherwise, <see langword="false"/>.</param>
        /// <param name="expectedFormat">A description of the expected format, used in validation messages to inform users of the required pattern.</param>
        /// <param name="severity">The severity level to associate with the validation issue if the value does not match the expected format.
        /// The default is <see cref="ValidationSeverity.Error"/>.</param>
        /// <returns>A <see cref="RuleBuilder{T, string}"/> that can be used to further configure the validation rule.</returns>
        public IRuleBuilder<T, string> Matches(
            Func<string, bool> predicate,
            string expectedFormat,
            ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value != null && predicate(value),
                field => ValidationIssues.InvalidFormat(field, expectedFormat, severity),
                "Matches"
            );
        }

        /// <summary>
        /// Defines this field must match the given regex. 
        /// The example parameter is used to provide an example of a valid format for the field, because regexes can be hard to read. 
        /// This example will be included in the validation issue if the field does not match the regex.
        /// </summary>
        /// <param name="regex">The regex to match</param>
        /// <param name="example">The example to give to the validation issue when the format is wrong</param>
        /// <param name="severity">The severity level to associate with the validation issue if the string is does not match the regex. The default is ValidationSeverity.Error.</param>  
        /// <returns>A <see cref="RuleBuilder{T, string}"/> that can be used to further configure the validation rule.</returns>
        public IRuleBuilder<T, string> Matches(
            Regex regex,
            string example,
            ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value != null && regex.IsMatch(value),
                field => ValidationIssues.InvalidFormat(field, example, severity),
                "Matches"
            );
        }


        public IRuleBuilder<T, string> NotWhitespace(ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value != null && !string.IsNullOrWhiteSpace(value),
                field => ValidationIssues.WhitespaceNotAllowed(field, severity),
                "NotWhitespace"
            );
        }

        /// <summary>
        /// Defines this field must not contain any whitespace characters (spaces, tabs, etc.).
        /// Null values are allowed.
        /// </summary>
        /// <param name="severity">The severity level to associate with the validation issue if the string contains whitespace. The default is
        /// ValidationSeverity.Error.</param>
        /// <returns>A <see cref="RuleBuilder{T, string}"/> that can be used to further configure the validation rule.</returns>
        public IRuleBuilder<T, string> NoWhitespace(ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value == null || !value.Any(char.IsWhiteSpace),
                field => ValidationIssues.WhitespaceNotAllowed(field, severity),
                "NoWhitespace"
            );
        }
    }
    extension<T, TItem>(IRuleBuilder<T, IEnumerable<TItem>> builder)
    {
        /// <summary>
        /// Defines the enumerable field must not be empty. If the enumerable is empty, a validation issue will be generated.
        /// </summary>
        /// <param name="severity">The severity level to associate with the validation issue if the enumerable is empty. The default is ValidationSeverity.Error.</param>
        /// <returns>A <see cref="RuleBuilder{T, IEnumerable{TItem}}"/> that can be used to further configure the validation rule.</returns>
        public IRuleBuilder<T, IEnumerable<TItem>> NotEmpty(ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value != null && value.Any(),
                field => ValidationIssues.EmptyCollection(field, severity),
                "NotEmpty"
            );
        }

        /// <summary>
        /// Defines this enumerable field must have at most <paramref name="max"/> items. 
        /// If the enumerable has more than <paramref name="max"/> items, a validation issue will be generated.
        /// </summary>
        /// <param name="max">The maximum number of items allowed in the enumerable.</param>
        /// <param name="severity">The severity level to associate with the validation issue if the enumerable has more than <paramref name="max"/> items. The default is ValidationSeverity.Error.</param>
        /// <returns>A <see cref="IRuleBuilder{T, IEnumerable{TItem}}"/> that can be used to further configure the validation rule.</returns>
        public IRuleBuilder<T, IEnumerable<TItem>> MaxCount(int max, ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value == null || value.Count() <= max,
                field => ValidationIssues.TooManyItems(field, max, severity),
                "MaxCount"
            );
        }

        /// <summary>
        /// Adds a rule that requires all elements in the collection to be unique according to a specified key selector.    
        /// </summary>
        /// <remarks>If the collection is null, the uniqueness rule is not applied. Use this method to
        /// ensure that no two elements in the collection have the same key value.</remarks>
        /// <typeparam name="TKey">The type of the key used to determine uniqueness for each element.</typeparam>
        /// <param name="keySelector">A function that selects the key used to compare elements for uniqueness. Cannot be null.</param>
        /// <param name="severity">The severity level to associate with the validation issue if duplicates are found. The default is
        /// ValidationSeverity.Error.</param>
        /// <returns>A RuleBuilder that enforces uniqueness of elements in the collection based on the specified key selector.</returns>
        public IRuleBuilder<T, IEnumerable<TItem>> Unique<TKey>(
            Func<TItem, TKey> keySelector,
            ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value =>
                {
                    var l = value.Select(keySelector).Distinct();
                    return value == null || l.Count() == value.Count();
                },
                field => ValidationIssues.NotUnique(field, severity),
                "Unique"
            );
        }
    }
    extension<T>(IRuleBuilder<T, bool?> builder)
    {
        /// <summary>
        /// Defines that this field must be true. If the value is false, a validation issue will be generated.
        /// </summary>
        /// <param name="severity">The severity level to associate with the validation issue if the value is false. The default is ValidationSeverity.Error.</param>
        /// <returns>A <see cref="IRuleBuilder{T, bool?}"/> that can be used to further configure the validation rule.</returns>
        public IRuleBuilder<T, bool?> MustBeTrue(ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value == null || value.Value == true,
                field => ValidationIssues.InvalidFormat(field, "Must be true", severity),
                "MustBeTrue"
            );
        }

        /// <summary>
        /// Defines that this field must be false. If the value is true, a validation issue will be generated.
        /// </summary>
        /// <param name="severity">The severity level to associate with the validation issue if the value is true. The default is ValidationSeverity.Error.</param>
        /// <returns>A <see cref="IRuleBuilder{T, bool?}"/> that can be used to further configure the validation rule.</returns>
        public IRuleBuilder<T, bool?> MustBeFalse(ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value == null || value.Value == false,
                field => ValidationIssues.InvalidFormat(field, "Must be false", severity),
                "MustBeFalse"
            );
        }
    }

    extension<T>(IRuleBuilder<T, DateTime> builder)
    {
        public IRuleBuilder<T, DateTime> MustBeBefore(DateTime other, ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value < other,
                field => ValidationIssues.MustBeBefore(field, other, severity),
                "MustBeBefore"
            );
        }

        public IRuleBuilder<T, DateTime> MustBeAfter(DateTime other, ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value > other,
                field => ValidationIssues.MustBeAfter(field, other, severity),
                "MustBeAfter"
            );
        }

        public IRuleBuilder<T, DateTime> MustBeBetween(DateTime start, DateTime end, ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value >= start && value <= end,
                field => ValidationIssues.MustBeBetween(field, start, end, severity),
                "MustBeBetween"
            );
        }
    }
}
