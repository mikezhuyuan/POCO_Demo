using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Data;

namespace POCO_Demo
{
    internal static class ObjectAssemblerHelper
    {
        public static IEnumerable<Expression> Union(params object[] objs)
        {
            IEnumerable<Expression> result = new Expression[0];
            foreach (var obj in objs)
            {
                if (obj is Expression)
                    result = result.Union(new[] { (Expression)obj });
                else if (obj is IEnumerable<Expression>)
                    result = result.Union((IEnumerable<Expression>)obj);
            }

            return result;
        }

        public static MethodInfo GetMethod(Type propertyType, string methodSuffix)
        {
            bool nullable = TypeHelper.IsNullable(propertyType);
            if (!methodSuffix.StartsWith("By"))
                methodSuffix = "By" + methodSuffix;

            if (nullable)
                propertyType = Nullable.GetUnderlyingType(propertyType);

            if (TypeHelper.IsSqlSupportedType(propertyType))
            {
                if (nullable)
                    return typeof(ObjectAssemblerHelper).GetMethod("GetNullablePrimitive" + methodSuffix).MakeGenericMethod(propertyType);

                return typeof(ObjectAssemblerHelper).GetMethod("GetPrimitive" + methodSuffix).MakeGenericMethod(propertyType);
            }

            throw new ArgumentException("Type is not supported: " + propertyType + ". Method suffix: " + methodSuffix);
        }

        public static T GetPrimitiveByName<T>(IDataReader dr, string name)
        {
            int ordinal = dr.GetOrdinal(name);

            try
            {
                return GetPrimitiveByOrdinal<T>(dr, ordinal);
            }
            catch (InvalidDataCastException ex)
            {
                ex.ColumnName = name;
                throw ex;
            }
        }

        public static T GetPrimitiveByOrdinal<T>(IDataReader dr, int ordinal)
        {
            if (dr.IsDBNull(ordinal))
                return default(T);

            var data = dr.GetValue(ordinal);

            try
            {
                return (T)data;
            }
            catch (InvalidCastException ex)
            {
                throw new InvalidDataCastException(data.GetType(), TypeCache<T>.Type, ex) { ColumnOrdinal = ordinal };
            }
        }

        //todo: check if need InvalidDataCastException
        public static T? GetNullablePrimitiveByName<T>(IDataReader dr, string name) where T : struct
        {
            int ordinal = dr.GetOrdinal(name);

            return GetNullablePrimitiveByOrdinal<T>(dr, ordinal);
        }

        public static T? GetNullablePrimitiveByOrdinal<T>(IDataReader dr, int ordinal) where T : struct
        {
            return dr.GetValue(ordinal) as Nullable<T>;
        }

        public static Func<IDataReader, int[]> CreateGetOrdinals<T>(Dictionary<string, string> columnNameMap, Dictionary<string, Delegate> columnValueMap, List<string> ignoredProperties)
        {
            var type = TypeCache<T>.Type;
            var props = GetPropertiesToAssemble(ignoredProperties, type);
            var dr = Expression.Parameter(TypeCache<IDataReader>.Type, "dr");
            var getOrdinal = typeof(IDataRecord).GetMethod("GetOrdinal");
            var ords = props.Select((prop, index) => Expression.Variable(typeof(int), "ord" + index)).ToArray();

            Expression body = Expression.Block(
                ords,
                Union(
                    ords.Select((ord, index) =>
                        Expression.Assign(ord,
                            Expression.Call(dr, getOrdinal,
                                Expression.Constant(GetColumnName(columnNameMap, props[index].Name))))),
                    NewArrayExpression.NewArrayInit(typeof(int), ords)
                ));

            return Expression.Lambda<Func<IDataReader, int[]>>(body, new[] { dr }).Compile();
        }

        public static Func<IDataReader, int[], T> CreateObjectAssembler<T>(Dictionary<string, string> columnNameMap, Dictionary<string, Delegate> columnValueMap, List<string> ignoredProperties, Delegate creator)
        {
            var type = TypeCache<T>.Type;
            var props = GetPropertiesToAssemble(ignoredProperties, type);
            var dr = Expression.Parameter(TypeCache<IDataReader>.Type, "dr");
            var ords = Expression.Parameter(typeof(int[]), "ords");
            var itm = Expression.Parameter(type, "itm");

            Expression body = Expression.Block(
                new[] { itm },
                Union(
                creator == null ? Expression.Assign(itm, Expression.New(type)) : Expression.Assign(itm, Expression.Call(creator.Target == null ? null : Expression.Constant(creator.Target), creator.Method, dr)),
                    props.Select((prop, index) =>
                    {
                        var name = prop.Name;
                        var ptype = prop.PropertyType;

                        Expression getValueExp;
                        
                        if (columnValueMap.ContainsKey(name))
                        {
                            var getValue = columnValueMap[name].Method;
                            var method = ObjectAssemblerHelper.GetMethod(getValue.GetParameters()[0].ParameterType, "Ordinal");
                            getValueExp = Expression.Call(null, getValue,
                                            Expression.Call(null, method, dr,
                                                Expression.ArrayAccess(ords, Expression.Constant(index))));
                        }
                        else
                        {
                            var method = ObjectAssemblerHelper.GetMethod(ptype, "Ordinal");
                            getValueExp = Expression.Call(null, method, dr, Expression.ArrayAccess(ords, Expression.Constant(index)));
                        }

                        return Expression.Assign(Expression.Property(itm, name), getValueExp);
                    }),
                    itm));

            return Expression.Lambda<Func<IDataReader, int[], T>>(body, new[] { dr, ords }).Compile();
        }

        public static Func<IDataReader, T> CreateScalarAssembler<T>()
        {
            return _ =>
            {
                var data = _[0];
                try
                {
                    return (T)data;
                }
                catch (InvalidCastException ex)
                {
                    throw new InvalidDataCastException(data.GetType(), typeof(T), ex) { ColumnOrdinal = 0 };
                }
            };
        }

        public static Func<IDataReader, T> CreateTupleAssembler<T>()
        {
            var type = TypeCache<T>.Type;
            var dr = Expression.Parameter(TypeCache<IDataReader>.Type, "dr");

            Expression body = Expression.New(type.GetConstructors()[0], type.GetGenericArguments().Select((t, index) =>
            {
                var method = ObjectAssemblerHelper.GetMethod(t, "Ordinal");

                return Expression.Call(null, method, dr, Expression.Constant(index));
            }).ToArray());

            return Expression.Lambda<Func<IDataReader, T>>(body, new[] { dr }).Compile();
        }

        private static PropertyInfo[] GetPropertiesToAssemble(List<string> ignoredProperties, Type type)
        {
            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty)
                            .Where(_ => _.CanWrite && !_.GetSetMethod(true).IsPrivate && !ignoredProperties.Contains(_.Name))
                            .ToArray();

            return props;
        }

        private static string GetColumnName(Dictionary<string, string> columnNameMap, string propName)
        {
            string col = null;
            if (!columnNameMap.TryGetValue(propName, out col))
                col = propName;

            return col;
        }
    }
}
