using System;

namespace Microsoft.AspNetCore.OData.Query.Paging
{
    public class PageSizeAttribute : Attribute
	{
		public virtual int? Value { get; }

		public PageSizeAttribute(PageSize pageSize)
		{

		}

		public PageSizeAttribute(int value)
		{
			Value = value;
		}
	}
}