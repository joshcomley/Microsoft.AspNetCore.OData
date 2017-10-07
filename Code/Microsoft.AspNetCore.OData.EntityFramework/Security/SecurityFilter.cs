using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.OData.EntityFramework.Security
{
    public abstract class SecurityFilter<T> : ISecurityFilter<T>
    {
        public bool CheckId { get; set; } = true;

        public virtual Task<bool> CanUseAsync(SecurityFilterContext context)
        {
            return Task.FromResult(true);
        }

        protected abstract Task<bool> CanEditAsync(SecurityFilterContext<T> context);

        public virtual Task<bool> CanReadAsync(SecurityFilterContext<T> context)
        {
            return Task.FromResult(true);
        }

        public virtual Task<bool> CanCreateAsync(SecurityFilterContext<T> context)
        {
            return CanEditAsync(context);
        }

        public virtual Task<bool> CanDeleteAsync(SecurityFilterContext<T> context)
        {
            return CanEditAsync(context);
        }

        public virtual Task<bool> CanUpdateAsync(SecurityFilterContext<T> context)
        {
            return CanEditAsync(context);
        }

        public virtual async Task<IActionResult> OnReadAsync(SecurityFilterContext<T> context)
        {
            if (!await CanReadAsync(context))
            {
                return NotFound(context);
            }
            return null;
        }

        public virtual async Task<IActionResult> OnCreateAsync(SecurityFilterContext<T> context)
        {
            if (!await CanCreateAsync(context))
            {
                return Unauthorized(context);
            }
            return null;
        }

        public virtual async Task<IActionResult> OnDeleteAsync(SecurityFilterContext<T> context)
        {
            if (!await CanDeleteAsync(context))
            {
                return Unauthorized(context);
            }
            return null;
        }

        public virtual async Task<IActionResult> OnUpdateAsync(SecurityFilterContext<T> context)
        {
            if (!await CanUpdateAsync(context))
            {
                return Unauthorized(context);
            }
            return null;
        }

        public Task<IActionResult> OnNotFoundAsync(SecurityFilterContext<T> context)
        {
            return Task.FromResult<IActionResult>(context.Controller.NotFound());
        }

        protected virtual StatusCodeResult Unauthorized(SecurityFilterContext<T> context)
        {
            return context.Controller.Unauthorized();
        }

        protected virtual StatusCodeResult NotFound(SecurityFilterContext<T> context)
        {
            return context.Controller.NotFound();
        }

        Task<bool> ISecurityFilter.CanReadAsync(SecurityFilterContext context)
        {
            return CanReadAsync((SecurityFilterContext<T>)context);
        }

        Task<bool> ISecurityFilter.CanCreateAsync(SecurityFilterContext context)
        {
            return CanCreateAsync((SecurityFilterContext<T>)context);
        }

        Task<bool> ISecurityFilter.CanDeleteAsync(SecurityFilterContext context)
        {
            return CanCreateAsync((SecurityFilterContext<T>)context);
        }

        Task<bool> ISecurityFilter.CanUpdateAsync(SecurityFilterContext context)
        {
            return CanCreateAsync((SecurityFilterContext<T>)context);
        }

        Task<IActionResult> ISecurityFilter.OnReadAsync(SecurityFilterContext context)
        {
            return OnReadAsync((SecurityFilterContext<T>)context);
        }

        Task<IActionResult> ISecurityFilter.OnCreateAsync(SecurityFilterContext context)
        {
            return OnCreateAsync((SecurityFilterContext<T>)context);
        }

        Task<IActionResult> ISecurityFilter.OnDeleteAsync(SecurityFilterContext context)
        {
            return OnDeleteAsync((SecurityFilterContext<T>)context);
        }

        Task<IActionResult> ISecurityFilter.OnUpdateAsync(SecurityFilterContext context)
        {
            return OnUpdateAsync((SecurityFilterContext<T>)context);
        }

        Task<IActionResult> ISecurityFilter.OnNotFoundAsync(SecurityFilterContext context)
        {
            return OnNotFoundAsync((SecurityFilterContext<T>)context);
        }
    }
}
