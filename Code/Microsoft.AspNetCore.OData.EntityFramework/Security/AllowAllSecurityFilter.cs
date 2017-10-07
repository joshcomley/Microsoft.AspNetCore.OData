using System.Threading.Tasks;

namespace Microsoft.AspNetCore.OData.EntityFramework.Security
{
    public class AllowAllSecurityFilter<T> : SecurityFilter<T>
    {
        protected override Task<bool> CanEditAsync(SecurityFilterContext<T> context)
        {
            return Task.FromResult(true);
        }
    }
}
