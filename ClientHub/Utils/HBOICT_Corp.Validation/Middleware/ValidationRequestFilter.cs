using WinterRose.Web.Problems;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using WinterRose.Web.Validation.Issues;
using System.Reflection;
using WinterRose;

namespace WinterRose.Web.Validation.Middleware;

/// <summary>
/// A middleware class that intercepts action execution in an ASP.NET Core application to 
/// perform validation on the DTOs (Data Transfer Objects) passed to the action methods.
/// It uses the registered validators to validate the action arguments and returns a 400 Bad Request response with 
/// validation errors if any issues are found. If validation passes, it stores the validation context for later use 
/// in the response filter.
/// </summary>
public class ValidationRequestFilter : IAsyncActionFilter
{
    private readonly IServiceProvider serviceProvider;
    private readonly IValidationStore validationStore;

    public ValidationRequestFilter(
        IServiceProvider serviceProvider,
        IValidationStore validationStore)
    {
        this.serviceProvider = serviceProvider;
        this.validationStore = validationStore;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            List<ValidationIssue> issues = [];

            if (issues.Count != 0)
            {
                List<APIProblemDetails> problemDetails = [];

                foreach (var problem in issues)
                {
                    problemDetails.Add(new APIProblemDetails()
                    {
                        Type = problem.Field,
                        Issue = problem
                    });
                }

                throw new ApiProblem(
                    "INVALID_REQUEST",
                    "One or more values had validation errors",
                    problemDetails);
            }
        }

        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg is null)
                continue;

            var argType = arg.GetType();

            var validatorType = typeof(Validation<>).MakeGenericType(argType);
            var validatorObj = serviceProvider.GetService(validatorType);
            if (validatorObj is null)
                continue;

            dynamic validator = validatorObj;
            var validationContext = await validator.ValidateAsync((dynamic)arg);
            if (validationContext.HasErrors)
            {
                throw new ApiProblem(
                    "VALIDATION_FAILED",
                    "One or more values failed to meet validation criteria",
                    new APIProblemDetails
                    {
                        Type = "VALIDATION_FAILED",
                        Issue = validationContext.Issues
                    });
            }

            var storeMethod = typeof(IValidationStore)
                .GetMethod("Set")!
                .MakeGenericMethod(argType);

            storeMethod.Invoke(validationStore, [validationContext]);
        }

        await next();
    }

    private static ValidationIssue? CreateInvalidTypeIssue(ActionExecutingContext context, string key)
    {
        var fieldName = key.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? key;
        var expectedField = ResolveExpectedField(context, key);
        if (expectedField is not null && ShouldIgnoreMissingField(context, fieldName, expectedField))
            return null;

        string expectedTypeName = BuildTypeName(expectedField?.Type);

        return ValidationIssue.Error(
            "INVALID_TYPE",
            $"Value for '{fieldName}' expected to be {expectedTypeName}",
            fieldName);
    }

    private static string BuildTypeName(Type? type) => BuildTypeName(type, false);

    private static string BuildTypeName(Type? type, bool recursive = false)
    {
        if (type is null)
            return "unknown";

        Type? nullableType = Nullable.GetUnderlyingType(type);

        if (nullableType is not null)
            return $"{BuildTypeName(nullableType)}?";

        if (type.IsArray)
            return $"an array {BuildTypeName(type.GetElementType())}";

        if (type.IsGenericType)
        {
            Type genericDefinition = type.GetGenericTypeDefinition();
            Type[] genericArguments = type.GetGenericArguments();

            string? collectionType = genericDefinition switch
            {
                Type t when
                    t == typeof(List<>) ||
                    t == typeof(HashSet<>) ||
                    t == typeof(IEnumerable<>) ||
                    t == typeof(ICollection<>) ||
                    t == typeof(IList<>) ||
                    t == typeof(IReadOnlyCollection<>) ||
                    t == typeof(IReadOnlyList<>)
                    => $"an array of '{BuildTypeName(genericArguments[0], true)}'",

                Type t when t == typeof(Dictionary<,>) =>
                    $"a dictionary of '{BuildTypeName(genericArguments[0], true)}'" +
                    $" to '{BuildTypeName(genericArguments[1], true)}'",

                _ => null
            };
            if (collectionType is not null)
                return collectionType;

            string genericTypeName = type.Name;

            int genericMarkerIndex = genericTypeName.IndexOf('`');

            if (genericMarkerIndex >= 0)
                genericTypeName = genericTypeName[..genericMarkerIndex];

            string formattedArguments = string.Join(
                ", ",
                genericArguments.Select(BuildTypeName));

            return $"{genericTypeName.ToLowerInvariant()} of {formattedArguments}";
        }

        string result = type.Name switch
        {
            nameof(Int16) => "short",
            nameof(Int32) => "int",
            nameof(Int64) => "long",

            nameof(UInt16) => "ushort",
            nameof(UInt32) => "uint",
            nameof(UInt64) => "ulong",

            nameof(Byte) => "byte",
            nameof(SByte) => "sbyte",

            nameof(Boolean) => "bool",
            nameof(Char) => "char",

            nameof(Single) => "float",
            nameof(Double) => "double",
            nameof(Decimal) => "decimal",

            nameof(Object) => "object",
            nameof(String) => "string",

            _ => type.Name.ToLowerInvariant(),
        };

        if (recursive)
            return result;
        else
            return $"of type '{result}'";
    }

    private static bool ShouldIgnoreMissingField(ActionExecutingContext context, string fieldName, FieldInfo fieldInfo)
    {
        if (context.HttpContext.Items.TryGetValue("present_fields", out var presentFieldsObj)
            && presentFieldsObj is HashSet<string> presentFields
            && !presentFields.Contains(fieldName))
        {
            if (fieldInfo.IsObsolete)
                return true;

            return fieldInfo.IsNullable;
        }

        return false;
    }

    private static FieldInfo? ResolveExpectedField(ActionExecutingContext context, string key)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor actionDescriptor)
            return null;

        var parts = key.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return null;
        if (parts[0] is "$")
            key = parts[1];
        else
            key = parts[0];
        var parameters = actionDescriptor.Parameters
            .OfType<ControllerParameterDescriptor>()
            .ToArray();

        foreach (var parameter in parameters)
        {
            if (!string.Equals(parameter.Name, parts[0], StringComparison.OrdinalIgnoreCase))
                continue;

            var currentType = parameter.ParameterType;
            for (var i = 1; i < parts.Length; i++)
            {
                var propInfo = currentType.GetProperty(parts[i], BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (propInfo is null)
                    return GetFieldInfo(parameter.ParameterInfo, currentType);
                if (i == parts.Length - 1)
                    return GetFieldInfo(propInfo, propInfo.PropertyType);
                currentType = propInfo.PropertyType;
            }

            return GetFieldInfo(parameter.ParameterInfo, currentType);
        }

        if (parameters.Length == 1)
        {
            var currentType = parameters[0].ParameterType;
            var propInfo = currentType.GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (propInfo is not null)
                return GetFieldInfo(propInfo, propInfo.PropertyType);
        }

        return null;
    }

    private static FieldInfo GetFieldInfo(ParameterInfo parameter, Type parameterType)
        => new(parameterType, IsNullable(parameter, parameterType), parameter.GetCustomAttribute<ObsoleteAttribute>() is not null);

    private static FieldInfo GetFieldInfo(PropertyInfo propInfo, Type propType)
        => new(propType, IsNullable(propInfo, propType), propInfo.GetCustomAttribute<ObsoleteAttribute>() is not null);

    private static bool IsNullable(ParameterInfo parameter, Type parameterType)
    {
        if (Nullable.GetUnderlyingType(parameterType) is not null)
            return true;

        if (!parameterType.IsValueType)
            return new NullabilityInfoContext().Create(parameter).ReadState == NullabilityState.Nullable;

        return false;
    }

    private static bool IsNullable(PropertyInfo propInfo, Type propType)
    {
        if (Nullable.GetUnderlyingType(propType) is not null)
            return true;

        if (!propType.IsValueType)
            return new NullabilityInfoContext().Create(propInfo).ReadState == NullabilityState.Nullable;

        return false;
    }

    private sealed record FieldInfo(Type Type, bool IsNullable, bool IsObsolete);
}
