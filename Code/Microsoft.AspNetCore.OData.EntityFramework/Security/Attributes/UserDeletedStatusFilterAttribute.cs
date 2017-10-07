using System;
using System.Threading.Tasks;
using Brandless.Data.EntityFramework.Extensions;
using Brandless.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.OData.EntityFramework.Security.Attributes
{
    public abstract class UserDeletedStatusFilterAttribute : ActionFilterAttribute
    {
        protected enum DeletedStatus
        {
            Deleted,
            NotDeleted
        }
        protected DeletedStatus OnlyAllow { get; }

        protected UserDeletedStatusFilterAttribute(DeletedStatus onlyAllow)
        {
            OnlyAllow = onlyAllow;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var id = context.ActionArguments["id"] as string;
            var db = context.HttpContext.RequestServices.GetService<DbContext>();
            var user = db.FindUserById(id);
            var lockoutEnd = (DateTimeOffset?)user.GetPropertyValue(nameof(IdentityUser.LockoutEnd));
            var isLockedOut = lockoutEnd.HasValue && lockoutEnd.Value > DateTime.Now;
            var shouldAllow = false;
            switch (OnlyAllow)
            {
                case DeletedStatus.Deleted:
                    shouldAllow = isLockedOut;
                    break;
                case DeletedStatus.NotDeleted:
                    shouldAllow = !isLockedOut;
                    break;
            }
            if(!shouldAllow)
            {
                context.Result = new UnauthorizedResult();
            }
            await base.OnActionExecutionAsync(context, next);
        }
    }
}
