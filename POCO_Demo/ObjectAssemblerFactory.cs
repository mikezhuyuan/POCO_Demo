using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace POCO_Demo
{
    public abstract class ObjectAssemblerFactory
    {
        public ObjectAssemblerFactory()
        {
            _builder = new MappingBuilder();
            OnMapping(_builder);
        }

        protected abstract void OnMapping(MappingBuilder builder);

        public IObjectAssembler<T> Get<T>()
        {
            object result = null;
            if (!_assemblers.TryGetValue(typeof(T), out result))
            {

                _assemblers[typeof(T)] = result = new ObjectAssembler<T>(_builder.GetPropertyNameMap<T>(), _builder.GetPropertyValueMap<T>(), _builder.GetIgnoredProperties<T>());
            }

            return (IObjectAssembler<T>)result;
        }

        private Dictionary<Type, object> _assemblers = new Dictionary<Type, object>();
        private MappingBuilder _builder;
    }
}
