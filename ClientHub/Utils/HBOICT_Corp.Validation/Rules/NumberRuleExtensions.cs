using WinterRose.Web.Validation.Issues;
using WinterRose.Web.Validation.Rules;
using System.Numerics;

namespace WinterRose.Web.Validation;

public static class NumberRuleExtensions
{
    extension<T, TNumber>(IRuleBuilder<T, TNumber> builder)
        where TNumber : INumber<TNumber>
    {
        public IRuleBuilder<T, TNumber> Positive(ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value >= TNumber.Zero,
                field => ValidationIssues.MustBePositive(field, severity),
                "Positive"
            );
        }

        public IRuleBuilder<T, TNumber> GreaterThan(TNumber min, ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value > min,
                field => ValidationIssues.MustBeGreaterThan(field, min, severity),
                "GreaterThan"
            );
        }

        public IRuleBuilder<T, TNumber> LessThan(TNumber max, ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value < max,
                field => ValidationIssues.MustBeLessThan(field, max, severity),
                "LessThan"
            );
        }

        public IRuleBuilder<T, TNumber> Range(TNumber min, TNumber max, ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value >= min && value <= max,
                field => ValidationIssues.OutOfRange(field, min, max, severity),
                "Range"
            );
        }

        public IRuleBuilder<T, TNumber> MustBeEven(ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value % TNumber.CreateChecked(2) == TNumber.Zero,
                field => ValidationIssues.MustBeEven(field, severity),
                "Even"
            );
        }

        public IRuleBuilder<T, TNumber> MustBeOdd(ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value % TNumber.CreateChecked(2) != TNumber.Zero,
                field => ValidationIssues.MustBeOdd(field, severity),
                "Odd"
            );
        }
    }

    extension<T, TNumber>(IRuleBuilder<T, TNumber?> builder)
        where TNumber : struct, INumber<TNumber>
    {
        public IRuleBuilder<T, TNumber?> Positive(ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value is null || value.Value >= TNumber.Zero,
                field => ValidationIssues.MustBePositive(field, severity),
                "Positive"
            );
        }

        public IRuleBuilder<T, TNumber?> GreaterThan(TNumber min, ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value is null || value.Value > min,
                field => ValidationIssues.MustBeGreaterThan(field, min, severity),
                "GreaterThan"
            );
        }

        public IRuleBuilder<T, TNumber?> LessThan(TNumber max, ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value is null || value.Value < max,
                field => ValidationIssues.MustBeLessThan(field, max, severity),
                "LessThan"
            );
        }

        public IRuleBuilder<T, TNumber?> Range(TNumber min, TNumber max, ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value is null || (value.Value >= min && value.Value <= max),
                field => ValidationIssues.OutOfRange(field, min, max, severity),
                "Range"
            );
        }

        public IRuleBuilder<T, TNumber?> MustBeEven(ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value is null || value.Value % TNumber.CreateChecked(2) == TNumber.Zero,
                field => ValidationIssues.MustBeEven(field, severity),
                "Even"
            );
        }

        public IRuleBuilder<T, TNumber?> MustBeOdd(ValidationSeverity severity = ValidationSeverity.Error)
        {
            return builder.Must(
                async value => value is null || value.Value % TNumber.CreateChecked(2) != TNumber.Zero,
                field => ValidationIssues.MustBeOdd(field, severity),
                "Odd"
            );
        }
    }
}
