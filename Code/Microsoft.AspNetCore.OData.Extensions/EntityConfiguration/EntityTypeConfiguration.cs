using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Display;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Metadata;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Reports;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Validation;
using Brandless.AspNetCore.OData.Extensions.Extensions;
using Iql.Entities;
using Microsoft.OData.Edm;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration
{
    public class EntityTypeConfiguration<TEntity> : EntityTypeConfigurationBase, IEntityTypeConfiguration
    {
        public Type EntityType => typeof(TEntity);
        internal EntityTypeConfiguration(EdmModel model)
        {
            Model = model;
            ValidationMap = new EntityValidationMap<TEntity>();
            DisplayRuleMap = new DisplayRuleMap<TEntity>();
            DisplayTextFormatterMap = new EntityDisplayTextFormatterMap<TEntity>();
            RelationshipFilterMap = new RelationshipFilterRuleMap<TEntity>();
            AnnotationsManager = new AnnotationManager<TEntity>(model);
            ReportDefinitions = new ReportDefinitionMap<TEntity>();
        }

        public EdmModel Model { get; }
        public IEntityMetadata Metadata { get; set; } = new EntityMetadata();
        public EntityValidationMap<TEntity> ValidationMap { get; set; }
        public DisplayRuleMap<TEntity> DisplayRuleMap { get; set; }
        public RelationshipFilterRuleMap<TEntity> RelationshipFilterMap { get; set; }
        internal AnnotationManager<TEntity> AnnotationsManager { get; }
        public ReportDefinitionMap<TEntity> ReportDefinitions { get; }

        public Dictionary<string, IPropertyMetadata> PropertyMetadatas { get; }
            = new Dictionary<string, IPropertyMetadata>();

        public IPropertyMetadata PropertyMetadata(Expression<Func<TEntity, object>> propertyExpression)
        {
            return PropertyMetadata(propertyExpression.GetAccessedProperty().Name);
        }

        public IPropertyMetadata PropertyMetadata(string propertyName)
        {
            if (!PropertyMetadatas.ContainsKey(propertyName))
            {
                PropertyMetadatas.Add(propertyName, new PropertyMetadata());
            }
            return PropertyMetadatas[propertyName];
        }

        public EntityDisplayTextFormatterMap<TEntity> DisplayTextFormatterMap { get; set; }

        IRuleMap IEntityTypeConfiguration.ValidationMap
        {
            get => ValidationMap;
            set => ValidationMap = (EntityValidationMap<TEntity>) value;
        }

        IEntityDisplayTextFormatterMap IEntityTypeConfiguration.DisplayTextFormatterMap
        {
            get => DisplayTextFormatterMap;
            set => DisplayTextFormatterMap = (EntityDisplayTextFormatterMap<TEntity>) value;
        }

        internal override AnnotationManagerBase AnnotationsManagerBase => AnnotationsManager;
    }
}