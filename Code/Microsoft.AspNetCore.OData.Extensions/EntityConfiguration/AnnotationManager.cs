using System.Collections.Generic;
using Microsoft.OData.Edm;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration
{
    internal class AnnotationManager<TEntity> : AnnotationManagerBase
    {
        private ConfigurationAnnotation<TEntity> EntityConfigurationAnnotation { get; }
        private Dictionary<string, ConfigurationAnnotation<TEntity>> PropertyConfigurationAnnotations { get; }
        = new Dictionary<string, ConfigurationAnnotation<TEntity>>();
        internal AnnotationManager(EdmModel model)
        {
            Model = model;
            EntityConfigurationAnnotation = new ConfigurationAnnotation<TEntity>(model);
        }

        public EdmModel Model { get; }
    }
}