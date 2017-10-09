using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OData.EntityFramework.Controllers;
using Microsoft.AspNetCore.OData.EntityFramework.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.OData.EntityFramework.Security
{
    public abstract class SecureRequestAttribute : ODataModelBaseAttribute
    {
        const string HttpKey = nameof(SecureRequestAttribute);
        public bool CheckId { get; set; } = true;
        //public Type EntityType { get; set; }
        public Type[] SecurityFilterTypes { get; set; }
        private ISecurityFilter[] SecurityFilters { get; set; }

        public virtual Task<IEnumerable<ISecurityFilter>> ResolveSecurityFiltersForRequestAsync<T>(
            ActionExecutingContext context, T entity)
            where T : class
        {
            return Task.FromResult<IEnumerable<ISecurityFilter>>(null);
        }

        private Task<IEnumerable<ISecurityFilter>> ResolveLocalSecurityFiltersAsync()
        {
            return Task.FromResult<IEnumerable<ISecurityFilter>>(SecurityFilters);
        }

        protected SecureRequestAttribute(params Type[] securityFilterTypes)
        {
            SecurityFilterTypes = securityFilterTypes;
        }

        protected SecureRequestAttribute()
        {
            // Make sure this runs AFTER LoadModelAttribute
            Order = 1;
        }

        protected SecureRequestAttribute(bool checkId)
        {
            CheckId = checkId;
        }

        public override async Task ActionExecutionAsync(ActionExecutingContext context)
        {
            if (context.ActionDescriptor.FilterDescriptors.AnyOfHigherScope(this))
            {
                return;
            }
            var method = GetType().GetRuntimeMethods()
                .Single(m => m.Name == nameof(ApplySecurityFiltersAsync));
            method = method.MakeGenericMethod(ODataController.EntityType);
            var securityFilterResult = await (Task<IActionResult>)method.Invoke(this, new[]
            {
                context,
                ODataController.PostedEntity
            });
            if (securityFilterResult != null)
            {
                context.Result = securityFilterResult;
            }
        }

        protected async Task<IActionResult> ApplySecurityFiltersAsync<T>(
            ActionExecutingContext context,
            T patchEntity)
            where T : class
        {
            if (context.HttpContext.Items.ContainsKey(HttpKey))
            {
                return null;
            }
            context.HttpContext.Items.Add(HttpKey, true);
            var controller = context.Controller as IODataCrudController<T>;
            if (controller == null)
            {
                throw DoesNotImplementException(context.Controller, typeof(IODataCrudController<T>));
            }
            IActionResult result = null;
            SecurityFilters = SecurityFilterTypes?
                .Select(sft => ActivatorUtilities.CreateInstance(context.HttpContext.RequestServices, sft))
                .Cast<ISecurityFilter>()
                .ToArray();
            var localSecurityFilters = await ResolveSecurityFiltersForRequestAsync(context, patchEntity)
                ?? new ISecurityFilter[] { };
            var localSecurityFilters2 = await ResolveLocalSecurityFiltersAsync()
                ?? new ISecurityFilter[] { };
            var securityFilters = localSecurityFilters.Concat(localSecurityFilters2).ToArray();
            if (securityFilters.Any())
            {
                foreach (var securityFilter in securityFilters)
                {
                    var entityById = default(T);
                    var hasId = false;
                    if (CheckId && securityFilter.CheckId)
                    {
                        entityById = (T)controller.TryGetModelFromId(context, out hasId);
                    }
                    var securityFilterContext = new SecurityFilterContext<T>(entityById ?? patchEntity, context, context.HttpContext, context.Controller as Controller, hasId);
                    if (!await securityFilter.CanUseAsync(securityFilterContext))
                    {
                        continue;
                    }
                    if (CheckId && hasId && entityById == null)
                    {
                        result = await securityFilter.OnNotFoundAsync(securityFilterContext);
                    }
                    else
                    {
                        switch (context.HttpContext.Request.Method)
                        {
                            case "GET":
                                result = await securityFilter.OnReadAsync(securityFilterContext);
                                break;
                            case "POST":
                                result = await securityFilter.OnCreateAsync(securityFilterContext);
                                break;
                            case "PATCH":
                            case "PUT":
                                result = await securityFilter.OnUpdateAsync(securityFilterContext);
                                break;
                            case "DELETE":
                                result = await securityFilter.OnDeleteAsync(securityFilterContext);
                                break;
                        }
                    }
                }
            }
            return result;
        }
    }
}
