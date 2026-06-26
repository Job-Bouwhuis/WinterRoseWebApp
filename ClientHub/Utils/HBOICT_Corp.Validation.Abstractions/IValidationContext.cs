using WinterRose.Web.Validation.Issues;
using System.Collections.Generic;

namespace WinterRose.Web.Validation
{
    /// <summary>
    /// Provides an interface for a validation context that tracks validation issues,
    /// errors, and warnings during the validation process.
    /// </summary>
    public interface IValidationContext
    {
        object Value { get; }

        /// <summary>
        /// Gets a value indicating whether the current object contains any errors.
        /// </summary>
        bool HasErrors { get; }
        /// <summary>
        /// Gets a value indicating whether the current object contains any warnings.
        /// </summary>
        bool HasWarnings { get; }
        /// <summary>
        /// Gets the collection of validation issues detected during the validation process.
        /// </summary>
        IReadOnlyList<ValidationIssue> Issues { get; }

        /// <summary>
        /// Adds a validation issue to the current context.
        /// </summary>
        /// <param name="issue">The validation issue to add.</param>
        void AddIssue(ValidationIssue issue);
        /// <summary>
        /// Builds a dictionary of validation warnings, grouped by field, if any warnings are present in the context.
        /// </summary>
        /// <returns>A dictionary where the keys are field names and the values are lists of warning objects associated with each field, or null if no warnings are present.</returns>
        Dictionary<string, List<object>> BuildWarnings();
        /// <summary>
        /// Validates the current object and throws an exception if it is in an invalid state.
        /// </summary>
        /// <remarks>Call this method to ensure that the object meets all required validation criteria before
        /// proceeding with operations that depend on its validity. The specific exception type and validation rules depend
        /// on the implementation.</remarks>
        void ThrowIfInvalid();
    }
}
