using Microsoft.OData.Edm;

namespace Microsoft.AspNetCore.OData.Extensions
{
    public class EdmModelAccessor : IEdmModelAccessor
    {
        public IEdmModel EdmModel { get; set; }
    }
}
