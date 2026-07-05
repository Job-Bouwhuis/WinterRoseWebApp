using System.Runtime.InteropServices;

namespace WinterRose.DependancyInjection;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class OSConstraintAttribute : Attribute
{
    public OSPlatform[] Platforms { get; }

    public OSConstraintAttribute(params OSPlatform[] platforms)
    {
        Platforms = platforms;
    }
}