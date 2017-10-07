using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.OData.Extensions.Extensions
{
    public static class ActionExecutingContextExtensions
    {
        public static bool HasId(this ActionExecutingContext context)
        {
            return context.ActionArguments.Keys.Any(k => k.Equals("key", StringComparison.CurrentCultureIgnoreCase));
        }

        public static int Id(this ActionExecutingContext context)
        {
            return context.Id<int>();
        }

        public static T Id<T>(this ActionExecutingContext context)
        {
            return context.Argument<T>("key");
        }

        public static T Argument<T>(this ActionExecutingContext context, string argument)
        {
            var key = context.ActionArguments.Keys.Single(k => k.Equals(argument, StringComparison.CurrentCultureIgnoreCase) || 
            (argument == "key" && k.Equals("id", StringComparison.CurrentCultureIgnoreCase)));
            return (T)context.ActionArguments[key];
        }
    }
}
