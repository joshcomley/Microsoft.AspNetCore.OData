using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Display;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Metadata;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Reports;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Validation;
using Iql.Queryable.Data.EntityConfiguration;
using Microsoft.OData.Edm;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration
{
    public class EntityTypeConfiguration<TEntity> : IEntityTypeConfiguration
    {
        internal EntityTypeConfiguration(EdmModel model)
        {
            Model = model;
            ValidationMap = new EntityValidationMap<TEntity>();
            DisplayTextFormatterMap = new EntityDisplayTextFormatterMap<TEntity>();
            AnnotationsManager = new AnnotationManager<TEntity>(model);
            ReportDefinitions = new ReportDefinitionMap<TEntity>();
        }

        public EdmModel Model { get; }
        public IEntityMetadata Metadata { get; set; }
        public EntityValidationMap<TEntity> ValidationMap { get; set; }
        internal AnnotationManager<TEntity> AnnotationsManager { get; }
        public ReportDefinitionMap<TEntity> ReportDefinitions { get; }
        public EntityDisplayTextFormatterMap<TEntity> DisplayTextFormatterMap { get; set; }

        IEntityValidationMap IEntityTypeConfiguration.ValidationMap
        {
            get => ValidationMap;
            set => ValidationMap = (EntityValidationMap<TEntity>) value;
        }

        IEntityDisplayTextFormatterMap IEntityTypeConfiguration.DisplayTextFormatterMap
        {
            get => DisplayTextFormatterMap;
            set => DisplayTextFormatterMap = (EntityDisplayTextFormatterMap<TEntity>) value;
        }
    }
}