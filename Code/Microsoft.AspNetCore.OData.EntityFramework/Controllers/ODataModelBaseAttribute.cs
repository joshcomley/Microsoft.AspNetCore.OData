using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.OData.EntityFramework.Controllers
{
    public abstract class ODataModelBaseAttribute : ActionFilterAttribute
    {
        public IODataCrudController ODataController { get; set; }
        public Controller Controller { get; set; }
        
        public abstract Task ActionExecutionAsync(ActionExecutingContext context);

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.Controller == null)
            {
                throw new ArgumentNullException(nameof(context), "Controller is null");
            }
            Controller = context.Controller as Controller;
            if (Controller == null)
            {
                throw DoesNotImplementException(context.Controller, typeof(Controller));
            }
            ODataController = Controller as IODataCrudController;
            if (ODataController == null)
            {
                throw new ArgumentException($"{context.Controller.GetType()} does not implement {nameof(IODataCrudController)}");
            }
            ODataController.SetModel(context);
            await ActionExecutionAsync(context);
            await base.OnActionExecutionAsync(context, next);
        }

        protected static Exception DoesNotImplementException(object obj, Type type)
        {
            return new ArgumentException($"{obj.GetType()} does not inherit from {type.Name}");
        }
    }
}
