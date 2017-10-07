using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.OData.EntityFramework.Security
{
    public interface ISecurityFilter
    {
        bool CheckId { get; set; }
        Task<bool> CanUseAsync(SecurityFilterContext context);
        Task<bool> CanReadAsync(SecurityFilterContext context);
        Task<bool> CanCreateAsync(SecurityFilterContext context);
        Task<bool> CanDeleteAsync(SecurityFilterContext context);
        Task<bool> CanUpdateAsync(SecurityFilterContext context);
        Task<IActionResult> OnReadAsync(SecurityFilterContext context);
        Task<IActionResult> OnCreateAsync(SecurityFilterContext context);
        Task<IActionResult> OnDeleteAsync(SecurityFilterContext context);
        Task<IActionResult> OnUpdateAsync(SecurityFilterContext context);
        Task<IActionResult> OnNotFoundAsync(SecurityFilterContext context);
    }
    public interface ISecurityFilter<T> : ISecurityFilter
    {
        Task<bool> CanReadAsync(SecurityFilterContext<T> context);
        Task<bool> CanCreateAsync(SecurityFilterContext<T> context);
        Task<bool> CanDeleteAsync(SecurityFilterContext<T> context);
        Task<bool> CanUpdateAsync(SecurityFilterContext<T> context);
        Task<IActionResult> OnReadAsync(SecurityFilterContext<T> context);
        Task<IActionResult> OnCreateAsync(SecurityFilterContext<T> context);
        Task<IActionResult> OnDeleteAsync(SecurityFilterContext<T> context);
        Task<IActionResult> OnUpdateAsync(SecurityFilterContext<T> context);
        Task<IActionResult> OnNotFoundAsync(SecurityFilterContext<T> context);
    }
}
