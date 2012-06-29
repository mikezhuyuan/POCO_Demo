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
    internal class ObjectAssembler<T> : IObjectAssembler<T>
    {
        public T Create(SqlDataReader dr)
        {
            if (dr.Read())
                return _assembler(dr);

            return default(T);
        }

        public T Create(IConfigurationContext config, IEFSqlCommand cmd)
        {
            using (var sm = new SqlConnectionManager())
            using (var dr = sm.ExecuteReader(cmd, config))
            {
                return Create(dr);
            }
        }

        public IEnumerable<T> CreateList(SqlDataReader dr)
        {
            var lst = new List<T>();

            while (dr.Read())
            {
                var itm = _assembler(dr);
                lst.Add(itm);
            }

            return lst;
        }

        public IEnumerable<T> CreateList(IConfigurationContext config, IEFSqlCommand cmd)
        {
            using (var sm = new SqlConnectionManager())
            using (var dr = sm.ExecuteReader(cmd, config))
            {
                return CreateList(dr);
            }
        }

        private readonly Func<SqlDataReader, T> _assembler;

        public ObjectAssembler(Dictionary<string, string> columnNameMap, Dictionary<string, Delegate> columnValueMap, List<string> ignoredProperties)
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
                _assembler = ObjectAssemblerHelper.CreateObjectAssembler<T>(columnNameMap, columnValueMap, ignoredProperties);
            }
        }
    }
}