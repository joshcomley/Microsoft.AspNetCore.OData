using Microsoft.OData.Edm;

namespace Brandless.AspNetCore.OData.Extensions
{
    public class EdmModelAccessor : IEdmModelAccessor
    {
        public EdmModel EdmModel { get; set; }
    }
}
