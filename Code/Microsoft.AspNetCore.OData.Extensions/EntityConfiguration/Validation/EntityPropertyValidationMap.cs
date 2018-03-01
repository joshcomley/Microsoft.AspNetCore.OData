using System;
using System.Collections.Generic;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Validation
{
    public class EntityPropertyValidationMap<TEntity>
    {
        public IReadOnlyList<EntityValidation<TEntity>> Validations => _validationExpressions.AsReadOnly();
        public string PropertyName { get; }
        public Type EntityType => typeof(TEntity);

        private readonly List<EntityValidation<TEntity>> _validationExpressions = new List<EntityValidation<TEntity>>();

        public EntityPropertyValidationMap(string propertyName)
        {
            PropertyName = propertyName;
        }
        public void AddValidation(EntityValidation<TEntity> validationExpression)
        {
            _validationExpressions.Add(validationExpression);
        }
    }
}