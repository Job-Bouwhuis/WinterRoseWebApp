namespace WinterRose.Web.Validation;

/// <summary>
/// Allows a DTO object to be validated only partially.<br></br>
/// Meaning that any rule that defines a property as required will
/// not be applied.<br></br>
/// This is useful for update operations.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public class PartialAttribute : Attribute
{
}