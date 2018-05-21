﻿using System.Collections.Generic;
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

        public EntityManageKind ManageKind { get; set; }
        public string SetFriendlyName { get; set; }
        public string SetName { get; set; }
        public List<string> PropertyOrder { get; set; }
    }
}