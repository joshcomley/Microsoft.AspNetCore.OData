using Iql.Queryable.Data.EntityConfiguration;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Metadata
{
    public class EntityMetadata : MetadataBase, IEntityMetadata
    {
        public string ResolveSetFriendlyName()
        {
            throw new System.NotImplementedException();
        }

        public string ResolveSetName()
        {
            throw new System.NotImplementedException();
        }

        public string SetFriendlyName { get; set; }
        public string SetName { get; set; }
    }
}