using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Data.SqlClient;
using EF.Frameworks.Common.ConfigurationEF;
using EF.Frameworks.Common.DataEF.SqlClientEF;

namespace POCO_Demo
{
    /// <summary>
    /// Map datareader to object. IL code auto emits on demand.
    /// </summary>    
    internal static class ObjectAssembler<T>
    {        
        public static T Create(SqlDataReader dr)
        {
            if (dr.Read())
                return _assembler(dr);

            return default(T);
        }

        public static T Create(IConfigurationContext config, IEFSqlCommand cmd)
        {
            using (var sm = new SqlConnectionManager())
            using (var dr = sm.ExecuteReader(cmd, config))
            {
                return Create(dr);
            }
        }

        public static IEnumerable<T> CreateList(SqlDataReader dr)
        {
            var lst = new List<T>();

            while (dr.Read())
            {
                var itm = _assembler(dr);
                lst.Add(itm);
            }

            return lst;
        }

        public static IEnumerable<T> CreateList(IConfigurationContext config, IEFSqlCommand cmd)
        {
            using (var sm = new SqlConnectionManager())
            using (var dr = sm.ExecuteReader(cmd, config))
            {
                return CreateList(dr);
            }
        }

        private static readonly Func<SqlDataReader, T> _assembler;

        //magic happens here
        static ObjectAssembler()
        {
            var type = typeof(T);
            if (type.IsPrimitive)
            {
                _assembler = ObjectAssemblerHelper.CreateScalarAssembler<T>();
            }
            else if (type.IsGenericType && type.Name.StartsWith("Tuple`"))
            {
                _assembler = ObjectAssemblerHelper.CreateTupleAssembler<T>();
            }
            else
            {
                _assembler = ObjectAssemblerHelper.CreateObjectAssembler<T>();
            }
        }
    }

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

        public static Func<SqlDataReader, T> CreateObjectAssembler<T>()
        {
            var type = typeof(T);
            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty);

            var dr = Expression.Parameter(typeof(SqlDataReader), "dr");
            var itm = Expression.Parameter(type, "itm");

            Expression body = Expression.Block(
                                    new[] { itm },
                                    Expression.Assign(itm, Expression.New(type))
                                    .Union(props.Where(_ => _.CanWrite && !_.GetSetMethod(true).IsPrivate)
                                                .Select((prop, index) =>
                                                {
                                                    var name = prop.Name;
                                                    var ptype = prop.PropertyType;

                                                    var method = ObjectAssemblerHelper.GetMethod(ptype, "Name");

                                                    return Expression.Assign(
                                                        Expression.Property(itm, name),
                                                        Expression.Call(null, method, dr,
                                                        Expression.Constant(name)));
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