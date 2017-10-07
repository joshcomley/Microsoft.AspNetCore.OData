using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.OData.EntityFramework.Security
{
    public class SecurityFilterContext
    {
        public ActionExecutingContext Context { get; }
        public HttpContext HttpContext { get; }
        public Controller Controller { get; }
        public bool HasFeedEntityId { get; }

        public SecurityFilterContext(
            ActionExecutingContext context, 
            HttpContext httpContext, 
            Controller controller,
            bool hasFeedEntityId)
        {
            Context = context;
            HttpContext = httpContext;
            Controller = controller;
            HasFeedEntityId = hasFeedEntityId;
        }
    }

    public class SecurityFilterContext<T> : SecurityFilterContext
    {
        public T Entity { get; }

        public SecurityFilterContext(T entity, ActionExecutingContext context, HttpContext httpContext, Controller controller, bool hasFeedEntityId)
            : base(context, httpContext, controller, hasFeedEntityId)
        {
            Entity = entity;
        }
    }
}
