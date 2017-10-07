using System.Threading.Tasks;

namespace Microsoft.AspNetCore.OData.EntityFramework.Security
{
	public class ReadOnlySecurityFilter<T> : SecurityFilter<T>
	{
		protected override Task<bool> CanEditAsync(SecurityFilterContext<T> context)
		{
			return Task.FromResult(false);
		}

		public override Task<bool> CanReadAsync(SecurityFilterContext<T> context)
		{
			return Task.FromResult(true);
		}
	}
}
