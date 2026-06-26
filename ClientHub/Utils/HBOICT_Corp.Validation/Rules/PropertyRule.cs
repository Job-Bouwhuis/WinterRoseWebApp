using WinterRose.Web.Validation.Issues;
using System.Linq.Expressions;
using System.Reflection;

namespace WinterRose.Web.Validation.Rules;

/// <summary>
/// Defines a validation rule for a specific property of an object of type T.
/// </summary>
/// <typeparam name="T">The type of the object being validated.</typeparam>
/// <typeparam name="TProp">The type of the property being validated.</typeparam>
public class PropertyRule<T, TProp> : IValidationRule
{
    private readonly Expression<Func<T, TProp>> expression;
    private readonly Func<T, TProp> selector;
    private readonly Func<TProp, Task<bool>> predicate;
    private readonly Func<ValidationIssue> issueFactory;

    public string FieldName { get; set; }
    public string RuleName { get; }

    public PropertyRule(
        Expression<Func<T, TProp>> selector,
        Func<TProp, Task<bool>> predicate,
        Func<ValidationIssue> issueFactory,
        string ruleName)
    {
        FieldName = Util.GetPropertyName(selector);
        this.expression = selector;
        this.selector = selector.Compile();
        this.predicate = predicate;
        this.issueFactory = issueFactory;
        RuleName = ruleName;
    }

    /// <summary>
    /// Performs asynchronous validation on the specified context and adds an issue if the validation fails.
    /// </summary>
    /// <param name="context">The validation context containing the value to be validated and for reporting validation issues.</param>
    /// <returns>A task that represents the asynchronous validation operation.</returns>
    public async Task Validate(IValidationContext context)
    {
        if(context.Value is not T prop)
        {
            context.AddIssue(ValidationIssues.WrongType(typeof(T), context.Value.GetType()));
            return;
        }
        var value = selector(prop);

        var res = await predicate(value);
        if (!res)
            context.AddIssue(issueFactory());
    }

    public async Task ValidateValue(object value, IValidationContext context)
    {
        if (value is TProp typedValue)
        {
            var res = await predicate(typedValue);
            if (!res)
                context.AddIssue(issueFactory());
        }
        else
            throw new ArgumentException($"Expected value of type {typeof(T).Name} but got {value.GetType().Name}");

    }
}
