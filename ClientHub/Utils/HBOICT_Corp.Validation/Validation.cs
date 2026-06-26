using WinterRose.Web.Validation.Issues;
using WinterRose.Web.Validation.Rules;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using WinterRose;

namespace WinterRose.Web.Validation;

/// <summary>
/// The <see cref="Validation{T}"/> class is responsible for validating instances of type 
/// <typeparamref name="T"/> based on a set of validation rules and definitions.
/// </summary>z
/// <typeparam name="T">The type of the object to be validated.</typeparam>
public class Validation<T> : IValidation<T>
{
    private readonly HashSet<string> fieldsPresent;
    private readonly List<IValidationRule> rules = [];
    private readonly IHttpContextAccessor httpContextAccessor;

    private static Type[] ValidationDefinitions = TypeWorker.FindTypesWithInterface(typeof(IValidationDefinition<>));
    private static readonly bool hasValidationDefinition = HasValidationDefinitionFor(typeof(T));

    private static bool HasValidationDefinitionFor(Type type)
    {
        return ValidationDefinitions
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(IValidationDefinition<>)))
            .Any(i => i.GetGenericArguments()[0] == type);
    }

    public Validation(IEnumerable<IValidationDefinition<T>> definitions, IHttpContextAccessor httpContextAccessor)
    {
        var definitionsList = definitions.ToArray();
        if (httpContextAccessor.HttpContext is not null
            && httpContextAccessor.HttpContext.Items.TryGetValue("present_fields", out var fields)
            && fields is HashSet<string> fieldSet)
            this.fieldsPresent = fieldSet;
        else
            this.fieldsPresent = [];

        foreach (var def in definitionsList)
            def.Define(this);
        this.httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Whether or not the request contains the provided field.
    /// </summary>
    /// <param name="field"></param>
    /// <returns></returns>
    public bool HasField(string field) => fieldsPresent.Contains(field);

    private static bool IsNullable(PropertyInfo propInfo)
        => new NullabilityInfoContext().Create(propInfo)
        .ReadState == NullabilityState.Nullable;

    /// <summary>
    /// Validates the provided value of type <typeparamref name="T"/> against the defined validation rules.
    /// </summary>
    /// <param name="value">The value to be validated.</param>
    /// <param name="autoThrowOnInvalid">Indicates whether an exception should be thrown automatically if validation fails.</param>
    /// <returns>A <see cref="ValidationContext{T}"/> containing the validation results.</returns>
    public async Task<IValidationContext> ValidateAsync(T value, bool autoThrowOnInvalid = true)
    {
        var context = new ValidationContext<T>(value, fieldsPresent);

        if (value is null)
        {
            AddNullIssue(context, autoThrowOnInvalid);
            return context;
        }

        var isPartial = ResolveIsPartialRequest();

        var dtoAttribute = typeof(T).GetCustomAttribute<ValidatedAttribute>();
        var isValidated = dtoAttribute is not null || hasValidationDefinition;

        if (isValidated)
        {
            await ValidateObjectBased(value, context, isPartial);
        }
        else
        {
            await ValidateRules(context);
        }

        if (autoThrowOnInvalid)
            context.ThrowIfInvalid();

        return context;
    }

    private bool ResolveIsPartialRequest()
    {
        var httpContext = httpContextAccessor.HttpContext;
        var endpoint = httpContext?.GetEndpoint();

        var actionDescriptor = endpoint?
            .Metadata
            .GetMetadata<ControllerActionDescriptor>();

        if (actionDescriptor is null)
            return false;

        var parameter = actionDescriptor.Parameters
            .OfType<ControllerParameterDescriptor>()
            .FirstOrDefault(p => p.ParameterType == typeof(T));

        if (parameter is null)
            return false;

        return parameter.ParameterInfo.GetCustomAttribute<PartialAttribute>() is not null;
    }

    private async Task ValidateObjectBased(
    T value,
    IValidationContext context,
    bool isPartial)
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var rulesByField = rules
                .GroupBy(r => r.FieldName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList(),
                    StringComparer.OrdinalIgnoreCase
                );

        foreach (var prop in props)
        {
            await ValidateProperty(value, prop, context, rulesByField, isPartial);
        }
    }

    private async Task ValidateRules(IValidationContext context)
    {
        foreach (var rule in rules)
            await rule.Validate(context);
    }

    private async Task ValidateProperty(
    T value,
    PropertyInfo prop,
    IValidationContext context,
    Dictionary<string, List<IValidationRule>> rulesByField,
    bool isPartial)
    {
        var valueForProp = prop.GetValue(value);

        var isNullable = IsNullable(prop);
        var isObsolete = prop.GetCustomAttribute<ObsoleteAttribute>() != null;

        if (isObsolete && fieldsPresent.Contains(prop.Name))
        {
            context.AddIssue(
                ValidationIssues.DeprecatedField(prop.Name)
            );
        }

        if (valueForProp is null && !isObsolete)
        {
            var isMissing = !fieldsPresent.Contains(prop.Name);

            if (!isNullable && !(isPartial && isMissing))
            {
                context.AddIssue(
                    ValidationIssues.Required(prop.Name)
                );
            }

            return;
        }

        if (valueForProp is not null)
        {
            var propType = prop.PropertyType;

            if (HasValidationDefinitionFor(propType))
            {
                var validatorType = typeof(Validation<>).MakeGenericType(propType);
                var validatorObj = httpContextAccessor.HttpContext?
                    .RequestServices.GetService(validatorType);

                if (validatorObj is not null)
                {
                    dynamic nestedValidator = validatorObj;
                    var nestedContext = await nestedValidator.ValidateAsync((dynamic)valueForProp);

                    foreach (var issue in nestedContext.Issues)
                    {
                        context.AddIssue(issue);
                    }
                }
            }
        }

        if (rulesByField.TryGetValue(prop.Name, out var fieldRules))
        {
            await Task.WhenAll(fieldRules.Select(r => r.Validate(context)));
        }
    }

    private void AddNullIssue(IValidationContext context, bool autoThrowOnInvalid)
    {
        context.AddIssue(
            ValidationIssue.Error(
                "REQUEST_NULL",
                "The request was null. Cannot validate a null request"
            )
        );

        if (autoThrowOnInvalid)
            context.ThrowIfInvalid();
    }

    /// <summary>
    /// Adds a validation rule to the current validator instance.
    /// </summary>
    /// <remarks>This method enables fluent chaining of multiple validation rules. The order in which rules
    /// are added determines the order in which they are evaluated.</remarks>
    /// <param name="rule">The validation rule to add. Cannot be null.</param>
    /// <returns>The current <see cref="Validation{T}"/> instance with the specified rule added.</returns>
    public IValidation<T> WithRule(IValidationRule rule)
    {
        rules.Add(rule);
        return this;
    }

    /// <summary>
    /// A factory method to create a <see cref="RuleBuilder{T, TProp}"/> for fluent rule definition.
    /// </summary>
    /// <typeparam name="TProp">The type of the property to be validated.</typeparam>
    /// <param name="expr">An expression that specifies the property to be validated.</param>
    /// <returns>A <see cref="RuleBuilder{T, TProp}"/> instance for defining validation rules for the specified property.</returns>
    public IRuleBuilder<T, TProp> RuleFor<TProp>(Expression<Func<T, TProp>> expr)
    => new RuleBuilder<T, TProp>(this, expr);


    public async Task<IValidationContext> ApplyFrom<TProp>(
        Expression<Func<T, TProp>> expr,
        TProp value,
        [CallerArgumentExpression("value")] string? valueFieldName = null,
        bool autoThrowWhenInvalid = true)
    {
        var context = new ValidationContext<object?>(value, fieldsPresent);
        if(value is null)
        {
            context.AddIssue(
                ValidationIssues.NullNotAllowed(valueFieldName ?? Util.GetPropertyName(expr))
            );
            if (autoThrowWhenInvalid)
                context.ThrowIfInvalid();
            return context;
        }

        var propertyName = Util.GetPropertyName(expr);

        var rulesToApply = rules.Where(r => r.FieldName == propertyName);
        foreach (var rule in rulesToApply)
            await rule.ValidateValue(value, context);

        foreach (var issue in context.Issues)
        {
            if (issue.Field.CompareTo(propertyName, StringComparison.OrdinalIgnoreCase) == 0)
                issue.Field = valueFieldName ?? propertyName;
        }

        if (autoThrowWhenInvalid)
            context.ThrowIfInvalid();

        return context.Cast<TProp>();
    }
}