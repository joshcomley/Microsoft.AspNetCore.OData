using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.OData.Abstracts;
using Microsoft.AspNetCore.OData.Common;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;

namespace Microsoft.AspNetCore.OData.Extensions
{
    public static class HttpRequestExtensions
    {
        public static IODataFeature ODataFeature(this HttpRequest request)
        {
            if (request == null)
            {
                throw Error.ArgumentNull("request");
            }

            return request.HttpContext.ODataFeature();
        }

        public static IETagHandler ETagHandler(this HttpRequest request)
        {
            if (request == null)
            {
                throw Error.ArgumentNull("request");
            }

            return request.HttpContext.ETagHandler();
        }

        public static IAssemblyProvider AssemblyProvider(this HttpRequest request)
        {
            if (request == null)
            {
                throw Error.ArgumentNull("request");
            }

            return request.HttpContext.AssemblyProvider();
        }

        public static bool HasQueryOptions(this HttpRequest request)
        {
            if (request == null)
            {
                throw Error.ArgumentNull("request");
            }

            return request?.Query != null && request.Query.Count > 0;
        }

        /// <summary>
        /// Creates a link for the next page of results; To be used as the value of @odata.nextLink.
        /// </summary>
        /// <param name="request">The request on which to base the next page link.</param>
        /// <param name="pageSize">The number of results allowed per page.</param>
        /// <returns>A next page link.</returns>
        public static Uri GetNextPageLink(this HttpRequest request, int pageSize)
        {
            var requestUriString = request?.GetDisplayUrl();
            if (requestUriString == null)
            {
                throw Error.ArgumentNull("request");
            }

            var requestUri = new Uri(requestUriString);
            if (!requestUri.IsAbsoluteUri)
            {
                throw Error.ArgumentUriNotAbsolute("request", requestUri);
            }

            return NextPageLinkBuilder.GetNextPageLink(requestUri, request.Query, pageSize);
        }
    }
}