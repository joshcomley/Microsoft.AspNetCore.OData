using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Display;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Validation;
using Microsoft.OData.Edm;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration
{
    public class EntityTypeConfiguration<TEntity> : IEntityTypeConfiguration
    {
        public EdmModel Model { get; }
        public EntityValidationMap<TEntity> ValidationMap { get; set; }
        internal AnnotationManager<TEntity> AnnotationsManager { get; }
        IEntityValidationMap IEntityTypeConfiguration.ValidationMap
        {
            get => ValidationMap;
            set => ValidationMap = (EntityValidationMap<TEntity>)value;
        }
        public EntityDisplayTextFormatterMap<TEntity> DisplayTextFormatterMap { get; set; }
        IEntityDisplayTextFormatterMap IEntityTypeConfiguration.DisplayTextFormatterMap
        {
            get => DisplayTextFormatterMap;
            set => DisplayTextFormatterMap = (EntityDisplayTextFormatterMap<TEntity>)value;
        }

        internal EntityTypeConfiguration(EdmModel model)
        {
            Model = model;
            ValidationMap = new EntityValidationMap<TEntity>();
            DisplayTextFormatterMap = new EntityDisplayTextFormatterMap<TEntity>();
            AnnotationsManager = new AnnotationManager<TEntity>(model);
        }
    }
}