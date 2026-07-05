using System.Runtime.InteropServices;

namespace WinterRose.DependancyInjection;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class LinuxOnlyAttribute() : OSConstraintAttribute(OSPlatform.Linux);