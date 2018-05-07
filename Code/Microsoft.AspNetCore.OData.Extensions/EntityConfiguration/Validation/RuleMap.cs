using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Iql.Queryable.Data.EntityConfiguration.Rules;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Validation
{
    public class RuleMap<TEntity, TRule> : IRuleMap
        where TRule: Rule<TEntity>
    {
        private readonly Dictionary<string, EntityPropertyRuleMap<TEntity, TRule>> _properties = new Dictionary<string, EntityPropertyRuleMap<TEntity, TRule>>();
        private readonly List<TRule> _validationExpressions = new List<TRule>();

        public IEnumerable<EntityPropertyRuleMap<TEntity, TRule>> PropertyValidations => _properties.Values;
        public IReadOnlyList<TRule> EntityValidations => _validationExpressions.AsReadOnly();

        public virtual void AddRule(
            TRule validationExpression, 
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
                _properties.Add(propertyName, new EntityPropertyRuleMap<TEntity, TRule>(propertyName));
            }
            _properties[propertyName].AddRule(validationExpression);
        }

        void IRuleMap.AddRule(IRule validationExpression, string propertyName = null)
        {
            AddRule((TRule)validationExpression, propertyName);
        }
    }
}