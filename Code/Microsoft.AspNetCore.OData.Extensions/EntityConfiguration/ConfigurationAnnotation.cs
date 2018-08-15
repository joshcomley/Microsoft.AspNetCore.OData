using System.Collections.Generic;
using System.Linq;
using Brandless.AspNetCore.OData.Extensions.Extensions;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Vocabularies;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration
{
    internal class ConfigurationAnnotation<TEntity>
    {
        private EdmLabeledExpression _metadataAnnotation;

        public ConfigurationAnnotation(EdmModel model, string propertyName = null)
        {
            Model = model;
            var type = Model.GetEdmType(typeof(TEntity)) as EdmEntityType;
            IEdmVocabularyAnnotatable target = type;
            if (propertyName != null)
            {
                target = type.Properties().SingleOrDefault(p => p.Name == propertyName);
                if (target == null)
                {
                    Valid = false;
                }
            }

            if (Valid)
            {
                var coll = new EdmCollectionExpression(ChildExpressions);
                var annotation = new EdmVocabularyAnnotation(target, AnnotationManagerBase.IqlConfigurationTerm, coll);
                annotation.SetSerializationLocation(Model, target.ToSerializationLocation());
                Model.AddVocabularyAnnotation(annotation);
            }
        }

        public bool Valid { get; set; } = true;
        public EdmModel Model { get; }
        public List<EdmLabeledExpression> ChildExpressions { get; } = new List<EdmLabeledExpression>();
    }
}