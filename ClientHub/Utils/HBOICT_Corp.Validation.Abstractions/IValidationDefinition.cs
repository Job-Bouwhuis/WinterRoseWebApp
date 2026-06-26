namespace WinterRose.Web.Validation
{
    /// <summary>
    /// Defines a contract for creating validation definitions for a specific type <typeparamref name="T"/>. 
    /// Implementations of this interface are responsible for defining the validation rules that apply to instances
    /// of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IValidationDefinition<T>
    {
        /// <summary>
        /// Defines the validation rules for the specified <paramref name="validator"/>.
        /// </summary>
        /// <param name="validator">The validator instance to which the rules will be added.</param>
        void Define(IValidation<T> validator);
    }
}


