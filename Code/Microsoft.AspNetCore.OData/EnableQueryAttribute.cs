using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OData.Common;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.Edm;

namespace Microsoft.AspNetCore.OData
{
    // TODO: Replace with full version in the future.
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class EnableQueryAttribute : ActionFilterAttribute
    {
        private ODataQuerySettings _querySettings;
        //internal static IServiceProvider ServiceProvider { get; set; }

        public EnableQueryAttribute()
        {
            _querySettings =
                new ODataQuerySettings
                {
                    SearchDerivedTypeWhenAutoExpand = true
                };
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context == null)
            {
                throw Error.ArgumentNull("context");
            }

            var response = context.Result as StatusCodeResult;
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

            var result = context.Result as ObjectResult;
            if (result == null)
            {
                if (context.Exception != null)
                {
                    throw context.Exception;
                }
                throw Error.Argument("context", SRResources.QueryingRequiresObjectContent, context.Result.GetType().FullName);
            }

            if (result?.Value is ODataError)
            {
                return;
            }

            var value = result.Value;
            if (request.GetDisplayUrl() == null || value == null ||
                value.GetType().GetTypeInfo().IsValueType || value is string)
            {
                return;
            }

            var elementClrType = result.GetElementType();
            var queryContext = new ODataQueryContext(
                model,
                elementClrType,
                request.ODataFeature().Path);

            var shouldApplyQuery =
                request.HasQueryOptions() ||
                ResolvePageSize(_querySettings, context.ActionDescriptor).HasValue ||
                new InterceptorContainer(elementClrType, context.HttpContext.RequestServices).Any ||
                value is SingleResult ||
                ODataCountMediaTypeMapping.IsCountRequest(context.HttpContext) ||
                ContainsAutoExpandProperty(queryContext);

            if (!shouldApplyQuery)
            {
                return;
            }

            var queryOptions = new ODataQueryOptions(queryContext, request, context.HttpContext.RequestServices);

            long? count = null;
            var processedResult = ApplyQueryOptions(result.Value, queryOptions, context.ActionDescriptor);

            var enumberable = processedResult as IEnumerable<object>;
            if (enumberable != null)
            {
                // Apply count to result, if necessary, and return as a page result
                if (queryOptions.Count)
                {
                    count = request.ODataFeature().TotalCount;
                    count = Count(result.Value, queryOptions, context.ActionDescriptor);
                }
                // We might be getting a single result, so no paging involved
                var nextPageLink = request.ODataFeature().NextLink;
                var pageResult = new PageResult<object>(enumberable, nextPageLink, count);
                result.Value = pageResult;
            }
            else
            {
                // Return just the single entity
                result.Value = processedResult;
            }
        }

