namespace WinterRose.DependancyInjection;

[AttributeUsage(AttributeTargets.Parameter)]
public class SpecificallyAttribute<T> : Attribute
{
}