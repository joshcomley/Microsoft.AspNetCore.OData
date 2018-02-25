using Microsoft.OData.Edm;

namespace Brandless.AspNetCore.OData.Extensions
{
    public class EdmModelAccessor : IEdmModelAccessor
    {
        public IEdmModel EdmModel { get; set; }
    }
}
