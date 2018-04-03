using System.Collections.Generic;
using System.Linq;
using Iql.Queryable.Data.EntityConfiguration;

namespace Brandless.AspNetCore.OData.Extensions
{
    public class MetadataConfigurator<T, TReturn>
        where T : IMetadata
        where TReturn : MetadataConfigurator<T, TReturn>
    {
        public T Metadata { get; }
        public MetadataConfigurator(T metadata)
        {
            Metadata = metadata;
        }

        public TReturn SetFriendlyName(string friendlyName)
        {
            Metadata.FriendlyName = friendlyName;
            return (TReturn)this;
        }
        public TReturn SetDescriptiton(string description)
        {
            Metadata.Description = description;
            return (TReturn)this;
        }
        public TReturn Name(string name)
        {
            Metadata.FriendlyName = name;
            return (TReturn)this;
        }
        public TReturn SetTitle(string title)
        {
            Metadata.Title = title;
            return (TReturn)this;
        }

        public TReturn SetHint(string name, string value = null)
        {
            Metadata.SetHint(name, value);
            return (TReturn)this;
        }

        public TReturn RemoveHint(string name)
        {
            Metadata.RemoveHint(name);
            return (TReturn)this;
        }
    }
}