using Microsoft.OData.Edm;

namespace Brandless.AspNetCore.OData.Extensions
{
    public interface IEdmModelAccessor
    {
        EdmModel EdmModel { get; set; }
    }
}
