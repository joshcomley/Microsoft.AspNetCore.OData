using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.OData.Common;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;

namespace Microsoft.AspNetCore.OData
{
    public class ODataQueryApplicator
    {
        public int? PageSize { get; set; }
        internal ODataQuerySettings QuerySettings { get; }
        public ODataQueryApplicator(int? pageSize = null)
        {
            PageSize = pageSize;
            QuerySettings =
                DefaultQuerySettings();
        }
        private static ODataQuerySettings DefaultQuerySettings()
        {
            return new ODataQuerySettings
            {
                SearchDerivedTypeWhenAutoExpand = true
            };
        }

        public virtual async Task<object> ProcessQueryAsync(
            HttpRequest request,
            object value,
            Type elementClrType,
            bool ignoreSkip = false,
            bool ignoreTop = false)
        {
            var model = request.ODataFeature().Model;
            if (request.GetDisplayUrl() == null || value == null ||
                value.GetType().GetTypeInfo().IsValueType || value is string)
            {
                return value;
            }

            var queryContext = new ODataQueryContext(
                model,
                elementClrType,
                request.ODataFeature().Path);

            var shouldApplyQuery =
                request.HasQueryOptions() ||
                PageSize.HasValue ||
                new InterceptorContainer(elementClrType, request.HttpContext.RequestServices).Any ||
                value is SingleResult ||
                ODataCountMediaTypeMapping.IsCountRequest(request.HttpContext) ||
                ContainsAutoExpandProperty(queryContext, QuerySettings);

            if (!shouldApplyQuery)
            {
                return value;
            }

            var queryOptions = new ODataQueryOptions(queryContext, request, request.HttpContext.RequestServices);
            if (ignoreSkip)
            {
                queryOptions.IgnoreSkip = true;
            }
            if (ignoreSkip)
            {
                queryOptions.IgnoreTop = true;
            }
            var processedResult = await ApplyQueryOptionsAsync(
                value,
                queryOptions);

            var enumberable = processedResult as IEnumerable<object>;
            if (enumberable != null)
            {
                long? count = null;
                // Apply count to result, if necessary, and return as a page result
                if (queryOptions.Count)
                {
                    //count = request.ODataFeature().TotalCount;
                    count = await Count(value, queryOptions);
                }

                // We might be getting a single result, so no paging involved
                var nextPageLink = request.ODataFeature().NextLink;
                var pageResult = new PageResult<object>(enumberable, nextPageLink, count);
                value = pageResult;
            }
            else
            {
                // Return just the single entity
                value = processedResult;
            }

            return value;
        }

        private static bool ContainsAutoExpandProperty(
            ODataQueryContext context,
            ODataQuerySettings querySettings)
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
                if (querySettings.SearchDerivedTypeWhenAutoExpand)
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

        internal static object SingleOrDefault(IQueryable queryable)
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

        protected virtual async Task<object> ApplyQueryOptionsAsync(object value,
            ODataQueryOptions options)
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
                    return await ApplyQueryObjectAsync(value, options, false);
                }
                // response is a composable SingleResult. ApplyQuery and call SingleOrDefault.
                var singleQueryable = singleResult.Queryable;
                singleQueryable = await ApplyQueryAsync(singleQueryable, options, true);
                return SingleOrDefault(singleQueryable);
            }

            // response is a collection.
            var query = (value as IQueryable) ?? enumerable.AsQueryable();
            query = await ApplyQueryAsync(query, options, true);
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
        protected virtual async Task<IQueryable> ApplyQueryAsync(IQueryable query,
            ODataQueryOptions queryOptions,
            bool shouldApplyQuery)
        {
            if (query == null)
            {
                throw Error.ArgumentNull("entity");
            }
            if (queryOptions == null)
            {
                throw Error.ArgumentNull("queryOptions");
            }
            query = (IQueryable)
                await InvokeInterceptorsAsync(query, queryOptions.Context.ElementClrType, queryOptions, QuerySettings);
            if (shouldApplyQuery)
            {
                // TODO: If we are using SQL, set this to false
                // otherwise if it is entities in code then
                // set it to true
                QuerySettings.HandleNullPropagation =
                    //HandleNullPropagationOption.True
                    HandleNullPropagationOptionHelper.GetDefaultHandleNullPropagationOption(query);
                //PageSize = actionDescriptor.PageSize(),

                //var pageSize = ResolvePageSize(settings, actionDescriptor);
                query = queryOptions.ApplyTo(query, new ODataQuerySettings
                {
                    HandleNullPropagation = HandleNullPropagationOption.False
                },
                    PageSize, queryOptions);
            }
            return query;
        }

        /// <summary>
        /// Applies the query to the given entity based on incoming query from uri and query settings.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="queryOptions">
        ///     The <see cref="ODataQueryOptions"/> instance constructed based on the incoming request.
        /// </param>
        /// <param name="shouldApplyQuery"></param>
        /// <param name="context"></param>
        /// <param name="entity">The original entity from the response message.</param>
        /// <returns>The new entity after the $select and $expand query has been applied to.</returns>
        protected virtual async Task<object> ApplyQueryObjectAsync(object query,
            ODataQueryOptions queryOptions,
            bool shouldApplyQuery)
        {
            if (query == null)
            {
                throw Error.ArgumentNull("query");
            }
            if (queryOptions == null)
            {
                throw Error.ArgumentNull("queryOptions");
            }

            query = await InvokeInterceptorsAsync(query, queryOptions.Context.ElementClrType, queryOptions, QuerySettings);

            if (shouldApplyQuery)
            {
                query = queryOptions.ApplyTo(query as IQueryable, QuerySettings, PageSize, queryOptions);//, ResolvePageSize(_querySettings, actionDescriptor));
            }
            return query;
        }

        private Task<object> InvokeInterceptorsAsync(object query, Type elementClrType, ODataQueryOptions queryOptions, ODataQuerySettings settings)
        {
            return ApplyInterceptorsAsync(query, elementClrType, queryOptions.Request, settings, queryOptions);
        }

        internal static Task<IQueryable> ApplyInterceptorsGeneric<T>(
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
            return Task.FromResult<IQueryable>(query);
        }

        internal static async Task<object> ApplyInterceptorsAsync(
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
                await (Task<IQueryable>)
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
            var methodInfo = typeof(ODataQueryApplicator)
                .GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
            return methodInfo
                .MakeGenericMethod(elementClrType);
        }

        protected virtual Task<long> Count(object value, ODataQueryOptions options)
        {
            var enumerable = value as IEnumerable;
            if (enumerable == null)
            {
                // response is single entity.
                return Task.FromResult(1L);
            }

            // response is a collection.
            var query = value as IQueryable ?? enumerable.AsQueryable();
            var settings = new ODataQuerySettings
            {
                HandleNullPropagation = HandleNullPropagationOption.False
            };
            var forCount = options.ApplyForCount(query, settings);
            var count = forCount.Cast<object>().LongCount();
            return Task.FromResult(count);
        }
    }
}