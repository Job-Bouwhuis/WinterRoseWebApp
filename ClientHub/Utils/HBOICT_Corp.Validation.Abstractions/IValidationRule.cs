using System.Threading.Tasks;

namespace WinterRose.Web.Validation.Rules
{
    /// <summary>
    /// Defines a validation rule for an object of type T. 
    /// Implementations of this interface should provide the logic to validate 
    /// the object and report any validation issues.
    /// </summary>
    /// <typeparam name="T">The type of the object to be validated</typeparam>
    public interface IValidationRule
    {
        string FieldName { get; set; }
        Task Validate(IValidationContext context);
        Task ValidateValue(object value, IValidationContext context);
    }
}
