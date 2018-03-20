using System.Collections.Generic;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Vocabularies;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration
{
    internal class CollectionAnnotation<TEntity>
    {
        private bool _initialized;
        public string Key { get; }
        public ConfigurationAnnotation<TEntity> Root { get; }
        private List<IEdmExpression> ChildExpressions { get; } = new List<IEdmExpression>();
        public EdmModel Model { get; }

        public CollectionAnnotation(string key, ConfigurationAnnotation<TEntity> root, EdmModel model)
        {
            Key = key;
            Root = root;
            Model = model;
        }

        public void Add(IEdmExpression expression)
        {
            if (!_initialized)
            {
                _initialized = true;
                var entityValidationRoot = new EdmLabeledExpression(Key,
                    new EdmCollectionExpression(ChildExpressions));
                Root.ChildExpressions.Add(entityValidationRoot);
            }
            ChildExpressions.Add(expression);
        }
    }
}