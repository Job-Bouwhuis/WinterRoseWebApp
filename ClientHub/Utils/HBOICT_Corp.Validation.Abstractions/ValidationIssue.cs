namespace WinterRose.Web.Validation.Issues
{
    /// <summary>
    /// Represents a validation issue that can occur during the validation process.
    /// </summary>
    public class ValidationIssue
    {
        /// <summary>
        /// The error code associated with the validation issue.
        /// </summary>
        public string Code { get; }
        /// <summary>
        /// A short description of the validation issue.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The DTO field associated with the validation issue, if applicable. this property is not included with JSON.
        /// </summary>
        /// <remarks>
        /// The set accessor is meant for <see cref="Validation{T}.ApplyFrom{TProp}(System.Linq.Expressions.Expression{Func{T, TProp}}, TProp, string?, bool)"/> so that
        /// the field can be set based on the provided TProp value. Allowing the problem to state the field that caused the issue
        /// instead of the field that was used to take the rules to validate with.
        /// </remarks>
        public string Field { get; set; }
        /// <summary>
        /// The severity of this issue
        /// </summary>
        public ValidationSeverity Severity { get; }

        public ValidationIssue(
            string code,
            ValidationSeverity severity,
            string message,
            string field = null)
        {
            Code = code;
            Severity = severity;
            Message = message;
            Field = field;
        }

        /// <summary>
        /// Create an error validation issue
        /// </summary>
        /// <param name="code">The error code associated with the validation issue.</param>
        /// <param name="message">A short description of the validation issue.</param>
        /// <param name="field">The DTO field associated with the validation issue, if applicable.</param>
        /// <returns>A <see cref="ValidationIssue"/> representing the error.</returns>
        public static ValidationIssue Error(string code, string message, string field = null)
            => new ValidationIssue(code, ValidationSeverity.Error, message, field);

        /// <summary>
        /// Creates a new validation issue with warning severity.
        /// </summary>
        /// <param name="code">The unique code that identifies the type of validation warning.</param>
        /// <param name="message">The message that describes the validation warning.</param>
        /// <param name="field">The name of the field associated with the warning, or null if the warning is not field-specific.</param>
        /// <returns>A <see cref="ValidationIssue"/> instance representing a warning with the specified code, message, and optional
        /// field.</returns>
        public static ValidationIssue Warning(string code, string message, string field = null)
            => new ValidationIssue(code, ValidationSeverity.Warning, message, field);
    }

}

