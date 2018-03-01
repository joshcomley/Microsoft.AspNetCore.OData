using Brandless.AspNetCore.OData.Extensions.EntityConfiguration;
using Microsoft.OData.Edm;

namespace Brandless.AspNetCore.OData.Extensions.Extensions
{
    public static class EdmModelExtensions
    {
        public static ModelConfiguration ModelConfiguration(this EdmModel model)
        {
            return EntityConfiguration.ModelConfiguration.ForModel(model);
        }
    }
}