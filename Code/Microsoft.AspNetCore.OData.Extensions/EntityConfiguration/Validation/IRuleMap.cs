using Iql.Entities.Rules;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Validation
{
    public interface IRuleMap
    {
        void AddRule(IRule rule, string propertyName = null);
    }
}