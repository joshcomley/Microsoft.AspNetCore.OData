using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OData.EntityFramework.Security;

namespace Microsoft.AspNetCore.OData.EntityFramework.Extensions
{
    public static class FilterDescriptorExtensions
    {
        public static bool AnyOfHigherScope<TFilter>(this IEnumerable<FilterDescriptor> filterDescriptors, TFilter currentFilter)
        {
            var descriptors = filterDescriptors as FilterDescriptor[] ?? filterDescriptors.ToArray();
            var list = descriptors
                .Where(d => d.Filter is SecureRequestAttribute).Select(descriptor =>
                    new
                    {
                        Filter = descriptor.Filter as SecureRequestAttribute,
                        Descriptor = descriptor
                    }).ToArray();
            var current = list
                .Single(f => ReferenceEquals(f.Filter, currentFilter));
            return list.Any(l => !ReferenceEquals(l.Filter, currentFilter) && l.Descriptor.Scope > current.Descriptor.Scope);
        }
    }
}
