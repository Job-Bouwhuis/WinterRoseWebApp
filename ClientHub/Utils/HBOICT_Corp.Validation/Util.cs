using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace WinterRose.Web.Validation;

internal class Util
{
    public static string GetPropertyName<T, TProp>(Expression<Func<T, TProp>> expr)
    {
        return GetPropertyInfo(expr).Name;
    }

    public static PropertyInfo GetPropertyInfo<T, Prop>(Expression<Func<T, Prop>> expr)
    {
        if (expr.Body is MemberExpression member)
        {
            if (member.Member is PropertyInfo prop)
                return prop;
        }

        if (expr.Body is UnaryExpression unary && unary.Operand is MemberExpression innerMember)
        {
            if (innerMember.Member is PropertyInfo prop)
                return prop;
        }

        throw new InvalidOperationException("Expression must point to a property.");
    }
}
