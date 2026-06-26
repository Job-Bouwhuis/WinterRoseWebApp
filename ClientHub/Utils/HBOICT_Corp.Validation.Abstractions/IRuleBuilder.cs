using WinterRose.Web.Validation.Issues;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace WinterRose.Web.Validation.Rules
{
    public interface IRuleBuilder<T, TProp>
    {
        string FieldName { get; }
        PropertyInfo PropertyInfo { get; }
        IValidation<T> Validator { get; }

        IValidationContext Ensure();
        IRuleBuilder<T, TProp> Must(Func<TProp, Task<bool>> predicate, Func<string, ValidationIssue> issueFactory, string ruleName);
    }
}

