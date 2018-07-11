using System.Collections.Generic;
using Iql.Entities;

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

        public string TitlePropertyName { get; set; }
        public string PreviewPropertyName { get; set; }
        public EntityManageKind ManageKind { get; set; }
        public string SetFriendlyName { get; set; }
        public string SetName { get; set; }
        public string DefaultSortExpression { get; set; }
        public bool DefaultSortDescending { get; set; }
    }
}