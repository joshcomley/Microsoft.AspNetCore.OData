using System;
using System.Diagnostics.Contracts;
using Microsoft.AspNetCore.OData.Common;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace Microsoft.AspNetCore.OData.Query.Validators
{
    /// <summary>
    /// Represents a validator used to validate a <see cref="CountQueryOption"/> 
    /// based on the <see cref="ODataValidationSettings"/>.
    /// </summary>
    public class CountQueryValidator
    {
        /// <summary>
        /// Validates a <see cref="CountQueryOption" />.
        /// </summary>
        /// <param name="countQueryOption">The $count query.</param>
        /// <param name="validationSettings">The validation settings.</param>
        public virtual void Validate(CountQueryOption countQueryOption, ODataValidationSettings validationSettings)
        {
            if (countQueryOption == null)
            {
                throw Error.ArgumentNull("countQueryOption");
            }

            if (validationSettings == null)
            {
                throw Error.ArgumentNull("validationSettings");
            }

            var path = countQueryOption.Context.Path;

            if (path != null && path.Count > 0)
            {
                ODataPathSegment lastSegment = path.LastSegment;
                
                if (lastSegment is CountSegment && path.Segments.Count > 1)
                {
                    ValidateCount(path.Segments[path.Segments.Count - 2], countQueryOption.Context.Model);
                }
                else
                {
                    ValidateCount(lastSegment, countQueryOption.Context.Model);
                }
            }
        }

        private static void ValidateCount(ODataPathSegment segment, IEdmModel model)
        {
            Contract.Assert(segment != null);
            Contract.Assert(model != null);

            NavigationPropertySegment navigationPathSegment = segment as NavigationPropertySegment;
            if (navigationPathSegment != null)
            {
                if (EdmLibHelpers.IsNotCountable(navigationPathSegment.NavigationProperty, model))
                {
                    throw new InvalidOperationException(Error.Format(
                        SRResources.NotCountablePropertyUsedForCount,
                        navigationPathSegment.NavigationProperty.Name));
                }
                return;
            }

            PropertySegment propertyAccessPathSegment = segment as PropertySegment;
            if (propertyAccessPathSegment != null)
            {
                if (EdmLibHelpers.IsNotCountable(propertyAccessPathSegment.Property, model))
                {
                    throw new InvalidOperationException(Error.Format(
                        SRResources.NotCountablePropertyUsedForCount,
                        propertyAccessPathSegment.Property.Name));
                }
            }
        }
    }
}