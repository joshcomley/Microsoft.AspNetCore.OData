using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OData.Common;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.OData;

namespace Microsoft.AspNetCore.OData
{
    // TODO: Replace with full version in the future.
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class EnableQueryAttribute : ActionFilterAttribute
    {
        private ODataQueryApplicator _queryApplicator;
        //internal static IServiceProvider ServiceProvider { get; set; }

        public EnableQueryAttribute()
        {
            _queryApplicator = new ODataQueryApplicator();
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            ActionFilterAttribute actionFilterAttribute = this;
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (next == null)
                throw new ArgumentNullException(nameof(next));
            actionFilterAttribute.OnActionExecuting(context);
            if (context.Result != null)
                return;
            ActionExecutedContext context1 = await next();
            await OnActionExecutedAsync(context1);
        }

        public async Task OnActionExecutedAsync(ActionExecutedContext context)
        {
            if (context == null)
            {
                throw Error.ArgumentNull("context");
            }

            var contextResult = context.Result;
            var response = contextResult as StatusCodeResult;
            if (response != null)// && !response.IsSuccessStatusCode())
            {
                return;
            }

            var request = context.HttpContext.Request;
            var model = request.ODataFeature().Model;
            if (model == null)
            {
                throw Error.InvalidOperation(SRResources.QueryGetModelMustNotReturnNull);
            }

            var result = contextResult as ObjectResult;
            if (result == null)
            {
                if (context.Exception != null)
                {
                    throw context.Exception;
                }

                return;
                throw Error.Argument("context", SRResources.QueryingRequiresObjectContent, contextResult.GetType().FullName);
            }

            if (result?.Value is ODataError)
            {
                return;
            }

            _queryApplicator.PageSize = await ResolvePageSize(context);

            result.Value = await _queryApplicator.ProcessQueryAsync(
                request,
                result.Value,
                result.GetElementType());
        }

        private int? _pageSize;
        private bool _pageSizeChecked;
        private async Task<int?> ResolvePageSize(
            ActionExecutedContext context)
        {
            // if (!_pageSizeChecked)
            //  {
            //   _pageSizeChecked = true;
            var queryPageSize = _queryApplicator.QuerySettings.PageSize;
            var actionPageSize = await context.PageSizeAsync();
            _pageSize = actionPageSize.IsSet ? actionPageSize.Size : queryPageSize;
            //    }
            return _pageSize;
        }
    }
}