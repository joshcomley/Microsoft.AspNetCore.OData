using System;
using System.Collections.Generic;
using Iql.Queryable.Data.EntityConfiguration.Rules;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Validation
{
    public class EntityPropertyRuleMap<TEntity, TRule>
    where TRule : Rule<TEntity>
    {
        public IReadOnlyList<TRule> Rules => _rules.AsReadOnly();
        public string PropertyName { get; }
        public Type EntityType => typeof(TEntity);

        private readonly List<TRule> _rules = new List<TRule>();

        public EntityPropertyRuleMap(string propertyName)
        {
            PropertyName = propertyName;
        }
        public virtual void AddRule(TRule validationExpression)
        {
            _rules.Add(validationExpression);
        }
    }
}