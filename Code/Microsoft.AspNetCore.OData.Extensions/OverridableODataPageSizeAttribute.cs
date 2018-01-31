using Microsoft.AspNetCore.OData.Query.Paging;

namespace Microsoft.AspNetCore.OData.Extensions
{
    public class OverridableODataPageSizeAttribute : PageSizeAttribute
    {
        private static int? _pageSize;
        private static int? _pageSizeOverride;
        private static bool _pageSizeOverrideSet;

        public override int? DefaultValue
        {
            get
            {
                if (_pageSizeOverrideSet)
                {
                    return _pageSizeOverride;
                }
                return _pageSize;
            }
        }


        public static void OverridePageSize(int? value)
        {
            _pageSizeOverride = value;
            _pageSizeOverrideSet = true;
        }

        public static void RestorePageSize()
        {
            _pageSizeOverrideSet = false;
        }

        public OverridableODataPageSizeAttribute(int value) : base(value)
        {
            _pageSize = value;
        }
    }
}
