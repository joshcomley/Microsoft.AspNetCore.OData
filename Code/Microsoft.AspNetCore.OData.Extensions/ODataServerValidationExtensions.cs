using Microsoft.AspNetCore.OData.Builder;

namespace Brandless.AspNetCore.OData.Extensions
{
    public static class ODataServerValidationExtensions
    {
        public static void AddODataServerFieldValidation(this ODataConventionModelBuilder builder)
        {
            var validateField =
                builder
                    .Action("ValidateField")
                    .Returns<string>();
            validateField.Parameter<string>("SetName");
            validateField.Parameter<string>("Name");
            validateField.Parameter<string>("Value");
        }

        public static void AddOdataServerEntityValidation<TEntity>(this EntityTypeConfiguration<TEntity> builder)
            where TEntity : class
        {
            var validateEntity =
                builder
                    .Action("ValidateEntity")
                    .Returns<string>();
            validateEntity.Parameter<string>("SetName");
            validateEntity.Parameter<TEntity>("Entity");
        }
    }
}
