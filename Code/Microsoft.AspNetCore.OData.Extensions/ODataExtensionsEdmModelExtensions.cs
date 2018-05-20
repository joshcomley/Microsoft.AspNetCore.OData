using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Reports;
using Brandless.AspNetCore.OData.Extensions.Extensions;
using Iql.Entities;
using Iql.Entities.Rules.Display;
using Iql.Entities.Rules.Relationship;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using IEdmEntityContainer = Microsoft.OData.Edm.IEdmEntityContainer;
using IEdmVocabularyAnnotatable = Microsoft.OData.Edm.Vocabularies.IEdmVocabularyAnnotatable;

namespace Brandless.AspNetCore.OData.Extensions
{
    public static class ODataExtensionsEdmModelExtensions
    {
        //public static void SetPermissionsCoreAnnotation(this EdmModel model, IEdmProperty property, CorePermission value)
        //{
        //    if (model == null) throw new ArgumentNullException("model");
        //    if (property == null) throw new ArgumentNullException("property");

        //    var target = property;
        //    var term = PermissionsTerm;
        //    var name = new EdmEnumTypeReference(PermissionType, false).ToStringLiteral((long)value);
        //    var expression = new EdmEnumMemberReferenceExpression(PermissionType.Members.Single(m => m.Name == name));
        //    var annotation = new EdmAnnotation(target, term, expression);
        //    annotation.SetSerializationLocation(model, property.ToSerializationLocation());
        //    model.AddVocabularyAnnotation(annotation);
        //}

        public static EdmModel AddEntityValidation<TEntity>(
            this EdmModel model,
            Expression<Func<TEntity, bool>> validationExpression,
            string message,
            string key = null)
        {
            model.ModelConfiguration()
                .ForEntityType<TEntity>()
                .AnnotationsManager
                .AddValidationRuleAnnotation(
                    validationExpression,
                    message,
                    key ?? Guid.NewGuid().ToString());
            return model;
        }

        public static EdmModel SetEntityDefaultDisplayTextFormatter<TEntity>(
            this EdmModel model,
            Expression<Func<TEntity, string>> formatterExpression)
        {
            model.ModelConfiguration()
                .ForEntityType<TEntity>()
                .AnnotationsManager
                .AddDisplayTextFormatterAnnotation(
                    formatterExpression);
            return model;
        }

        public static EdmModel SetEntityDisplayTextFormatter<TEntity>(
            this EdmModel model,
            Expression<Func<TEntity, string>> formatterExpression,
            string key)
        {
            model.ModelConfiguration()
                .ForEntityType<TEntity>()
                .AnnotationsManager
                .AddDisplayTextFormatterAnnotation(
                    formatterExpression,
                    key);
            return model;
        }

        public static EdmModel DefineReports<TEntity>(
            this EdmModel model,
            Action<ReportDefinitionMap<TEntity>> reportsMapper)
        {
            reportsMapper(
                model.ModelConfiguration()
                    .ForEntityType<TEntity>()
                    .ReportDefinitions);
            return model;
        }

        public static EdmModel AddEntityPropertyValidation<TEntity>(
            this EdmModel model,
            Expression<Func<TEntity, object>> propertyExpression,
            Expression<Func<TEntity, bool>> validationExpression,
            string message,
            string key = null)
        {
            model.ModelConfiguration()
                .ForEntityType<TEntity>()
                .AnnotationsManager
                .AddValidationRuleAnnotation(
                    validationExpression,
                    message,
                    key ?? Guid.NewGuid().ToString(),
                    propertyExpression
                );
            return model;
        }

        public static EdmModel AddEntityPropertyDisplayRule<TEntity>(
            this EdmModel model,
            Expression<Func<TEntity, object>> propertyExpression,
            Expression<Func<TEntity, bool>> displayRuleExpression,
            string message = null,
            string key = null,
            DisplayRuleKind kind = DisplayRuleKind.NewAndEdit)
        {
            model.ModelConfiguration()
                .ForEntityType<TEntity>()
                .AnnotationsManager
                .AddDisplayRuleAnnotation(
                    displayRuleExpression,
                    message,
                    key ?? Guid.NewGuid().ToString(),
                    propertyExpression
                );
            return model;
        }

        public static EdmModel AddRelationshipFilterRule<TEntity, TRelationship>(
            this EdmModel model,
            Expression<Func<TEntity, TRelationship>> propertyExpression,
            Expression<Func<RelationshipFilterContext<TEntity>, Expression<Func<TRelationship, bool>>>> filterExpression,
            string message = null,
            string key = null)
        {
            model.ModelConfiguration()
                .ForEntityType<TEntity>()
                .AnnotationsManager
                .AddRelationshipFilterAnnotation(
                    propertyExpression,
                    filterExpression,
                    message,
                    key ?? Guid.NewGuid().ToString()
                );
            return model;
        }
        public static EdmModel AddRelationshipFilterRule<TEntity, TRelationship>(
            this EdmModel model,
            Expression<Func<TEntity, IEnumerable<TRelationship>>> propertyExpression,
            Expression<Func<RelationshipFilterContext<TEntity>, Expression<Func<TRelationship, bool>>>> filterExpression,
            string message = null,
            string key = null)
        {
            model.ModelConfiguration()
                .ForEntityType<TEntity>()
                .AnnotationsManager
                .AddRelationshipFilterAnnotation(
                    propertyExpression,
                    filterExpression,
                    message,
                    key ?? Guid.NewGuid().ToString()
                );
            return model;
        }

