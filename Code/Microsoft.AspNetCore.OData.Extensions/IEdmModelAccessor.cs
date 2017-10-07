using Microsoft.OData.Edm;

namespace Microsoft.AspNetCore.OData.Extensions
{
    public interface IEdmModelAccessor
    {
        IEdmModel EdmModel { get; set; }
    }
}
