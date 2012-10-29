using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;

namespace POCO_Demo
{
    public static class ExpressionHelper
    {
        public static string GetMethodName(LambdaExpression expression)
        {
            return (((((expression.Body as UnaryExpression).Operand as MethodCallExpression).Arguments.Last()) as
                 ConstantExpression).Value as MethodInfo).Name;
        }

        public static PropertyInfo GetPropertyInfo(LambdaExpression expr)
        {
            return (PropertyInfo)((MemberExpression)expr.Body).Member;
        }

        public static string GetPropertyName(LambdaExpression expr)
        {
            return GetPropertyInfo(expr).Name;
        }
    }
}