using System;
using Microsoft.AspNetCore.OData.Builder;
using Microsoft.OData.Edm;

namespace Microsoft.AspNetCore.OData.Extensions.Configuration
{
    public interface IODataEntitySetConfigurator
    {
        void Configure(ODataModelBuilder builder, Action<Action<EdmModel>> model);
    }
}