using System;
using System.Reflection;
using Microsoft.AspNetCore.OData.Builder;

namespace Microsoft.AspNetCore.OData.Extensions
{
    public static class PropertyInfoExtensions
    {
        internal static bool IsIgnored(this PropertyInfo property,
            IEdmTypeConfiguration configuration)
        {
            return property.IsIgnored(configuration.ClrType, configuration);
        }

        internal static bool IsIgnored(this PropertyInfo property,
            Type clrType,
            params IEdmTypeConfiguration[] configurations)
        {
            var config = property.GetConfiguration(clrType, configurations);
            if (config != null)
            {
                return config.IsIgnored;
            }
            return true;
        }

        internal static PropertyConfiguration GetConfiguration(this PropertyInfo property, 
            IEdmTypeConfiguration configuration)
        {
            return property.GetConfiguration(configuration.ClrType, configuration);
        }

        internal static PropertyConfiguration GetConfiguration(this PropertyInfo property, Type clrType, params IEdmTypeConfiguration[] configurations)
        {
            foreach (var config in configurations)
            {
                if (config.ClrType == clrType)
                {
                    var structuralTypeConfiguration = config as StructuralTypeConfiguration;
                    if (structuralTypeConfiguration != null && structuralTypeConfiguration.ExplicitProperties.ContainsKey(property))
                    {
                        return structuralTypeConfiguration.ExplicitProperties[property];
                    }
                    return null;
                }
            }
            return null;
        }
    }
}