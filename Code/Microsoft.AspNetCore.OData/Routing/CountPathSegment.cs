using System.Collections.Generic;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Semantic = Microsoft.OData.UriParser;

namespace Microsoft.AspNetCore.OData.Routing
{
    /// <summary>
    /// An <see cref="ODataPathSegment"/> implementation representing a $count segment.
    /// </summary>
    public class CountPathSegment : Semantic.ODataPathSegment
    {
        /// <summary>
        /// Gets the segment kind for the current segment.
        /// </summary>
        public virtual string SegmentKind
        {
            get
            {
                return ODataSegmentKinds.Count;
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return ODataSegmentKinds.Count;
        }

        ///// <inheritdoc/>
        //public virtual bool TryMatch(ODataPathSegment pathSegment, IDictionary<string, object> values)
        //{
        //    return pathSegment.SegmentKind == ODataSegmentKinds.Count;
        //}
        public override T TranslateWith<T>(PathSegmentTranslator<T> translator)
        {
            return default(T);
        }

        public override void HandleWith(PathSegmentHandler handler)
        {
        }

        public override IEdmType EdmType => EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.Int32);
    }
}