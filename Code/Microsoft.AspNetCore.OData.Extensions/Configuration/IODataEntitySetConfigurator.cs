using Microsoft.AspNetCore.OData.Builder;

namespace Brandless.AspNetCore.OData.Extensions.Configuration
{
    public interface IODataEntitySetConfigurator
    {
        void Configure(ODataModelBuilder builder);
    }
}