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
    internal static class ObjectAssembler<T> where T : class
    {        
        public static T Create(SqlDataReader dr)
        {
            if (dr.Read())
                return _assemble(dr);

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
                var itm = _assemble(dr);
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

        private static readonly Func<SqlDataReader, T> _assemble;

        //magic happens here
        static ObjectAssembler()
        {
            if (typeof(T).IsPrimitive)
            {
                _assemble = _ => (T)_[0];
                return;
            }

            var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty);

            var dr = Expression.Parameter(typeof(SqlDataReader), "dr");
            var itm = Expression.Parameter(typeof(T), "itm");

            Expression body = Expression.Block(
                                    new[] { itm },
                                    Expression.Assign(itm, Expression.New(typeof(T)))
                                    .Union(props.Where(_ => _.CanWrite && !_.GetSetMethod(true).IsPrivate)
                                                .Select((prop, index) =>
                                                {
                                                    var name = prop.Name;
                                                    var ptype = prop.PropertyType;

                                                    var method = ObjectAssemblerHelper.GetMethod(ptype);

                                                    return Expression.Assign(
                                                        Expression.Property(itm, name),
                                                        Expression.Call(null, method, dr,
                                                                        Expression.Constant(name)));
                                                }))
                                    .Union(itm));

            _assemble = Expression.Lambda<Func<SqlDataReader, T>>(body, new[] { dr }).Compile();
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

        public static MethodInfo GetMethod(Type propertyType)
        {
            bool nullable = IsNullable(propertyType);

            if (nullable)
                propertyType = Nullable.GetUnderlyingType(propertyType);

            if (propertyType == typeof(string))
            {
                return typeof(ObjectAssemblerHelper).GetMethod("GetString");
            }

            if (!nullable && propertyType == typeof(DateTime))
            {
                return typeof(ObjectAssemblerHelper).GetMethod("GetDateTime");
            }

            if (propertyType.IsValueType)
            {
                if (nullable)
                    return typeof(ObjectAssemblerHelper).GetMethod("GetNullablePrimitive").MakeGenericMethod(propertyType);

                return typeof(ObjectAssemblerHelper).GetMethod("GetPrimitive").MakeGenericMethod(propertyType);
            }

            throw new ArgumentException();
        }

        private static bool IsNullable(Type type)
        {
            return (type.IsGenericType && type.
              GetGenericTypeDefinition().Equals
              (typeof(Nullable<>)));
        }

        public static T GetPrimitive<T>(SqlDataReader dr, string name) where T : struct
        {
            int ordinal = dr.GetOrdinal(name);
            if (dr.IsDBNull(ordinal))
                return default(T);

            return (T)dr.GetValue(ordinal);
        }

        public static T? GetNullablePrimitive<T>(SqlDataReader dr, string name) where T : struct
        {
            int ordinal = dr.GetOrdinal(name);
            return dr.GetValue(ordinal) as Nullable<T>;
        }

        public static string GetString(SqlDataReader dr, string name)
        {
            int ordinal = dr.GetOrdinal(name);
            if (dr.IsDBNull(ordinal))
                return string.Empty;

            return ((string)dr.GetValue(ordinal)).TrimEnd(' ');
        }

        private static readonly DateTime DefaultDateTime = new DateTime(1800, 1, 1);
        public static DateTime GetDateTime(SqlDataReader dr, string name)
        {
            int ordinal = dr.GetOrdinal(name);
            if (dr.IsDBNull(ordinal))
                return DefaultDateTime;

            return (DateTime)dr.GetValue(ordinal);
        }
    }
}