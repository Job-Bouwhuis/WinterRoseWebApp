using WinterRose.Web.Validation.Rules;
using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace WinterRose.Web.Validation
{
    public interface IValidation<T>
    {
        Task<IValidationContext> ApplyFrom<TProp>(Expression<Func<T, TProp>> expr, TProp value, string valueFieldName = null, bool autoThrowWhenInvalid = true);
        bool HasField(string field);
        IRuleBuilder<T, TProp> RuleFor<TProp>(Expression<Func<T, TProp>> expr);
        Task<IValidationContext> ValidateAsync(T value, bool autoThrowOnInvalid = true);
        IValidation<T> WithRule(IValidationRule rule);
    }
}