        public static EntityMetadataConfigurator Entity<TEntity>(
            this EdmModel model,
            Action<IEntityMetadata> metadataExpression = null)
        {
            return model.Entity(typeof(TEntity), metadataExpression);
        }

        public static EntityMetadataConfigurator Entity(
            this EdmModel model,
            Type entityType,
            Action<IEntityMetadata> metadataExpression = null)
        {
            var propertyMetadata = model.ModelConfiguration()
                .ForEntityType(entityType)
                .Metadata;
            metadataExpression?.Invoke(propertyMetadata);
            return new EntityMetadataConfigurator(propertyMetadata);
        }

        public static void ForAllEntityTypes(this EdmModel model, Action<IEntityMetadata> action, params Type[] types)
        {
            foreach (var type in types)
            {
                model.Entity(type, action);
            }
        }

        public static void ForAllEntityTypes(this EdmModel model, Func<IEntityTypeConfiguration, bool> filter, Action<IEntityMetadata> action)
        {
            model.ForAllEntityTypes(action, model.ModelConfiguration().All().Where(filter).Select(t => t.EntityType).ToArray());
        }

        public static PropertyMetadataConfigurator Property<TEntity>(
            this EdmModel model,
            Expression<Func<TEntity, object>> propertyExpression,
            Action<IPropertyMetadata> metadataExpression = null)
        {
            var propertyMetadata = model.ModelConfiguration()
                .ForEntityType<TEntity>()
                .PropertyMetadata(propertyExpression);
            metadataExpression?.Invoke(propertyMetadata);
            return new PropertyMetadataConfigurator(propertyMetadata);
        }

        public static PropertyMetadataConfigurator Property(
            this EdmModel model,
            PropertyInfo property,
            Action<IPropertyMetadata> metadataExpression = null)
        {
            var entityType = property.DeclaringType;
            var propertyName = property.Name;
            return model.Property(entityType, propertyName, metadataExpression);
        }

        public static PropertyMetadataConfigurator Property(
            this EdmModel model,
            Type entityType,
            string propertyName,
            Action<IPropertyMetadata> metadataExpression
        )
        {
            var propertyMetadata = model.ModelConfiguration()
                .ForEntityType(entityType)
                .PropertyMetadata(propertyName);
            metadataExpression?.Invoke(propertyMetadata);
            return new PropertyMetadataConfigurator(propertyMetadata);
        }

        public static PropertyMetadataConfigurator Property<TEntity>(
            this EdmModel model,
            string propertyName,
            Action<IPropertyMetadata> metadataExpression
        )
        {
            return model.Property(typeof(TEntity), propertyName, metadataExpression);
        }

        public static PropertyMetadataConfigurator Property<TEntity>(
            this EdmModel model,
            PropertyInfo property,
            Action<IPropertyMetadata> metadataExpression
        )
        {
            return model.Property(typeof(TEntity), property.Name, metadataExpression);
        }

        public static EdmModel SetRegexValidation<TEntity>(
            this EdmModel model,
            Expression<Func<TEntity, object>> propertyExpression,
            string regex)
        {
            model.ModelConfiguration()
                .ForEntityType<TEntity>()
                .AnnotationsManager
                .SetRegexValidation(propertyExpression, regex);
            return model;
        }

        //public static EdmModel SetMaxLengthValidation<TEntity>(
        //    this EdmModel model,
        //    Expression<Func<TEntity, object>> propertyExpression,
        //    int maxLength)
        //{
        //    model.ModelConfiguration()
        //        .ForEntityType<TEntity>()
        //        .AnnotationsManager
        //        .SetMaxLengthValidation(propertyExpression, maxLength);
        //    return model;
        //}

        //public static EdmModel SetMinLengthValidation<TEntity>(
        //    this EdmModel model,
        //    Expression<Func<TEntity, object>> propertyExpression,
        //    int minLength)
        //{
        //    model.ModelConfiguration()
        //        .ForEntityType<TEntity>()
        //        .AnnotationsManager
        //        .SetMinLengthValidation(propertyExpression, minLength);
        //    return model;
        //}

        public static EdmModel SetRequiredValidation<TEntity>(
            this EdmModel model,
            Expression<Func<TEntity, object>> propertyExpression,
            bool required)
        {
            model.ModelConfiguration()
                .ForEntityType<TEntity>()
                .AnnotationsManager
                .SetRequiredAnnotation(propertyExpression, required);
            return model;
        }

        internal static EdmVocabularyAnnotationSerializationLocation ToSerializationLocation(this IEdmVocabularyAnnotatable target)
        {
            return target is IEdmEntityContainer ? EdmVocabularyAnnotationSerializationLocation.OutOfLine : EdmVocabularyAnnotationSerializationLocation.Inline;
        }
    }
}