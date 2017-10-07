namespace Microsoft.AspNetCore.OData.Query.Paging
{
    public class ActionPageSize
    {
        public bool IsSet { get; set; }
        public int? Size { get; set; }

        public ActionPageSize() { }

        public ActionPageSize(bool isSet, int? size)
        {
            IsSet = isSet;
            Size = size;
        }
    }
}