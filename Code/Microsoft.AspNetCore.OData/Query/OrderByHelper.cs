using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace Microsoft.AspNetCore.OData.Query
{
    internal class OrderByHelper
    {
        // Generates the OrderByQueryOption to use by default for $skip or $top
        // when no other $orderby is available.  It will produce a stable sort.
        // This may return a null if there are no available properties.
        internal static OrderByQueryOption GenerateDefaultOrderBy(ODataQueryContext context, IServiceProvider serviceProvider)
        {
            string orderByRaw = String.Empty;
            if (context.ElementClrType.IsDynamicTypeWrapper())
            {
                orderByRaw = String.Join(",",
                    context.ElementClrType.GetTypeInfo()
                        .DeclaredProperties
                        .Where(property => EdmLibHelpers.GetEdmPrimitiveTypeOrNull(property.PropertyType) != null)
                        .Select(property => property.Name));
            }
            else
            {
                orderByRaw = String.Join(",",
                    GetAvailableOrderByProperties(context)
                        .Select(property => property.Name));
            }

            return String.IsNullOrEmpty(orderByRaw)
                ? null
                : new OrderByQueryOption(orderByRaw, context, serviceProvider);
        }

        /// <summary>
        /// Ensures the given <see cref="OrderByQueryOption"/> will produce a stable sort.
        /// If it will, the input <paramref name="orderBy"/> will be returned
        /// unmodified.  If the given <see cref="OrderByQueryOption"/> will not produce a
        /// stable sort, a new <see cref="OrderByQueryOption"/> instance will be created
        /// and returned.
        /// </summary>
        /// <param name="orderBy">The <see cref="OrderByQueryOption"/> to evaluate.</param>
        /// <param name="context">The <see cref="ODataQueryContext"/>.</param>
        /// <param name="serviceProvider"></param>
        /// <returns>An <see cref="OrderByQueryOption"/> that will produce a stable sort.</returns>
        internal static OrderByQueryOption EnsureStableSortOrderBy(OrderByQueryOption orderBy, ODataQueryContext context, IServiceProvider serviceProvider)
        {
            Contract.Assert(orderBy != null);
            Contract.Assert(context != null);

            // Strategy: create a hash of all properties already used in the given OrderBy
            // and remove them from the list of properties we need to add to make the sort stable.
            HashSet<string> usedPropertyNames =
                new HashSet<string>(orderBy.OrderByNodes.OfType<OrderByPropertyNode>().Select(node => node.Property.Name));

            IEnumerable<IEdmStructuralProperty> propertiesToAdd = GetAvailableOrderByProperties(context).Where(prop => !usedPropertyNames.Contains(prop.Name));

            if (propertiesToAdd.Any())
            {
                // The existing query options has too few properties to create a stable sort.
                // Clone the given one and add the remaining properties to end, thereby making
                // the sort stable but preserving the user's original intent for the major
                // sort order.
                orderBy = new OrderByQueryOption(orderBy, serviceProvider);

                foreach (IEdmStructuralProperty property in propertiesToAdd)
                {
                    orderBy.OrderByNodes.Add(new OrderByPropertyNode(property, OrderByDirection.Ascending));
                }
            }

            return orderBy;
        }


        // Returns a sorted list of all properties that may legally appear
        // in an OrderBy.  If the entity type has keys, all are returned.
        // Otherwise, when no keys are present, all primitive properties are returned.
        private static IEnumerable<IEdmStructuralProperty> GetAvailableOrderByProperties(ODataQueryContext context)
        {
            Contract.Assert(context != null);

            IEdmEntityType entityType = context.ElementType as IEdmEntityType;
            if (entityType != null)
            {
                IEnumerable<IEdmStructuralProperty> properties =
                    entityType.Key().Any()
                        ? entityType.Key()
                        : entityType
                            .StructuralProperties()
                            .Where(property => property.Type.IsPrimitive());

                // Sort properties alphabetically for stable sort
                return properties.OrderBy(property => property.Name);
            }
            else
            {
                return Enumerable.Empty<IEdmStructuralProperty>();
            }
        }
    }
}