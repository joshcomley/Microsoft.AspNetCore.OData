using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Display
{
    public class EntityDisplayTextFormatterMap<TEntity> : IEntityDisplayTextFormatterMap
    {
        private readonly Dictionary<string, EntityDisplayTextFormatter<TEntity>> _formatters
            = new Dictionary<string, EntityDisplayTextFormatter<TEntity>>();

        public const string DefaultKey = "Default";
        public EntityDisplayTextFormatter<TEntity> Default
        {
            get
            {
                if (Has(DefaultKey))
                {
                    return Get(DefaultKey);
                }

                return null;
            }
        }

        IEntityDisplayTextFormatter IEntityDisplayTextFormatterMap.Default => Default;

        public void SetDefault(Expression<Func<TEntity, string>> formatterExpression)
        {
            Set(formatterExpression, DefaultKey);
        }

        public void Set(Expression<Func<TEntity, string>> formatterExpression, string key)
        {
            if (!_formatters.ContainsKey(key))
            {
                _formatters.Add(key, null);
            }
            _formatters[key] = new EntityDisplayTextFormatter<TEntity>(formatterExpression);
        }

        public void Remove(string key)
        {
            _formatters.Remove(key);
        }

        public bool Has(string key)
        {
            return _formatters.ContainsKey(key);
        }

        public EntityDisplayTextFormatter<TEntity> Get(string key)
        {
            if (_formatters.ContainsKey(key))
            {
                return _formatters[key];
            }

            return null;
        }

        IEntityDisplayTextFormatter IEntityDisplayTextFormatterMap.Get(string key)
        {
            return Get(key);
        }

        void IEntityDisplayTextFormatterMap.Set(Expression formatter, string key)
        {
            Set((Expression<Func<TEntity, string>>)formatter, key);
        }
    }
}