// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.OData.UriParser.Aggregation;
using ODataPath = Microsoft.AspNetCore.OData.Routing.ODataPath;

namespace Microsoft.AspNetCore.OData.Abstracts
{
    /// <summary>
    /// Provide the interface for the details of a given OData request.
    /// </summary>
    public interface IODataFeature
    {
        /// <summary>
        /// Gets or sets the EDM model.
        /// </summary>
        IEdmModel Model { get; set; }

        /// <summary>
        /// Gets or sets the OData path.
        /// </summary>
        ODataPath Path { get; set; }

        /// <summary>
        /// Gets or sets the route prerix.
        /// </summary>
        string RoutePrefix { get; set; }

        /// <summary>
        /// Gets or sets whether the request is the valid OData request.
        /// </summary>
        bool IsValidODataRequest { get; set; }

        /// <summary>
        /// Gets or sets the next link for the OData response.
        /// </summary>
        Uri NextLink { get; set; }

        /// <summary>
        /// Gets or sets the total count for the OData response.
        /// </summary>
        /// <value><c>null</c> if no count should be sent back to the client.</value>
        long? TotalCount { get; set; }

        Func<long> TotalCountFunc { get; set; }
        
        /// <summary>
        /// Gets or sets the parsed OData <see cref="SelectExpandClause"/> of the request.
        /// </summary>
        SelectExpandClause SelectExpandClause { get; set; }

        /// <summary>
        /// Gets the data store used by <see cref="IODataRoutingConvention"/>s to store any custom route data.
        /// </summary>
        /// <value>Initially an empty <c>IDictionary&lt;string, object&gt;</c>.</value>
        IDictionary<string, object> RoutingConventionsStore { get; set; }

        ODataUriResolverSettings UriResolverSettings { get; set; }

        /// <summary>
        /// Gets or sets the parsed OData <see cref="ApplyClause"/> of the request. The
        /// <see cref="ODataMediaTypeFormatter"/> will use this information (if any) while writing the response for
        /// this request.
        /// </summary>
        ApplyClause ApplyClause { get; set; }
        //TODO: Add more OData features below
    }
}
