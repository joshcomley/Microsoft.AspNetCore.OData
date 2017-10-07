using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.OData.EntityFramework.Controllers
{
    public class ValidateModelAttribute : ODataModelAttribute
    {
        public ValidateModelAttribute()
        {
            Order = 0;
        }

        public override async Task ActionExecutionAsync(ActionExecutingContext context)
        {
            //var str = await context.HttpContext.GetHttpRequestMessage().Content.ReadAsStringAsync();
            if (!Controller.ModelState.IsValid)
            {
                context.Result = Controller.ODataModelStateError();
            }
            //return false;
            //return Task.FromResult(false);
        }
    }
}
