namespace WinterRose.Web.Validation;

/// <summary>
/// Defines a store for managing validation contexts, 
/// allowing for the storage and retrieval of validation results associated with different types.
/// </summary>
public class ValidationStore : IValidationStore
{
    private readonly Dictionary<Type, object> store = [];

    public void Set<T>(IValidationContext context)
    {
        store[typeof(T)] = context;
    }

    public IValidationContext? Get<T>()
    {
        return store.TryGetValue(typeof(T), out var value)
            ? value as ValidationContext<T>
            : null;
    }

    public Dictionary<string, List<object>>? CreateWarningsList()
    {
        var warnings = new Dictionary<string, List<object>>();
        foreach (var context in store.Values)
        {
            if (context is not IValidationContext validationContext)
                continue;

            var contextWarnings = validationContext.BuildWarnings();
            if (contextWarnings == null)
                continue;

            foreach (var kvp in contextWarnings)
            {
                if (!warnings.TryGetValue(kvp.Key, out var list))
                {
                    list = new List<object>();
                    warnings[kvp.Key] = list;
                }
                list.AddRange(kvp.Value);
            }
        }
        return warnings.Count > 0 ? warnings : null;
    }
}