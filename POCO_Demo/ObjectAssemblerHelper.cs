using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Reflection;

namespace POCO_Demo
{
    internal static class ObjectAssemblerHelper
    {
        public static IEnumerable<Expression> Union(this Expression exp, IEnumerable<Expression> list)
        {
            return new[] { exp }.Union(list);
        }

        public static IEnumerable<Expression> Union(this IEnumerable<Expression> list, Expression exp)
        {
            return list.Union(new[] { exp });
        }

        public static MethodInfo GetMethod(Type propertyType, string methodSuffix)
        {
            bool nullable = IsNullable(propertyType);
            if (!methodSuffix.StartsWith("By"))
                methodSuffix = "By" + methodSuffix;

            if (nullable)
                propertyType = Nullable.GetUnderlyingType(propertyType);

            if (propertyType == typeof(string))
            {
                return typeof(ObjectAssemblerHelper).GetMethod("GetString" + methodSuffix);
            }

            if (!nullable && propertyType == typeof(DateTime))
            {
                return typeof(ObjectAssemblerHelper).GetMethod("GetDateTime" + methodSuffix);
            }

            if (propertyType.IsValueType)
            {
                if (nullable)
                    return typeof(ObjectAssemblerHelper).GetMethod("GetNullablePrimitive" + methodSuffix).MakeGenericMethod(propertyType);

                return typeof(ObjectAssemblerHelper).GetMethod("GetPrimitive" + methodSuffix).MakeGenericMethod(propertyType);
            }

            throw new ArgumentException();
        }

        private static bool IsNullable(Type type)
        {
            return (type.IsGenericType && type.
              GetGenericTypeDefinition().Equals
              (typeof(Nullable<>)));
        }

        public static T GetPrimitiveByName<T>(SqlDataReader dr, string name) where T : struct
        {
            int ordinal = dr.GetOrdinal(name);
            return GetPrimitiveByOrdinal<T>(dr, ordinal);
        }

        public static T GetPrimitiveByOrdinal<T>(SqlDataReader dr, int ordinal) where T : struct
        {
            if (dr.IsDBNull(ordinal))
                return default(T);

            return (T)dr.GetValue(ordinal);
        }

        public static T? GetNullablePrimitiveByName<T>(SqlDataReader dr, string name) where T : struct
        {
            int ordinal = dr.GetOrdinal(name);
            return GetNullablePrimitiveByOrdinal<T>(dr, ordinal);
        }

        public static T? GetNullablePrimitiveByOrdinal<T>(SqlDataReader dr, int ordinal) where T : struct
        {
            return dr.GetValue(ordinal) as Nullable<T>;
        }

        public static string GetStringByName(SqlDataReader dr, string name)
        {
            int ordinal = dr.GetOrdinal(name);
            return GetStringByOrdinal(dr, ordinal);
        }

        public static string GetStringByOrdinal(SqlDataReader dr, int ordinal)
        {
            if (dr.IsDBNull(ordinal))
                return string.Empty;

            return ((string)dr.GetValue(ordinal)).TrimEnd(' ');
        }

        private static readonly DateTime DefaultDateTime = new DateTime(1800, 1, 1);
        public static DateTime GetDateTimeByName(SqlDataReader dr, string name)
        {
            int ordinal = dr.GetOrdinal(name);
            return GetDateTimeByOrdinal(dr, ordinal);
        }

        public static DateTime GetDateTimeByOrdinal(SqlDataReader dr, int ordinal)
        {
            if (dr.IsDBNull(ordinal))
                return DefaultDateTime;

            return (DateTime)dr.GetValue(ordinal);
        }

        public static Func<SqlDataReader, T> CreateObjectAssembler<T>(Dictionary<string, string> columnNameMap, Dictionary<string, Delegate> columnValueMap, List<string> ignoredProperties)
        {
            if (columnNameMap == null)
                columnNameMap = new Dictionary<string, string>();

            if (columnValueMap == null)
                columnValueMap = new Dictionary<string, Delegate>();

            if (ignoredProperties == null)
                ignoredProperties = new List<string>();

            var type = typeof(T);
            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty);

            var dr = Expression.Parameter(typeof(SqlDataReader), "dr");
            var itm = Expression.Parameter(type, "itm");

            Expression body = Expression.Block(
                                    new[] { itm },
                                    Expression.Assign(itm, Expression.New(type))
                                    .Union(props.Where(_ => _.CanWrite && !_.GetSetMethod(true).IsPrivate && !ignoredProperties.Contains(_.Name))
                                                .Select((prop, index) =>
                                                {
                                                    var name = prop.Name;
                                                    string col = null;
                                                    var ptype = prop.PropertyType;

                                                    if (!columnNameMap.TryGetValue(name, out col))
                                                        col = name;

                                                    Expression getValueExp;

                                                    if (columnValueMap.ContainsKey(name))
                                                    {
                                                        var getValue = columnValueMap[name].Method;
                                                        var method = ObjectAssemblerHelper.GetMethod(getValue.GetParameters()[0].ParameterType, "Name");
                                                        getValueExp = Expression.Call(null, getValue, Expression.Call(null, method, dr, Expression.Constant(col)));
                                                    }
                                                    else
                                                    {
                                                        var method = ObjectAssemblerHelper.GetMethod(ptype, "Name");
                                                        getValueExp = Expression.Call(null, method, dr, Expression.Constant(col));
                                                    }

                                                    return Expression.Assign(
                                                        Expression.Property(itm, name),
                                                        getValueExp);
                                                }))
                                    .Union(itm));

            return Expression.Lambda<Func<SqlDataReader, T>>(body, new[] { dr }).Compile();
        }

        public static Func<SqlDataReader, T> CreateScalarAssembler<T>()
        {
            return _ => (T)_[0];
        }

        public static Func<SqlDataReader, T> CreateTupleAssembler<T>()
        {
            var type = typeof(T);
            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty);

            var dr = Expression.Parameter(typeof(SqlDataReader), "dr");

            Expression body = Expression.New(type.GetConstructors()[0], type.GetGenericArguments().Select((t, index) =>
            {
                var method = ObjectAssemblerHelper.GetMethod(t, "Ordinal");

                return Expression.Call(null, method, dr, Expression.Constant(index));
            }).ToArray()
            );

            return Expression.Lambda<Func<SqlDataReader, T>>(body, new[] { dr }).Compile();
        }
    }
}
