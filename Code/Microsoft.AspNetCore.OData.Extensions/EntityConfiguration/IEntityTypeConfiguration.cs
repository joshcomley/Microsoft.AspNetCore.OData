using System;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration
{
    public interface IEntityTypeConfiguration
    {
        Type EntityType { get; }
    }
}