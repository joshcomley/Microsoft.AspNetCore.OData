using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.OData.Query.Paging
{
    public class PageSizeAttribute : Attribute
	{
		public virtual int? DefaultValue { get; }

		public PageSizeAttribute(PageSize pageSize)
		{

		}

		public PageSizeAttribute(int defaultValue)
		{
			DefaultValue = defaultValue;
		}

	    public virtual Task<int?> GetValueAsync(ActionExecutedContext context)
	    {
	        return Task.FromResult(DefaultValue);
	    }
	}
}