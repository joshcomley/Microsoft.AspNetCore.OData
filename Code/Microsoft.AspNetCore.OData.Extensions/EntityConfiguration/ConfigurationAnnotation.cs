using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Vocabularies;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration
{
    internal class ConfigurationAnnotation<TEntity>
    {
        public EdmModel Model { get; }
        public List<IEdmExpression> ChildExpressions { get; } = new List<IEdmExpression>();
        public CollectionAnnotation<TEntity> ValidationAnnotation { get; }
        public ConfigurationAnnotation(EdmModel model, string propertyName = null)
        {
            Model = model;
            var type = Model.GetEdmType(typeof(TEntity)) as EdmEntityType;
            IEdmVocabularyAnnotatable target = type;
            if (propertyName != null)
            {
                target = type.Properties().Single(p => p.Name == propertyName);
            }
            var coll = new EdmCollectionExpression(ChildExpressions);
            var annotation = new EdmVocabularyAnnotation(target, AnnotationManagerBase.IqlConfigurationTerm, coll);
            annotation.SetSerializationLocation(Model, target.ToSerializationLocation());
            Model.AddVocabularyAnnotation(annotation);

            ValidationAnnotation = new CollectionAnnotation<TEntity>("Validations", this, model);
            DisplayFormattingAnnotation = new CollectionAnnotation<TEntity>("DisplayFormatters", this, model);
        }

        public CollectionAnnotation<TEntity> DisplayFormattingAnnotation { get; set; }
    }
}