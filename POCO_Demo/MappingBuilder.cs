using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace POCO_Demo
{
    public class MappingBuilder
    {
        public EntityMapBuilder<T> Entity<T>() where T : class, new()
        {
            var type = typeof(T);
            if (!_propertyNameMaps.ContainsKey(type))
                _propertyNameMaps[type] = new Dictionary<string, string>();

            if (!_propertyValueMaps.ContainsKey(type))
                _propertyValueMaps[type] = new Dictionary<string, Delegate>();

            if (!_ignoredPropertiesMap.ContainsKey(type))
                _ignoredPropertiesMap[type] = new List<string>();

            return new EntityMapBuilder<T>(this);
        }

        public void MapPropertyName<T>(string propertyName, string columnName)
        {
            var type = typeof(T);

            _propertyNameMaps[type][propertyName] = columnName;
        }

        public void MapPropertyValue<T>(string propertyName, Delegate exec)
        {
            var type = typeof(T);

            _propertyValueMaps[type][propertyName] = exec;
        }

        public void IgnoreProperty<T>(string propertyName)
        {
            var type = typeof(T);

            _ignoredPropertiesMap[type].Add(propertyName);
        }

        public Dictionary<string, string> GetPropertyNameMap<T>()
        {
            var type = typeof(T);

            Dictionary<string, string> result = null;

            _propertyNameMaps.TryGetValue(type, out result);

            return result;
        }

        public Dictionary<string, Delegate> GetPropertyValueMap<T>()
        {
            var type = typeof(T);

            Dictionary<string, Delegate> result = null;

            _propertyValueMaps.TryGetValue(type, out result);

            return result;
        }

        public List<string> GetIgnoredProperties<T>()
        {
            var type = typeof(T);

            List<string> result = null;

            _ignoredPropertiesMap.TryGetValue(type, out result);

            return result;
        }

        private readonly Dictionary<Type, Dictionary<string, string>> _propertyNameMaps = new Dictionary<Type, Dictionary<string, string>>();
        private readonly Dictionary<Type, Dictionary<string, Delegate>> _propertyValueMaps = new Dictionary<Type, Dictionary<string, Delegate>>();
        private readonly Dictionary<Type, List<string>> _ignoredPropertiesMap = new Dictionary<Type, List<string>>();
    }
}
