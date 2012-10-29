using System;
using System.Collections.Generic;
using System.Data;

namespace POCO_Demo
{
    public class MappingBuilder
    {
        public EntityMapBuilder<T> Entity<T>() where T : class
        {
            var type = TypeCache<T>.Type;
            if (!_propertyNameMap.ContainsKey(type))
                _propertyNameMap[type] = new Dictionary<string, string>();

            if (!_propertyValueMap.ContainsKey(type))
                _propertyValueMap[type] = new Dictionary<string, Delegate>();

            if (!_ignoredPropertiesMap.ContainsKey(type))
                _ignoredPropertiesMap[type] = new List<string>();

            return new EntityMapBuilder<T>(this);
        }

        public void Create<T>(Func<IDataReader, T> creator)
        {
            var type = TypeCache<T>.Type;

            _creatorMap[type] = creator;
        }

        public void MapPropertyName<T>(string propertyName, string columnName)
        {
            var type = TypeCache<T>.Type;

            _propertyNameMap[type][propertyName] = columnName;
        }

        public void MapPropertyValue<T>(string propertyName, Delegate exec)
        {
            var type = TypeCache<T>.Type;

            _propertyValueMap[type][propertyName] = exec;
        }

        public void IgnoreProperty<T>(string propertyName)
        {
            var type = TypeCache<T>.Type;

            _ignoredPropertiesMap[type].Add(propertyName);
        }

        public Dictionary<string, string> GetPropertyNameMap<T>()
        {
            var type = TypeCache<T>.Type;

            Dictionary<string, string> result = null;

            _propertyNameMap.TryGetValue(type, out result);

            return result;
        }

        public Dictionary<string, Delegate> GetPropertyValueMap<T>()
        {
            var type = TypeCache<T>.Type;

            Dictionary<string, Delegate> result = null;

            _propertyValueMap.TryGetValue(type, out result);

            return result;
        }

        public List<string> GetIgnoredProperties<T>()
        {
            var type = TypeCache<T>.Type;

            List<string> result = null;

            _ignoredPropertiesMap.TryGetValue(type, out result);

            return result;
        }

        public Delegate GetCreator<T>()
        {
            var type = TypeCache<T>.Type;

            Delegate result = null;

            _creatorMap.TryGetValue(type, out result);

            return result;
        }

        private readonly Dictionary<Type, Delegate> _creatorMap = new Dictionary<Type, Delegate>();
        private readonly Dictionary<Type, Dictionary<string, string>> _propertyNameMap = new Dictionary<Type, Dictionary<string, string>>();
        private readonly Dictionary<Type, Dictionary<string, Delegate>> _propertyValueMap = new Dictionary<Type, Dictionary<string, Delegate>>();
        private readonly Dictionary<Type, List<string>> _ignoredPropertiesMap = new Dictionary<Type, List<string>>();
    }
}
