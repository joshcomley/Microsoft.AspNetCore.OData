// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Abstracts;
using Microsoft.AspNetCore.OData.Common;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.OData.UriParser.Aggregation;
using ODataPath = Microsoft.AspNetCore.OData.Routing.ODataPath;

namespace Microsoft.AspNetCore.OData
{
    /// <summary>
    /// Contains the details of a given OData request. These properties should all be mutable.
    /// None of these properties should ever be set to null.
    /// </summary>
    public class ODataFeature : IODataFeature
    {
        private const string TotalCountFuncKey = "Microsoft.AspNetCore.OData.TotalCountFunc";
        private const string TotalCountKey = "Microsoft.AspNetCore.OData.TotalCount";
        private const string ApplyClauseKey = "Microsoft.AspNetCore.OData.ApplyClause";
        private readonly HttpContext _context;
        internal const string ODataServiceVersionHeader = "OData-Version";
        internal const ODataVersion DefaultODataVersion = ODataVersion.V4;

        public ODataFeature(HttpContext context)
        {
            _context = context;
            Model = context.ODataModel();

            //Model = EdmCoreModel.Instance; // default Edm model
            RoutingConventionsStore = new ConcurrentDictionary<string, object>();
            UriResolverSettings = new ODataUriResolverSettings
            {
                UnqualifiedNameCall = true,
                EnumPrefixFree = true
            };
        }

        public ODataUriResolverSettings UriResolverSettings { get; set; }

        /// <summary>
        /// Gets or sets the EDM model.
        /// </summary>
        public IEdmModel Model { get; set; }

        /// <summary>
        /// Gets or sets the OData path.
        /// </summary>
        public ODataPath Path { get; set; }

        /// <summary>
        /// Gets or sets the route prerix.
        /// </summary>
        public string RoutePrefix { get; set; }

        /// <summary>
        /// Gets or sets whether the request is the valid OData request.
        /// </summary>
        public bool IsValidODataRequest { get; set; }

        /// <summary>
        /// Gets or sets the next link for the OData response.
        /// </summary>
        public Uri NextLink { get; set; }

        private Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();
        public Func<long> TotalCountFunc
        {
            get
            {
                object totalCountFunc;
                if (Properties.TryGetValue(TotalCountFuncKey, out totalCountFunc))
                {
                    return (Func<long>)totalCountFunc;
                }

                return null;
            }
            set { Properties[TotalCountFuncKey] = value; }
        }

        /// <summary>
        /// Gets or sets the total count for the OData response.
        /// </summary>
        /// <value><c>null</c> if no count should be sent back to the client.</value>
        public long? TotalCount
        {
            get
            {
                object totalCount;
                if (Properties.TryGetValue(TotalCountKey, out totalCount))
                {
                    // Fairly big problem if following cast fails. Indicates something else is writing properties with
                    // names we've chosen. Do not silently return null because that will hide the problem.
                    return (long)totalCount;
                }

                if (TotalCountFunc != null)
                {
                    var count = TotalCountFunc();
                    Properties[TotalCountKey] = count;
                    return count;
                }

                return null;
            }
            set
            {
                if (!value.HasValue)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                Properties[TotalCountKey] = value;
            }
        }

        /// <summary>
        /// Gets or sets the parsed OData <see cref="SelectExpandClause"/> of the request.
        /// </summary>
        public SelectExpandClause SelectExpandClause { get; set; }

        /// <summary>
        /// Gets the data store used by <see cref="IODataRoutingConvention"/>s to store any custom route data.
        /// </summary>
        /// <value>Initially an empty <c>IDictionary&lt;string, object&gt;</c>.</value>
        public IDictionary<string, object> RoutingConventionsStore { get; set; }

        /// <summary>
        /// Gets or sets the parsed OData <see cref="ApplyClause"/> of the request. The
        /// <see cref="ODataMediaTypeFormatter"/> will use this information (if any) while writing the response for
        /// this request.
        /// </summary>
        public ApplyClause ApplyClause
        {
            get
            {
                return GetValueOrNull<ApplyClause>(ApplyClauseKey);
            }
            set
            {
                if (value == null)
                {
                    throw Error.ArgumentNull("value");
                }

                Properties[ApplyClauseKey] = value;
            }
        }

        private T GetValueOrNull<T>(string propertyName) where T : class
        {
            object value;
            if (Properties.TryGetValue(propertyName, out value))
            {
                // Fairly big problem if following cast fails. Indicates something else is writing properties with
                // names we've chosen. Do not silently return null because that will hide the problem.
                return (T)value;
            }

            return null;
        }
        // TODO: and more features below.
    }
}
