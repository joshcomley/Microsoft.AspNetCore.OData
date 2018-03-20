using Iql.Queryable.Data.EntityConfiguration;

namespace Brandless.AspNetCore.OData.Extensions
{
    public class EntityMetadataConfigurator : MetadataConfigurator<IEntityMetadata, EntityMetadataConfigurator>
    {
        public EntityMetadataConfigurator(IEntityMetadata metadata) : base(metadata) { }

        public EntityMetadataConfigurator SetEntitySetFriendlyName(string friendlyName)
        {
            Metadata.SetFriendlyName = friendlyName;
            return this;
        }

        public EntityMetadataConfigurator SetEntitySetName(string name)
        {
            Metadata.SetName = name;
            return this;
        }
    }
}