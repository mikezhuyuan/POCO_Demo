using System;
using System.Collections.Generic;
using System.Data;
using EF.Frameworks.Common.ConfigurationEF;
using EF.Frameworks.Common.DataEF.SqlClientEF;

namespace POCO_Demo
{
    public abstract class ObjectAssemblerManager : IObjectAssemblerManager
    {
        public class MultipleResultAssembler
        {
            internal MultipleResultAssembler(ObjectAssemblerManager manager, IDataReader dataReader)
            {
                _manager = manager;
                _dataReader = dataReader;
            }

            public T Create<T>()
            {
                return _manager.Create<T>(_dataReader);
            }

            public IEnumerable<T> CreateList<T>()
            {
                return _manager.CreateList<T>(_dataReader);
            }

            private IDataReader _dataReader;

            private ObjectAssemblerManager _manager;
        }

        protected abstract void OnMapping(MappingBuilder builder);

        public IObjectAssembler<T> GetAssembler<T>()
        {
            var type = TypeCache<T>.Type;
            object result = null;

            lock (_locker)
            {
                if (!_assemblers.TryGetValue(type, out result))
                {
                    EnsureBuild();

                    if (TypeHelper.IsSqlSupportedType(type))
                    {
                        result = new ScalarAssembler<T>();
                    }
                    else if (TypeHelper.IsTuple(type))
                    {
                        result = new TupleAssembler<T>();
                    }
                    else
                    {
                        result = new ObjectAssembler<T>(_builder.GetPropertyNameMap<T>(),
                                                        _builder.GetPropertyValueMap<T>(),
                                                        _builder.GetIgnoredProperties<T>(), _builder.GetCreator<T>());
                    }

                    _assemblers.Add(TypeCache<T>.Type, result);
                }            
            }
            

            return (IObjectAssembler<T>)result;
        }

        /// <summary>
        /// facade methods
        /// </summary>
        public T Create<T>(IDataReader dr)
        {
            return GetAssembler<T>().Create(dr);
        }

        public T Create<T>(IConfigurationContext config, IEFSqlCommand cmd)
        {
            var assembler = GetAssembler<T>();

            using (cmd)
            using (var sm = new SqlConnectionManager())
            using (var dr = sm.ExecuteReader(cmd, config))
            {
                return assembler.Create(dr);
            }
        }

        public IEnumerable<T> CreateList<T>(IDataReader dr)
        {
            return GetAssembler<T>().CreateList(dr);
        }

        public IEnumerable<T> CreateList<T>(IConfigurationContext config, IEFSqlCommand cmd)
        {
            var assembler = GetAssembler<T>();

            using (cmd)
            using (var sm = new SqlConnectionManager())
            using (var dr = sm.ExecuteReader(cmd, config))
            {
                return assembler.CreateList(dr);
            }
        }

        /// <summary>
        /// Multi-DataResutls
        /// </summary>
        public void CreateMany(IDataReader dr, params Action<MultipleResultAssembler>[] assemblers)
        {
            var mrAssembler = new MultipleResultAssembler(this, dr);
            foreach (var assembler in assemblers)
            {
                assembler(mrAssembler);
                if (!dr.NextResult())
                    break;
            }
        }

        public void CreateMany(IConfigurationContext config, IEFSqlCommand cmd, params Action<MultipleResultAssembler>[] assemblers)
        {
            using (cmd)
            using (var sm = new SqlConnectionManager())
            using (var dr = sm.ExecuteReader(cmd, config))
            {
                CreateMany(dr, assemblers);
            }
        }

        #region Private
        private void EnsureBuild()
        {
            if (_builder == null)
            {
                _builder = new MappingBuilder();
                OnMapping(_builder);
            }
        }
        private Dictionary<Type, object> _assemblers = new Dictionary<Type, object>();
        private MappingBuilder _builder;
        private readonly object _locker = new object();
        #endregion
    }
}
