using System;
using System.Linq.Expressions;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Metadata;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Reports;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Validation;
using Brandless.AspNetCore.OData.Extensions.Extensions;
using Iql.Queryable.Data.EntityConfiguration;
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
                .AddValidationAnnotation(
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
                .AddValidationAnnotation(
                    validationExpression,
                    message,
                    key ?? Guid.NewGuid().ToString(),
                    propertyExpression
                );
            return model;
        }

        public static EntityMetadataConfigurator Entity<TEntity>(
            this EdmModel model,
            Action<IEntityMetadata> metadataExpression = null)
        {
            var propertyMetadata = model.ModelConfiguration()
                .ForEntityType<TEntity>()
                .Metadata;
            metadataExpression?.Invoke(propertyMetadata);
            return new EntityMetadataConfigurator(propertyMetadata);
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

        public static EdmModel SetMaxLengthValidation<TEntity>(
            this EdmModel model,
            Expression<Func<TEntity, object>> propertyExpression,
            int maxLength)
        {
            model.ModelConfiguration()
                .ForEntityType<TEntity>()
                .AnnotationsManager
                .SetMaxLengthValidation(propertyExpression, maxLength);
            return model;
        }

        public static EdmModel SetMinLengthValidation<TEntity>(
            this EdmModel model,
            Expression<Func<TEntity, object>> propertyExpression,
            int minLength)
        {
            model.ModelConfiguration()
                .ForEntityType<TEntity>()
                .AnnotationsManager
                .SetMinLengthValidation(propertyExpression, minLength);
            return model;
        }

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