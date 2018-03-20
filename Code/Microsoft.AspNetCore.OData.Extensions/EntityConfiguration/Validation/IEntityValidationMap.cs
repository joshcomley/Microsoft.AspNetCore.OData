using System;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Validation
{
    public interface IEntityValidationMap
    {
        void AddValidation(IEntityValidation validationExpression, string propertyName = null);
    }
}