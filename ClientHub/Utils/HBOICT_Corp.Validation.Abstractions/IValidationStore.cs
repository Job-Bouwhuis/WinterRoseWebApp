using System.Collections.Generic;

namespace WinterRose.Web.Validation
{
    /// <summary>
    /// Defines a contract for a validation store that can hold validation contexts for different types.
    /// </summary>
    public interface IValidationStore
    {
        /// <summary>
        /// Sets the specified validation context for use in subsequent validation operations.
        /// </summary>
        /// <typeparam name="T">The type of the object being validated.</typeparam>
        /// <param name="context">The validation context to set. Provides information about the object and its environment for validation
        /// purposes. Cannot be null.</param>
        void Set<T>(IValidationContext context);

        /// <summary>
        /// Gets the current validation context for the specified type parameter.
        /// </summary>
        /// <typeparam name="T">The type for which the validation context is requested.</typeparam>
        /// <returns>A <see cref="ValidationContext{T}"/> instance representing the current validation context for type <typeparamref
        /// name="T"/>; or <see langword="null"/> if no context is available.</returns>
        IValidationContext Get<T>();
        /// <summary>
        /// Creates a dictionary containing warning messages grouped by category.
        /// </summary>
        /// <returns>A dictionary where each key is a warning category and the value is a list of warning details for that category;
        /// or null if there are no warnings.</returns>
        Dictionary<string, List<object>> CreateWarningsList();
    }
}
