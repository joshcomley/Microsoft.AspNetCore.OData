using System;
using System.Collections.Generic;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Display;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Validation;
using Iql.Queryable.Data.EntityConfiguration;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration
{
    public interface IEntityTypeConfiguration
    {
        Type EntityType { get; }
        IRuleMap ValidationMap { get; set; }
        IEntityDisplayTextFormatterMap DisplayTextFormatterMap { get; set; }
        IEntityMetadata Metadata { get; set; }
        Dictionary<string, IPropertyMetadata> PropertyMetadatas { get; }
        IPropertyMetadata PropertyMetadata(string propertyName);
    }
}