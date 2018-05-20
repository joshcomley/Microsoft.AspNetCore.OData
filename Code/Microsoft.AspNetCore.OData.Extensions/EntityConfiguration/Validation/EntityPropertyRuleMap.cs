using System;
using System.Collections.Generic;
using Iql.Entities.Rules;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Validation
{
    public class EntityPropertyRuleMap<TEntity, TRule>
    where TRule : IRule
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