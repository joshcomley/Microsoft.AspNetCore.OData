using System;
using System.Linq.Expressions;
using Brandless.AspNetCore.OData.Extensions.Extensions;
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

        public static void SetEntityValidation<TEntity>(
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
        }

        public static void SetEntityDefaultDisplayTextFormatter<TEntity>(
            this EdmModel model,
            Expression<Func<TEntity, string>> formatterExpression)
        {
            model.ModelConfiguration()
                .ForEntityType<TEntity>()
                .AnnotationsManager
                .AddDisplayTextFormatterAnnotation(
                    formatterExpression);
        }

        public static void SetEntityDisplayTextFormatter<TEntity>(
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
        }

        public static void SetEntityPropertyValidation<TEntity>(
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
        }

        public static void SetRegexValidation<TEntity>(
            this EdmModel model,
            Expression<Func<TEntity, object>> propertyExpression,
            string regex)
        {
            model.ModelConfiguration()
                .ForEntityType<TEntity>()
                .AnnotationsManager
                .SetRegexValidation(propertyExpression, regex);
        }

        public static void SetMaxLengthValidation<TEntity>(
            this EdmModel model,
            Expression<Func<TEntity, object>> propertyExpression,
            int maxLength)
        {
            model.ModelConfiguration()
                .ForEntityType<TEntity>()
                .AnnotationsManager
                .SetMaxLengthValidation(propertyExpression, maxLength);
        }

        public static void SetMinLengthValidation<TEntity>(
            this EdmModel model,
            Expression<Func<TEntity, object>> propertyExpression,
            int minLength)
        {
            model.ModelConfiguration()
                .ForEntityType<TEntity>()
                .AnnotationsManager
                .SetMinLengthValidation(propertyExpression, minLength);
        }

        public static void SetRequiredValidation<TEntity>(
            this EdmModel model,
            Expression<Func<TEntity, object>> propertyExpression,
            bool required)
        {
            model.ModelConfiguration()
                .ForEntityType<TEntity>()
                .AnnotationsManager
                .SetRequiredAnnotation(propertyExpression, required);
        }

        internal static EdmVocabularyAnnotationSerializationLocation ToSerializationLocation(this IEdmVocabularyAnnotatable target)
        {
            return target is IEdmEntityContainer ? EdmVocabularyAnnotationSerializationLocation.OutOfLine : EdmVocabularyAnnotationSerializationLocation.Inline;
        }
    }
}