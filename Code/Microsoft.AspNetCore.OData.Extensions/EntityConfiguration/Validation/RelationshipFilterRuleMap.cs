using System;
using Iql.Entities.Rules.Display;
using Iql.Entities.Rules.Relationship;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Validation
{
    public class RelationshipFilterRuleMap<TEntity> : RuleMap<TEntity, IRelationshipRule>
    {
        public override void AddRule(IRelationshipRule validationExpression, string propertyName = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentException($"{nameof(DisplayRule<object>)}s must have a property specified");
            }
            base.AddRule(validationExpression, propertyName);
        }
    }
}