using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.OData.Query
{
    public interface IODataQueryInterceptor<T>
    {
        Expression<Func<T, bool>> Intercept(HttpContext context, ODataQuerySettings querySettings, ODataQueryOptions queryOptions);
    }
}