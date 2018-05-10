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

        public MetadataHint FindHint(string name)
        {
            return HintHelper.FindHint(this, name);
        }

        public bool HasHint(string name)
        {
            return HintHelper.HasHint(this, name);
        }

        public void SetHint(string name, string value = null)
        {
            HintHelper.SetHint(this, name, value);
        }

        public void RemoveHint(string name)
        {
            HintHelper.RemoveHint(this, name);
        }

        public string GroupPath { get; set; }
        public string Description { get; set; }
        public string FriendlyName { get; set; }
        public List<string> Hints { get; set; } = new List<string>();
        public string Name { get; set; }
        public string Title { get; set; }
    }
}