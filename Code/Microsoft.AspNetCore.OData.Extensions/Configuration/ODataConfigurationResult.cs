using Microsoft.AspNetCore.OData.Builder;
using Microsoft.OData.Edm;

namespace Brandless.AspNetCore.OData.Extensions.Configuration
{
    public class ODataConfigurationResult
    {
        public IEdmModel Model { get; }
        public ODataModelBuilder ModelBuilder { get; }

        public ODataConfigurationResult(IEdmModel model, ODataModelBuilder modelBuilder)
        {
            Model = model;
            ModelBuilder = modelBuilder;
        }
    }
}