using Iql.Queryable.Data.EntityConfiguration;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Metadata
{
    public class PropertyMetadata : MetadataBase, IPropertyMetadata
    {
        public string Placeholder { get; set; }
        public string ConvertedFromType { get; set; }
        public PropertyKind Kind { get; set; }
        public PropertySearchKind SearchKind { get; set; }
        public bool? Nullable { get; set; }
        public bool ReadOnly { get; set; }
    }
}