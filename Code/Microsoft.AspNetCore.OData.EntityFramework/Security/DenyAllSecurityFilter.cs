using System.Threading.Tasks;

namespace Microsoft.AspNetCore.OData.EntityFramework.Security
{
    public class DenyAllSecurityFilter<T> : SecurityFilter<T>
    {

	    public override Task<bool> CanReadAsync(SecurityFilterContext<T> context)
	    {
		    return Task.FromResult(false);
	    }

	    protected override Task<bool> CanEditAsync(SecurityFilterContext<T> context)
        {
            return Task.FromResult(false);
        }

        public override Task<bool> CanDeleteAsync(SecurityFilterContext<T> context)
        {
            return Task.FromResult(false);
        }

        public override Task<bool> CanCreateAsync(SecurityFilterContext<T> context)
        {
            return Task.FromResult(false);
        }

        public override Task<bool> CanUpdateAsync(SecurityFilterContext<T> context)
        {
            return Task.FromResult(false);
        }
    }
}
