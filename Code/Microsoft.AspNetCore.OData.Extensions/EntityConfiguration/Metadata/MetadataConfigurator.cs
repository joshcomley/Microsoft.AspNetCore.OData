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

        public TReturn AddHint(string hint)
        {
            Metadata.Hints = Metadata.Hints ?? new List<string>();
            if (!Metadata.Hints.Contains(hint))
            {
                Metadata.Hints.Add(hint);
            }
            return (TReturn)this;
        }

        public TReturn RemoveHint(string hint)
        {
            if (Metadata.Hints == null || !Metadata.Hints.Any())
            {
                return (TReturn)this;
            }
            Metadata.Hints.Remove(hint);
            return (TReturn)this;
        }
    }
}