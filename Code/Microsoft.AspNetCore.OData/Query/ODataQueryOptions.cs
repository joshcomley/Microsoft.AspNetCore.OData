// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Common;
using Microsoft.AspNetCore.OData.Extensions;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace Microsoft.AspNetCore.OData.Query
{
    // TODO: Replace with full version in the future.
    public class ODataQueryOptions
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAssemblyProvider _assemblyProvider;
        private readonly ODataQueryOptionParser _queryOptionParser;

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataQueryOptions"/> class based on the incoming request and some metadata information from
        /// the <see cref="ODataQueryContext"/>.
        /// </summary>
        /// <param name="context">The <see cref="ODataQueryContext"/> which contains the <see cref="IEdmModel"/> and some type information.</param>
        /// <param name="request">The incoming request message.</param>
        /// <param name="serviceProvider"></param>
        public ODataQueryOptions(ODataQueryContext context, HttpRequest request, IServiceProvider serviceProvider)
        {
            if (context == null)
            {
                throw Error.ArgumentNull("context");
            }

            if (request == null)
            {
                throw Error.ArgumentNull("request");
            }
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

            _serviceProvider = serviceProvider;
            _assemblyProvider = request.AssemblyProvider();

            Context = context;
            Request = request;
            RawValues = new ODataRawQueryOptions();

            var queryOptionDict = request.Query.ToDictionary(p => p.Key, p => p.Value.FirstOrDefault());
            _queryOptionParser = new ODataQueryOptionParser(
                context.Model,
                context.ElementType,
                context.NavigationSource,
                queryOptionDict);

            _queryOptionParser.Resolver = request.ODataFeature().UriResolverSettings.CreateResolver();

            BuildQueryOptions(queryOptionDict);
        }

        /// <summary>
        ///  Gets the given <see cref="ODataQueryContext"/>
        /// </summary>
        public ODataQueryContext Context { get; }

        /// <summary>
        /// Gets the request message associated with this instance.
        /// </summary>
        public HttpRequest Request { get; }

        /// <summary>
        /// Gets the raw string of all the OData query options
        /// </summary>
        public ODataRawQueryOptions RawValues { get; }

        /// <summary>
        /// Gets the <see cref="FilterQueryOption"/>.
        /// </summary>
        public FilterQueryOption Filter { get; private set; }

        /// <summary>
        /// Gets the <see cref="OrderByQueryOption"/>.
        /// </summary>
        public OrderByQueryOption OrderBy { get; private set; }

        /// <summary>
        /// Gets the <see cref="SelectExpandQueryOption"/>.
        /// </summary>
        public SelectExpandQueryOption SelectExpand { get; private set; }

        /// <summary>
        /// Gets the <see cref="CountQueryOption"/>.
        /// </summary>
        public CountQueryOption CountQueryOption { get; private set; }

        /// <summary>
        /// Gets the <see cref="ApplyQueryOption"/>.
        /// </summary>
        public ApplyQueryOption Apply { get; private set; }

        /// <summary>
        /// Specifies a non-negative integer n that excludes the first n items of the queried collection from the result.         
        /// </summary>
        /// <remarks>
        /// Corresponds to the $skip query option.  The service returns items starting at position n+1.
        /// </remarks>
        public int? Skip { get; private set; }

        /// <summary>
        /// Specifies a non-negative integer n that limits the number of items returned from a collection.
        /// </summary>
        /// <remarks>
        /// Corresponds to the $take query option.  he service returns the number of available items up to but not greater than the specified value n.
        /// </remarks>
        public int? Top { get; private set; }

        /// <summary>
        /// The $count system query option allows clients to request a count of the matching resources included with the resources in the response. 
        /// The $count query option has a Boolean value of true or false.       
        /// </summary>
        /// <remarks>
        /// The semantics of $count is covered in the [OData - Protocol] document.
        /// </remarks>
        public bool Count { get; private set; }

        /// <summary>
        /// Applies the queries that are applicable to the $count computation, as per the OData protocol.
        /// </summary>
        /// <remarks>
        /// According to the protocol:
        /// <para>
        /// The $count system query option ignores any $top, $skip, or $expand query options, and returns the total count of results across all pages including only those results matching any specified $filter and $search. Clients should be aware that the count returned inline may not exactly equal the actual number of items returned, due to latency between calculating the count and enumerating the last value or due to inexact calculations on the service.
        /// </para>
        /// </remarks>
        /// <param name="query">The original <see cref="IQueryable"/>.</param>
        /// <param name="querySettings">The settings to use in query composition.</param>
        /// <returns>The new <see cref="IQueryable"/> after the query has been applied to.</returns>
        public virtual IQueryable ApplyForCount(IQueryable query, ODataQuerySettings querySettings)
        {
            if (query == null)
            {
                throw Error.ArgumentNull("query");
            }

            // Construct the actual query and apply them in the following order: filter
            if (Filter != null)
            {
                query = Filter.ApplyTo(query, querySettings, _assemblyProvider);
            }

            return query;
        }

        /// <summary>
        /// Apply the individual query to the given IQueryable in the right order.
        /// </summary>
        /// <param name="query">The original <see cref="IQueryable"/>.</param>
        /// <param name="querySettings">The settings to use in query composition.</param>
		/// <param name="pageSize">The page size for this query</param>
        /// <returns>The new <see cref="IQueryable"/> after the query has been applied to.</returns>
        public virtual IQueryable ApplyTo(IQueryable query, ODataQuerySettings querySettings, int? pageSize)
        {
            if (query == null)
            {
                throw Error.ArgumentNull("query");
            }

            // Construct the actual query and apply them in the following order: filter
            if (Filter != null)
            {
                query = Filter.ApplyTo(query, querySettings, _assemblyProvider);
            }

            var orderBy = OrderBy;
            // $skip or $top require a stable sort for predictable results.
            // Result limits require a stable sort to be able to generate a next page link.
            // If either is present in the query and we have permission,
            // generate an $orderby that will produce a stable sort.
            if (querySettings.EnsureStableOrdering &&
                (Skip != null ||
                 Top != null ||
                 pageSize.HasValue))
            {
                // If there is no OrderBy present, we manufacture a default.
                // If an OrderBy is already present, we add any missing
                // properties necessary to make a stable sort.
                // Instead of failing early here if we cannot generate the OrderBy,
                // let the IQueryable backend fail (if it has to).
                orderBy = orderBy == null
                            ? OrderByHelper.GenerateDefaultOrderBy(Context, _serviceProvider)
                            : OrderByHelper.EnsureStableSortOrderBy(orderBy, Context, _serviceProvider);
            }

            // First apply $apply
            // Section 3.15 of the spec http://docs.oasis-open.org/odata/odata-data-aggregation-ext/v4.0/cs01/odata-data-aggregation-ext-v4.0-cs01.html#_Toc378326311
            if (Apply != null)
            {
                query = Apply.ApplyTo(query, querySettings, _assemblyProvider);
                Request.ODataFeature().ApplyClause = Apply.ApplyClause;
                Context.ElementClrType = Apply.ResultClrType;
            }

            if (orderBy != null)
            {
                query = orderBy.ApplyTo(query, querySettings);
            }

            if (Skip.HasValue)
            {
                query = ExpressionHelpers.Skip(query, Skip.Value, Context.ElementClrType, false);
            }

            int? take = null;
            if (querySettings.PageSize.HasValue)
            {
                take = Math.Min(querySettings.PageSize.Value, int.MaxValue);
            }
            if (Top.HasValue)
            {
                take = Math.Min(Top.Value, take ?? int.MaxValue);
            }
            if (take.HasValue)
            {
                query = ExpressionHelpers.Take(query, take.Value, Context.ElementClrType, false);
            }

            if (SelectExpand != null)
            {
                query = SelectExpand.ApplyTo(query, querySettings, _assemblyProvider);
            }

            if (CountQueryOption != null)
            {
                if (Request.ODataFeature().TotalCountFunc == null)
                {
                    Func<long> countFunc = CountQueryOption.GetEntityCountFunc(query);
                    if (countFunc != null)
                    {
                        Request.ODataFeature().TotalCountFunc = countFunc;
                    }
                }

                if (ODataCountMediaTypeMapping.IsCountRequest(Request.HttpContext))
                {
                    return query;
                }
            }
            if (pageSize.HasValue)
            {
                bool resultsLimited;
                query = LimitResults(query, pageSize.Value, out resultsLimited);
                if (resultsLimited && Request.GetDisplayUrl() != null && new Uri(Request.GetDisplayUrl()).IsAbsoluteUri && Request.ODataFeature().NextLink == null)
                {
                    Uri nextPageLink = Request.GetNextPageLink(pageSize.Value);
                    Request.ODataFeature().NextLink = nextPageLink;
                }
            }
            return query;
        }

        private static readonly MethodInfo _limitResultsGenericMethod = typeof(ODataQueryOptions).GetMethod(nameof(LimitResults));
        internal static IQueryable LimitResults(IQueryable queryable, int limit, out bool resultsLimited)
        {
            MethodInfo genericMethod = _limitResultsGenericMethod.MakeGenericMethod(queryable.ElementType);
            object[] args = new object[] { queryable, limit, null };
            IQueryable results = genericMethod.Invoke(null, args) as IQueryable;
            resultsLimited = (bool)args[2];
            return results;
        }
        /// <summary>
        /// Limits the query results to a maximum number of results.
        /// </summary>
        /// <typeparam name="T">The entity CLR type</typeparam>
        /// <param name="queryable">The queryable to limit.</param>
        /// <param name="limit">The query result limit.</param>
        /// <param name="resultsLimited"><c>true</c> if the query results were limited; <c>false</c> otherwise</param>
        /// <returns>The limited query results.</returns>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", Justification = "Not intended for public use, only public to enable invokation without security issues.")]
        public static IQueryable<T> LimitResults<T>(IQueryable<T> queryable, int limit, out bool resultsLimited)
        {
            TruncatedCollection<T> truncatedCollection = new TruncatedCollection<T>(queryable, limit);
            resultsLimited = truncatedCollection.IsTruncated;
            return truncatedCollection.AsQueryable();
        }

        private void BuildQueryOptions(IDictionary<string, string> queryParameters)
        {
            foreach (var kvp in queryParameters)
            {
                switch (kvp.Key.ToLowerInvariant())
                {
                    case "$filter":
                        ThrowIfEmpty(kvp.Value, "$filter");
                        RawValues.Filter = kvp.Value;
                        Filter = new FilterQueryOption(kvp.Value, Context, _queryOptionParser);
                        break;
                    case "$orderby":
                        ThrowIfEmpty(kvp.Value, "$orderby");
                        RawValues.OrderBy = kvp.Value;
                        OrderBy = new OrderByQueryOption(kvp.Value, Context, _queryOptionParser, _serviceProvider);
                        break;
                    case "$top":
                        ThrowIfEmpty(kvp.Value, "$top");
                        RawValues.Top = kvp.Value;
                        Top = TryParseNonNegativeInteger("$top", kvp.Value);
                        break;
                    case "$skip":
                        ThrowIfEmpty(kvp.Value, "$skip");
                        RawValues.Skip = kvp.Value;
                        Skip = TryParseNonNegativeInteger("$skip", kvp.Value);
                        SkipQueryOption = new SkipQueryOption(kvp.Value, Context, _queryOptionParser);
                        break;
                    case "$select":
                        RawValues.Select = kvp.Value;
                        break;
                    case "$count":
                        // According to the OData 4 protocol, the value of this query option is optional:
                        // http://docs.oasis-open.org/odata/odata/v4.0/errata02/os/complete/part1-protocol/odata-v4.0-errata02-os-part1-protocol-complete.html#_Toc406398308
                        // "A $count query option with a value of false (or not specified) hints that the service SHOULD NOT return a count."
                        RawValues.Count = kvp.Value;
                        if (string.IsNullOrWhiteSpace(kvp.Value) == false)
                        {
                            bool count;
                            if (bool.TryParse(kvp.Value, out count))
                            {
                                Count = count;
                            }
                            else
                            {
                                throw new ODataException($"If a value for the query '$count' is specified, it must have a value of '{bool.TrueString}' or '{bool.FalseString}'");
                            }
                        }
                        break;
                    case "$expand":
                        RawValues.Expand = kvp.Value;
                        // TODO Parse the select statement if any
                        Request.ODataFeature().SelectExpandClause = _queryOptionParser.ParseSelectAndExpand();
                        SelectExpand = new SelectExpandQueryOption(string.Empty, kvp.Value, Context, _queryOptionParser, Request, _serviceProvider);
                        break;
                    case "$format":
                        RawValues.Format = kvp.Value;
                        break;
                    case "$skiptoken":
                        RawValues.SkipToken = kvp.Value;
                        break;
                }
            }
            if (ODataCountMediaTypeMapping.IsCountRequest(Request.HttpContext))
            {
                CountQueryOption = new CountQueryOption(
                    "true",
                    Context,
                    new ODataQueryOptionParser(
                        Context.Model,
                        Context.ElementType,
                        Context.NavigationSource,
                        new Dictionary<string, string> { { "$count", "true" } }));
            }
        }

        public SkipQueryOption SkipQueryOption { get; set; }

        private static void ThrowIfEmpty(string queryValue, string queryName)
        {
            if (String.IsNullOrWhiteSpace(queryValue))
            {
                throw new ODataException(Error.Format("Query '{0}' cannot be empty", queryName));
            }
        }

        private static int TryParseNonNegativeInteger(string parameterName,
            string value,
            System.Globalization.NumberStyles styles = System.Globalization.NumberStyles.Any,
            IFormatProvider provider = null)
        {
            provider = provider ?? CultureInfo.InvariantCulture;
            int n;
            if (int.TryParse(value, styles, provider, out n) == false)
            {
                throw new ODataException($"Query '{parameterName}' must be an integer.");
            }
            if (n < 0)
            {
                throw new ODataException($"Query '{parameterName}' must be an non-negative integer.");
            }
            return n;
        }
    }
}