using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace POCO_Demo
{
    public static class TypeHelper
    {
        public static bool IsSqlSupportedType(Type type)
        {
            if (IsNullable(type))
                return IsSqlSupportedType(Nullable.GetUnderlyingType(type));

            return type.IsPrimitive || type == TypeCache<string>.Type || type == TypeCache<DateTime>.Type;
        }

        public static bool IsTuple(Type type)
        {
            return type.IsGenericType && type.Name.StartsWith("Tuple`");
        }

        public static bool IsNullable(Type type)
        {
            return (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        //helper to call private methods
        public static object InvokeAny(this object obj, string methodName, params object[] args)
        {
            var types = args.Select(_ => _.GetType()).ToArray();
            MethodInfo method = obj.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, types, null);
            return method.Invoke(obj, args);
        }
    }
}
