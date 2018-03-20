using Iql.Queryable.Data.EntityConfiguration;

namespace Brandless.AspNetCore.OData.Extensions
{
    public class PropertyMetadataConfigurator : MetadataConfigurator<IPropertyMetadata, PropertyMetadataConfigurator>
    {
        public PropertyMetadataConfigurator(IPropertyMetadata metadata) : base(metadata) { }

        public PropertyMetadataConfigurator SetReadOnly(bool value = true)
        {
            Metadata.ReadOnly = true;
            return this;
        }

        public PropertyMetadataConfigurator SetConvertedFromType(string convertedFromType)
        {
            Metadata.ConvertedFromType = convertedFromType;
            return this;
        }

        public PropertyMetadataConfigurator SetNullable(bool nullable = true)
        {
            Metadata.Nullable = nullable;
            return this;
        }

        public PropertyMetadataConfigurator SetNullable(PropertyKind kind)
        {
            Metadata.Kind = kind;
            return this;
        }
    }
}