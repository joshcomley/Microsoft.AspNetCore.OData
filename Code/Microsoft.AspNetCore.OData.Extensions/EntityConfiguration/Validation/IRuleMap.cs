using System;
using Iql.Queryable.Data.EntityConfiguration.Rules;
using Iql.Queryable.Data.EntityConfiguration.Validation;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Validation
{
    public interface IRuleMap
    {
        void AddRule(IRule rule, string propertyName = null);
    }
}