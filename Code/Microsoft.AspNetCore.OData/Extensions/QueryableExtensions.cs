using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Formatter;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.OData.Extensions
{
    public static class QueryableExtensions
    {
        internal class ODataResponse<T>
        {
            public List<T> Value { get; set; }
        }

        public static async Task<List<T>> ToListWithODataRequestAsync<T>(
            this IQueryable<T> queryable,
            HttpRequest request)
        {
            var json = await ModernOutputFormatter.SerializeToJsonAsync(queryable, request);
            var data = JsonConvert.DeserializeObject<ODataResponse<T>>(json);
            return data.Value;
        }

    }
}