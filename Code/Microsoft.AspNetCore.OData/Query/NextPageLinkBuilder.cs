using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.OData.Query
{
    internal class NextPageLinkBuilder
    {
        internal static Uri GetNextPageLink(Uri requestUri, IQueryCollection queryParameters, int pageSize)
        {
            Contract.Assert(requestUri != null);
            Contract.Assert(queryParameters != null);
            Contract.Assert(requestUri.IsAbsoluteUri);

            var queryBuilder = new StringBuilder();

            var nextPageSkip = pageSize;

            foreach (var kvp in queryParameters)
            {
                var key = kvp.Key;
                string value = kvp.Value;
                switch (key)
                {
                    case "$top":
                        int top;
                        if (Int32.TryParse(value, out top))
                        {
                            // There is no next page if the $top query option's value is less than or equal to the page size.
                            //Contract.Assert(top > pageSize);
                            // We decrease top by the pageSize because that's the number of results we're returning in the current page
                            value = (top - pageSize).ToString(CultureInfo.InvariantCulture);
                        }
                        break;
                    case "$skip":
                        int skip;
                        if (Int32.TryParse(value, out skip))
                        {
                            // We increase skip by the pageSize because that's the number of results we're returning in the current page
                            nextPageSkip += skip;
                        }
                        continue;
                    default:
                        break;
                }

                if (key.Length > 0 && key[0] == '$')
                {
                    // $ is a legal first character in query keys
                    key = '$' + Uri.EscapeDataString(key.Substring(1));
                }
                else
                {
                    key = Uri.EscapeDataString(key);
                }
                value = Uri.EscapeDataString(value);

                queryBuilder.Append(key);
                queryBuilder.Append('=');
                queryBuilder.Append(value);
                queryBuilder.Append('&');
            }

            queryBuilder.AppendFormat("$skip={0}", nextPageSkip);

            UriBuilder uriBuilder = new UriBuilder(requestUri)
            {
                Query = queryBuilder.ToString()
            };
            return uriBuilder.Uri;
        }
    }
}