using WinterRose.Web.Validation.Rules;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace WinterRose.Web.Validation;

public class Validator
{
    private readonly IServiceProvider serviceProvider;

    public Validator(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Begins a validation call for the provided value, build the rules by method chaining. Call <see cref="ValidationCall{T}.Ensure"/> to execute the validation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value">The value to validate</param>
    /// <param name="valueFieldName">Do <b>NOT</b> provide this parameter manually. It is automatically populated with the name of the argument passed to the <paramref name="value"/> parameter.</param>
    public ValidationCall<T> Expect<T>(T value, bool autoThrowOnInvalid = true, [CallerArgumentExpression("value")] string? valueFieldName = null)
    {
        var validator = new Validation<CustomValidationValue<T>>([], serviceProvider.GetRequiredService<IHttpContextAccessor>());
        return new ValidationCall<T>(value, valueFieldName, validator, autoThrowOnInvalid);
    }

    public class ValidationCall<T>(
        T value,
        string? valueFieldName,
        Validation<CustomValidationValue<T>> validator,
        bool autoThrowOnInvalid = true)
        : RuleBuilder<CustomValidationValue<T>, T>(validator, (e) => e.Value)
    {
        private readonly T value = value;
        private readonly bool autoThrowOnInvalid = autoThrowOnInvalid;

        public new IValidationContext Ensure()
        {
            Task<IValidationContext> t
                = Validator.ValidateAsync(
                    new CustomValidationValue<T>(value),
                    false);

            var ctx = t.GetAwaiter().GetResult();

            foreach(var issue in ctx.Issues)
                if (issue.Field == "value")
                    issue.Field = valueFieldName;

            if (autoThrowOnInvalid)
                ctx.ThrowIfInvalid();

            return ctx;
        }
    }
}
