using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;

namespace POCO_Demo
{
    static class ExpressionHelper
    {
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
