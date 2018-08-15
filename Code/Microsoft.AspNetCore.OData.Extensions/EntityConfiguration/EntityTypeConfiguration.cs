using System;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Reports;
using Microsoft.OData.Edm;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration
{
    public class EntityTypeConfiguration<TEntity> : EntityTypeConfigurationBase, IEntityTypeConfiguration
    {
        public Type EntityType => typeof(TEntity);
        internal EntityTypeConfiguration(EdmModel model)
        {
            Model = model;
            AnnotationsManager = new AnnotationManager<TEntity>(model);
            ReportDefinitions = new ReportDefinitionMap<TEntity>();
        }

        public EdmModel Model { get; }
        internal AnnotationManager<TEntity> AnnotationsManager { get; }
        public ReportDefinitionMap<TEntity> ReportDefinitions { get; }
        internal override AnnotationManagerBase AnnotationsManagerBase => AnnotationsManager;
    }
}