using System;
using System.Collections.Generic;
using System.Text;

namespace WinterRose.Web.Validation;

/// <summary>
/// A marker interface that indicates that a class can be validated using the validation framework.
/// </summary>
/// <remarks>DTO means "Data Transfer Object"</remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class ValidatedAttribute() : Attribute();
