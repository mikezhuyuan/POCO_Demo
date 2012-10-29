using System;
using System.Collections.Generic;
using System.Data;

namespace POCO_Demo
{
    internal class ObjectAssembler<T> : IObjectAssembler<T>
    {
        public T Create(IDataReader dr)
        {
            int[] ords = null;

            if (dr.Read())
            {
                if (ords == null)
                    ords = _getOrdinals(dr);

                return _assembler(dr, ords);
            }

            return default(T);
        }

        public IEnumerable<T> CreateList(IDataReader dr)
        {
            var lst = new List<T>();
            int[] ords = null;

            while (dr.Read())
            {
                if (ords == null)
                    ords = _getOrdinals(dr);

                var itm = _assembler(dr, ords);
                lst.Add(itm);
            }

            return lst;
        }

        private readonly Func<IDataReader, int[], T> _assembler;
        private readonly Func<IDataReader, int[]> _getOrdinals;

        public ObjectAssembler(Dictionary<string, string> columnNameMap, Dictionary<string, Delegate> columnValueMap, List<string> ignoredProperties, Delegate creator)
        {
            if (columnNameMap == null)
                columnNameMap = new Dictionary<string, string>();

            if (columnValueMap == null)
                columnValueMap = new Dictionary<string, Delegate>();

            if (ignoredProperties == null)
                ignoredProperties = new List<string>();

            _getOrdinals = ObjectAssemblerHelper.CreateGetOrdinals<T>(columnNameMap, columnValueMap, ignoredProperties);
            _assembler = ObjectAssemblerHelper.CreateObjectAssembler<T>(columnNameMap, columnValueMap, ignoredProperties, creator);
        }
    }

    internal abstract class DefaultAssembler<T> : IObjectAssembler<T>
    {
        public T Create(IDataReader dr)
        {
            if (dr.Read())
                return _assembler(dr);

            return default(T);
        }

        public IEnumerable<T> CreateList(IDataReader dr)
        {
            var lst = new List<T>();

            while (dr.Read())
            {
                var itm = _assembler(dr);
                lst.Add(itm);
            }

            return lst;
        }

        protected Func<IDataReader, T> _assembler;
    }

    internal class ScalarAssembler<T> : DefaultAssembler<T>
    {
        public ScalarAssembler()
        {
            _assembler = ObjectAssemblerHelper.CreateScalarAssembler<T>();
        }
    }

    internal class TupleAssembler<T> : DefaultAssembler<T>
    {
        public TupleAssembler()
        {
            _assembler = ObjectAssemblerHelper.CreateTupleAssembler<T>();
        }
    }
}
