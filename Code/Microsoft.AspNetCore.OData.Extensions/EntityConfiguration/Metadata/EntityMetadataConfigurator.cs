using System;
using System.Linq;
using System.Linq.Expressions;
using Iql.Conversion;
using Iql.Entities;

namespace Brandless.AspNetCore.OData.Extensions
{
    public class EntityMetadataConfigurator<T> : EntityMetadataConfigurator
    {
        public EntityMetadataConfigurator(IEntityMetadata metadata) : base(metadata) { }

        public EntityMetadataConfigurator SetPropertyOrder(params Expression<Func<T, object>>[] propertyExpression)
        {
            SetPropertyOrder(propertyExpression.Select(p => IqlConverter.Instance.GetPropertyName(p)).ToArray());
            return this;
        }
    }
    public class EntityMetadataConfigurator : MetadataConfigurator<IEntityMetadata, EntityMetadataConfigurator>
    {
        public EntityMetadataConfigurator(IEntityMetadata metadata) : base(metadata)
        {
        }

        public EntityMetadataConfigurator SetDefaultSortExpression(string expression)
        {
            Metadata.DefaultSortExpression = expression;
            return this;
        }

        public EntityMetadataConfigurator SetTitlePropertyName(string name)
        {
            Metadata.TitlePropertyName = name;
            return this;
        }

        public EntityMetadataConfigurator SetPreviewPropertyName(string name)
        {
            Metadata.PreviewPropertyName = name;
            return this;
        }

        public EntityMetadataConfigurator SetDefaultSortDescending(bool descending = true)
        {
            Metadata.DefaultSortDescending = descending;
            return this;
        }

        public EntityMetadataConfigurator SetPropertyOrder(params string[] properties)
        {
            Metadata.PropertyOrder = properties.ToList();
            return this;
        }

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

        public EntityMetadataConfigurator SetManageKind(EntityManageKind kind)
        {
            Metadata.ManageKind = kind;
            return this;
        }
    }
}