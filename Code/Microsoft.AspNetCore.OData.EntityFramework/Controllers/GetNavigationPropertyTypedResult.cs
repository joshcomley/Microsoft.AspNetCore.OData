using System.Linq;

namespace Microsoft.AspNetCore.OData.EntityFramework.Controllers
{
    public class GetNavigationPropertyTypedResult
    {
        public IQueryable Queryable { get; }
        public bool IsSingleResult { get; }
        public SingleResult SingleResult { get; }

        public GetNavigationPropertyTypedResult(IQueryable queryable, bool isSingleResult, SingleResult singleResult)
        {
            Queryable = queryable;
            IsSingleResult = isSingleResult;
            SingleResult = singleResult;
        }
    }
}