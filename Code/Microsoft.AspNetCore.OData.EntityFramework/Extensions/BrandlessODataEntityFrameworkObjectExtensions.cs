namespace Microsoft.AspNetCore.OData.EntityFramework.Extensions
{
    public static class BrandlessODataEntityFrameworkObjectExtensions
    {
        public static string ToStringOrEmpty(this object obj)
        {
            return obj?.ToString() ?? string.Empty;
        }
    }
}