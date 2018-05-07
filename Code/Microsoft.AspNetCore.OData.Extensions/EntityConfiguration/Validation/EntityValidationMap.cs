using Iql.Queryable.Data.EntityConfiguration.Validation;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Validation
{
    public class EntityValidationMap<TEntity> : RuleMap<TEntity, ValidationRule<TEntity>>
    {

    }
}