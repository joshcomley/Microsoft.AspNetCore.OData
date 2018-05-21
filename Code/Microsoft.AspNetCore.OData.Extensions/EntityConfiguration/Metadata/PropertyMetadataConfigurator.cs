﻿using Iql.Entities;

namespace Brandless.AspNetCore.OData.Extensions
{
    public class PropertyMetadataConfigurator : MetadataConfigurator<IPropertyMetadata, PropertyMetadataConfigurator>
    {
        public PropertyMetadataConfigurator(IPropertyMetadata metadata) : base(metadata) { }

        public PropertyMetadataConfigurator SetReadOnlyAndHidden(bool value = true)
        {
            Metadata.ReadOnly = value;
            Metadata.Hidden = value;
            return this;
        }

        public PropertyMetadataConfigurator SetReadOnly(bool value = true)
        {
            Metadata.ReadOnly = value;
            return this;
        }

        public PropertyMetadataConfigurator SetHidden(bool value = true)
        {
            Metadata.Hidden = value;
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

        public PropertyMetadataConfigurator SetNullable(bool nullable = true)
        {
            Metadata.Nullable = nullable;
            return this;
        }
    }
}