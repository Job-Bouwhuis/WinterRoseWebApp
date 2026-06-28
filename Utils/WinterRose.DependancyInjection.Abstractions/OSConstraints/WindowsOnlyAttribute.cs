using System.Runtime.InteropServices;

namespace WinterRose.DependancyInjection;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class WindowsOnlyAttribute() : OSConstraintAttribute(OSPlatform.Windows);