﻿using Iql.Queryable.Data.EntityConfiguration;

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

        public PropertyMetadataConfigurator SetKind(PropertyKind kind)
        {
            Metadata.Kind = kind;
            return this;
        }

        public PropertyMetadataConfigurator SetPlaceholder(string placeholder)
        {
            Metadata.Placeholder = placeholder;
            return this;
        }
    }
}