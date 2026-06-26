using WinterRose.Web.Validation.Issues;
using System.Linq.Expressions;
using System.Reflection;

namespace WinterRose.Web.Validation.Rules;

/// <summary>
/// A builder class that allows for the fluent definition of validation rules for a specific property of a type T.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="TProp"></typeparam>
public class RuleBuilder<T, TProp> : IRuleBuilder<T, TProp>
{
    private readonly IValidation<T> validator;
    public IValidation<T> Validator => validator;

    /// <summary>
    /// The name of the model property that this rule builder is associated with.
    /// </summary>
    public string FieldName => fieldName;

    private readonly Expression<Func<T, TProp>> selector;
    private readonly string fieldName;

    public PropertyInfo? PropertyInfo => Util.GetPropertyInfo(selector);

    public RuleBuilder(
        Validation<T> validator,
        Expression<Func<T, TProp>> expression)
    {
        this.validator = validator;
        selector = expression;
        fieldName = ExtractName(expression);
    }

    private static string ExtractName(Expression<Func<T, TProp>> expr)
    {
        if (expr.Body is not MemberExpression member)
            throw new InvalidOperationException("RuleBuilder can only be used with member expressions. (e.g., x => x.PropertyName)");
        return member.Member.Name.ToLower();
    }

    public IRuleBuilder<T, TProp> Must(
        Func<TProp, Task<bool>> predicate,
        Func<string, ValidationIssue> issueFactory,
        string ruleName)
    {
        validator.WithRule(new PropertyRule<T, TProp>(
            selector,
            predicate,
            () => issueFactory(fieldName),
            ruleName
        ));

        return this;
    }

    /// <summary>
    /// Ensures the validation did not result in any errors
    /// </summary>
    /// <returns>A <see cref="ValidationContext{T}"/> with info on the validated value</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public IValidationContext Ensure()
    {
        if (this is Validator.ValidationCall<TProp> handler)
            return handler.Ensure();
        else
            throw new InvalidOperationException("This rule builder was not made using Validator.Validate(). " +
                "Note the lack of generic argument on Validator.");
    }
}
