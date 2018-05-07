using System;
using Iql.Queryable.Data.EntityConfiguration.Rules.Display;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Validation
{
    public class DisplayRuleMap<TEntity> : RuleMap<TEntity, DisplayRule<TEntity>>
    {
        public override void AddRule(DisplayRule<TEntity> validationExpression, string propertyName = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentException($"{nameof(DisplayRule<object>)}s must have a property specified");
            }
            base.AddRule(validationExpression, propertyName);
        }
    }
}