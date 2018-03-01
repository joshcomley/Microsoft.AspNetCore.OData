using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Validation
{
    public class EntityValidationMap<TEntity> : IEntityValidationMap
    {
        private readonly Dictionary<string, EntityPropertyValidationMap<TEntity>> _properties = new Dictionary<string, EntityPropertyValidationMap<TEntity>>();
        private readonly List<EntityValidation<TEntity>> _validationExpressions = new List<EntityValidation<TEntity>>();

        public IEnumerable<EntityPropertyValidationMap<TEntity>> PropertyValidations => _properties.Values;
        public IReadOnlyList<EntityValidation<TEntity>> EntityValidations => _validationExpressions.AsReadOnly();

        public void AddValidation(
            EntityValidation<TEntity> validationExpression, 
            string propertyName = null)
        {
            if (propertyName == null)
            {
                _validationExpressions.Add(validationExpression);
                return;
            }
            if (typeof(TEntity).GetRuntimeProperties().All(p => p.Name != propertyName))
            {
                throw new ArgumentException($"No property \"{propertyName}\" found on type \"{typeof(TEntity).Name}\"");
            }
            if (!_properties.ContainsKey(propertyName))
            {
                _properties.Add(propertyName, new EntityPropertyValidationMap<TEntity>(propertyName));
            }
            _properties[propertyName].AddValidation(validationExpression);
        }

        void IEntityValidationMap.AddValidation(IEntityValidation validationExpression, string propertyName = null)
        {
            AddValidation((EntityValidation<TEntity>)validationExpression, propertyName);
        }
    }
}