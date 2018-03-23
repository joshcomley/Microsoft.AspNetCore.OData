using System.Collections.Generic;
using Iql.Queryable.Data.EntityConfiguration;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Metadata
{
    public class MetadataBase : IMetadata
    {
        public string ResolveFriendlyName()
        {
            throw new System.NotImplementedException();
        }

        public string ResolveName()
        {
            throw new System.NotImplementedException();
        }

        public string Description { get; set; }
        public string FriendlyName { get; set; }
        public List<string> Hints { get; set; } = new List<string>();
        public string Name { get; set; }
        public string Title { get; set; }
    }
}