        private bool ContainsAutoExpandProperty(ODataQueryContext context)
        {
            Type elementClrType = context.ElementClrType;

            IEdmModel model = context.Model;
            if (model == null)
            {
                throw Error.InvalidOperation(SRResources.QueryGetModelMustNotReturnNull);
            }
            IEdmEntityType baseEntityType = EdmModelExtensions.GetEdmType(model, elementClrType) as IEdmEntityType;
            List<IEdmEntityType> entityTypes = new List<IEdmEntityType>();
            if (baseEntityType != null)
            {
                entityTypes.Add(baseEntityType);
                if (_querySettings.SearchDerivedTypeWhenAutoExpand)
                {
                    entityTypes.AddRange(EdmLibHelpers.GetAllDerivedEntityTypes(baseEntityType, model));
                }

                foreach (var entityType in entityTypes)
                {
                    var navigationProperties = entityType == baseEntityType
                        ? entityType.NavigationProperties()
                        : entityType.DeclaredNavigationProperties();
                    if (navigationProperties != null)
                    {
                        foreach (var navigationProperty in navigationProperties)
                        {
                            if (EdmLibHelpers.IsAutoExpand(navigationProperty, model))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        internal static object SingleOrDefault(IQueryable queryable, ActionDescriptor actionDescriptor)
        {
            var enumerator = queryable.GetEnumerator();
            try
            {
                var result = enumerator.MoveNext() ? enumerator.Current : null;

                if (enumerator.MoveNext())
                {
                    throw new InvalidOperationException(Error.Format(
                        SRResources.SingleResultHasMoreThanOneEntity));
                }

                return result;
            }
            finally
            {
                // Fix for Issue #2097
                // Ensure any active/open database objects that were created
                // iterating over the IQueryable object are properly closed.
                var disposable = enumerator as IDisposable;
                disposable?.Dispose();
            }
        }

        public virtual object ApplyQueryOptions(object value, ODataQueryOptions options, ActionDescriptor descriptor)
        {
            var enumerable = value as IEnumerable;

            if (enumerable == null || value is string)
            {
                // response is not a collection; we only support $select and $expand on single entities.
                //ValidateSelectExpandOnly(queryOptions);

                //options.Request.ODataFeature().IsEnumerated = true;
                var singleResult = value as SingleResult;
                if (singleResult == null)
                {
                    // response is a single entity.
                    return ApplyQueryObject(value, options, false, descriptor);
                }
                // response is a composable SingleResult. ApplyQuery and call SingleOrDefault.
                var singleQueryable = singleResult.Queryable;
                singleQueryable = ApplyQuery(singleQueryable, options, true, descriptor);
                return SingleOrDefault(singleQueryable, descriptor);
            }

            // response is a collection.
            var query = (value as IQueryable) ?? enumerable.AsQueryable();
            query = ApplyQuery(query, options, true, descriptor);
            if (ODataCountMediaTypeMapping.IsCountRequest(options.Request.HttpContext))
            {
                long? count = options.Request.ODataFeature().TotalCount;

                if (count.HasValue)
                {
                    // Return the count value if it is a $count request.
                    return count.Value;
                }
            }
            return query;
        }

        /// <summary>
        /// Applies the query to the given entity based on incoming query from uri and query settings.
        /// </summary>
        /// <param name="query">The original entity from the response message.</param>
        /// <param name="queryOptions">
        ///     The <see cref="ODataQueryOptions"/> instance constructed based on the incoming request.
        /// </param>
        /// <param name="shouldApplyQuery"></param>
        /// <param name="actionDescriptor"></param>
        /// <returns>The new entity after the $select and $expand query has been applied to.</returns>
        public virtual IQueryable ApplyQuery(IQueryable query, ODataQueryOptions queryOptions, bool shouldApplyQuery, ActionDescriptor actionDescriptor)
        {
            if (query == null)
            {
                throw Error.ArgumentNull("entity");
            }
            if (queryOptions == null)
            {
                throw Error.ArgumentNull("queryOptions");
            }
            query = (IQueryable)InvokeInterceptors(query, queryOptions.Context.ElementClrType, queryOptions, _querySettings);
            if (shouldApplyQuery)
            {
                // TODO: If we are using SQL, set this to false
                // otherwise if it is entities in code then
                // set it to true
                _querySettings.HandleNullPropagation =
                    //HandleNullPropagationOption.True
                    HandleNullPropagationOptionHelper.GetDefaultHandleNullPropagationOption(query);
                //PageSize = actionDescriptor.PageSize(),

                //var pageSize = ResolvePageSize(settings, actionDescriptor);
                query = queryOptions.ApplyTo(query, new ODataQuerySettings
                {
                    HandleNullPropagation = HandleNullPropagationOption.False
                },
                ResolvePageSize(_querySettings, actionDescriptor));
            }
            return query;
        }

        /// <summary>
        /// Applies the query to the given entity based on incoming query from uri and query settings.
        /// </summary>
        /// <param name="entity">The original entity from the response message.</param>
        /// <param name="queryOptions">
        /// The <see cref="ODataQueryOptions"/> instance constructed based on the incoming request.
        /// </param>
        /// <returns>The new entity after the $select and $expand query has been applied to.</returns>
        public virtual object ApplyQueryObject(object query, ODataQueryOptions queryOptions, bool shouldApplyQuery,
            ActionDescriptor actionDescriptor)
        {
            if (query == null)
            {
                throw Error.ArgumentNull("query");
            }
            if (queryOptions == null)
            {
                throw Error.ArgumentNull("queryOptions");
            }

            query = InvokeInterceptors(query, queryOptions.Context.ElementClrType, queryOptions, _querySettings);

            if (shouldApplyQuery)
            {
                query = queryOptions.ApplyTo(query as IQueryable, _querySettings, ResolvePageSize(_querySettings, actionDescriptor));//, ResolvePageSize(_querySettings, actionDescriptor));
            }
            return query;
        }

        private object InvokeInterceptors(object query, Type elementClrType, ODataQueryOptions queryOptions, ODataQuerySettings settings)
        {
            return ApplyInterceptors(query, elementClrType, queryOptions.Request, settings, queryOptions);
        }

        internal static IQueryable<T> ApplyInterceptorsGeneric<T>(
            IQueryable<T> query,
            HttpRequest request,
            ODataQuerySettings querySettings,
            ODataQueryOptions queryOptions
            )
        {
            var interceptors = queryOptions.Request.HttpContext.RequestServices.GetServices<IODataQueryInterceptor<T>>();
            foreach (var interceptor in interceptors)
            {
                query = query.Where(interceptor.Intercept(request.HttpContext, querySettings, queryOptions));
            }
            return query;
        }

        internal static object ApplyInterceptors(
            object query,
            Type elementClrType,
            HttpRequest request,
            ODataQuerySettings querySettings,
            ODataQueryOptions queryOptions = null
            )
        {
            var queryToFilter = query;
            var returnAsSingle = false;
            var type = queryToFilter.GetType();
            if (queryToFilter is IEnumerable && type.GetTypeInfo().IsGenericType && type.GetGenericArguments()[0] == typeof(object))
            {
                var enumerable = queryToFilter as IEnumerable<object>;
                var cast = true;
                var objects = enumerable as object[] ?? enumerable.ToArray();
                if (objects.Any() && !objects.All(elementClrType.IsInstanceOfType))
                {
                    cast = false;
                }
                if (cast)
                {
                    var method = typeof(EnableQueryAttribute)
                        .GetMethod(nameof(Cast), BindingFlags.Static | BindingFlags.NonPublic)
                        .MakeGenericMethod(elementClrType);
                    queryToFilter = method.Invoke(null, new[] { objects });
                }
            }
            //var model = GetModel(elementClrType, request, null);
            if (!typeof(IQueryable<>).MakeGenericType(elementClrType).IsInstanceOfType(queryToFilter))
            {
                returnAsSingle = true;
                queryToFilter =
                    GetGenericMethod(nameof(ToQueryable), elementClrType)
                        .Invoke(null, new[] { queryToFilter });
            }
            var result =
                (IQueryable)
                    GetGenericMethod(nameof(ApplyInterceptorsGeneric), elementClrType)
                .Invoke(null, new[] { queryToFilter, request, querySettings, queryOptions });
            if (returnAsSingle)
            {
                foreach (var single in result)
                {
                    // Return first entry
                    return single;
                }
            }
            return result;
        }

        internal static IQueryable<T> ToQueryable<T>(T entity)
        {
            return new[] { entity }.AsQueryable();
        }

        private static IEnumerable<T> Cast<T>(IEnumerable<object> enumerable)
        {
            return enumerable.Cast<T>().AsQueryable();
        }

        private static MethodInfo GetGenericMethod(string name, Type elementClrType)
        {
            var methodInfo = typeof(EnableQueryAttribute)
                .GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
            return methodInfo
                .MakeGenericMethod(elementClrType);
        }

        public virtual long Count(object value, ODataQueryOptions options, ActionDescriptor descriptor)
        {
            var enumerable = value as IEnumerable;
            if (enumerable == null)
            {
                // response is single entity.
                return 1;
            }

            // response is a collection.
            var query = value as IQueryable ?? enumerable.AsQueryable();
            var settings = new ODataQuerySettings
            {
                HandleNullPropagation = HandleNullPropagationOption.False
            };
            var forCount = options.ApplyForCount(query, settings);
            var count = forCount.Cast<object>().LongCount();
            return count;
        }

        private int? _pageSize;
        private bool _pageSizeChecked;
        private int? ResolvePageSize(ODataQuerySettings querySettings, ActionDescriptor actionDescriptor)
        {
           // if (!_pageSizeChecked)
          //  {
             //   _pageSizeChecked = true;
                var queryPageSize = querySettings.PageSize;
                var actionPageSize = actionDescriptor.PageSize();
                _pageSize = actionPageSize.IsSet ? actionPageSize.Size : queryPageSize;
        //    }
            return _pageSize;
        }
    }
}