using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace POCO_Demo
{
    public class EntityMapBuilder<TEntity> where TEntity : class, new()
    {
        public EntityMapBuilder(MappingBuilder mappingBuilder)
        {
            _mappingBuilder = mappingBuilder;
        }

        public EntityMapBuilder<TEntity> PropertyName<TProperty>(Expression<Func<TEntity, TProperty>> expr, string column)
        {
            _mappingBuilder.MapPropertyName<TEntity>(ExpressionHelper.GetPropertyName(expr), column);

            return this;
        }

        public EntityMapBuilder<TEntity> PropertyValue<TFrom, TProperty>(Expression<Func<TEntity, TProperty>> expr, string column, Func<TFrom, TProperty> exec)
        {
            if (!string.IsNullOrWhiteSpace(column))
                _mappingBuilder.MapPropertyName<TEntity>(ExpressionHelper.GetPropertyName(expr), column);

            _mappingBuilder.MapPropertyValue<TEntity>(ExpressionHelper.GetPropertyName(expr), exec);

            return this;
        }

        public EntityMapBuilder<TEntity> IgnoreProperty<TProperty>(Expression<Func<TEntity, TProperty>> expr)
        {
            _mappingBuilder.IgnoreProperty<TEntity>(ExpressionHelper.GetPropertyName(expr));

            return this;
        }

        private MappingBuilder _mappingBuilder;
    }
}
