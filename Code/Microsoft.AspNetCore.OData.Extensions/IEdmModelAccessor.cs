using Microsoft.OData.Edm;

namespace Brandless.AspNetCore.OData.Extensions
{
    public interface IEdmModelAccessor
    {
        IEdmModel EdmModel { get; set; }
    }
}
