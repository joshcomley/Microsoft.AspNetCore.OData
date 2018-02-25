using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using Brandless.AspNetCore.OData.Extensions.Extensions;
using Brandless.AspNetCore.OData.Extensions.Validation;
using Iql.DotNet;
using Iql.DotNet.Serialization;
using Iql.Queryable;
using Brandless.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.Data.Edm;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Validation;
using Microsoft.OData.Edm.Vocabularies;
using EdmCollectionExpression = Microsoft.OData.Edm.Vocabularies.EdmCollectionExpression;
using EdmLabeledExpression = Microsoft.OData.Edm.Vocabularies.EdmLabeledExpression;
using EdmPrimitiveTypeKind = Microsoft.OData.Edm.EdmPrimitiveTypeKind;
using IEdmEntityContainer = Microsoft.OData.Edm.IEdmEntityContainer;
using IEdmModel = Microsoft.OData.Edm.IEdmModel;
using IEdmTerm = Microsoft.OData.Edm.Vocabularies.IEdmTerm;
using IEdmVocabularyAnnotatable = Microsoft.OData.Edm.Vocabularies.IEdmVocabularyAnnotatable;

namespace Brandless.AspNetCore.OData.Extensions
{
    public static class ODataExtensionsEdmModelExtensions
    {
        public static readonly IEdmModel Instance;
        public static readonly IEdmTerm ValidationTerm;
        public static readonly IEdmTerm ValidationRegexTerm;
        public static readonly IEdmValueTerm PermissionsTerm;
        public static readonly IEdmTerm ValidationMaxLengthTerm;
        public static readonly IEdmTerm ValidationMinLengthTerm;
        public static readonly IEdmTerm ValidationRequiredTerm;

        static ODataExtensionsEdmModelExtensions()
        {
            IEnumerable<EdmError> errors;
            var assembly = typeof(ODataExtensionsEdmModelExtensions).GetTypeInfo().Assembly;
            using (var stream = assembly.GetManifestResourceStream(
                $"{assembly.GetName().Name}.Vocabularies.MeasuresVocabularies.xml"))
            {
                CsdlReader.TryParse(XmlReader.Create(stream), out Instance, out errors);
            }
            //ISOCurrencyTerm = Instance.FindDeclaredTerm(MeasuresISOCurrency);
            var validationNs = "Validation";
            ValidationTerm = StringEdmTerm("Expression", validationNs);
            ValidationRegexTerm = StringEdmTerm("RegularExpression", validationNs);
            ValidationMaxLengthTerm = NumberEdmTerm("MaximumLength", validationNs);
            ValidationMinLengthTerm = NumberEdmTerm("MinimumLength", validationNs);
            ValidationRequiredTerm = BooleanEdmTerm("Required", validationNs);
            //ISOCurrencyTerm = Instance.FindDeclaredValueTerm(MeasuresISOCurrency);

        }

        private static class AppliesTo
        {
            public const string Property = "Property";
        }
        private static IEdmTerm StringEdmTerm(string name, string @namespace = null)
        {
            return EdmTerm(name, EdmPrimitiveTypeKind.String, @namespace);
        }

        private static IEdmTerm NumberEdmTerm(string name, string @namespace = null)
        {
            return EdmTerm(name, EdmPrimitiveTypeKind.Int32, @namespace);
        }

        private static IEdmTerm BooleanEdmTerm(string name, string @namespace = null)
        {
            return EdmTerm(name, EdmPrimitiveTypeKind.Boolean, @namespace);
        }

        private static IEdmTerm EdmTerm(string name, EdmPrimitiveTypeKind type, string @namespace = null)
        {
            return new EdmTerm(@namespace/* ?? typeof(ApiDbContext).Namespace*/, name, type, AppliesTo.Property);
        }

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
            string message)
        {
            var iql = IqlSerializer.SerializeToXml(validationExpression);
            model.SetValidationAnnotation(
                ValidationTerm,
                new EdmStringConstant(iql),
                message,
                validationExpression);
        }

        public static void SetEntityPropertyValidation<TEntity>(
            this EdmModel model,
            Expression<Func<TEntity, object>> propertyExpression,
            Expression<Func<TEntity, bool>> validationExpression,
            string message)
        {
            var iql = IqlSerializer.SerializeToXml(validationExpression);
            model.SetValidationAnnotation(
                ValidationTerm,
                new EdmStringConstant(iql),
                message,
                validationExpression,
                propertyExpression);
        }

        public static void SetRegexValidation<TEntity>(
            this EdmModel model,
            Expression<Func<TEntity, object>> propertyExpression,
            string regex)
        {
            model.SetAnnotation(
                ValidationRegexTerm,
                new EdmStringConstant(regex),
                propertyExpression);
        }

        public static void SetMaxLengthValidation<TEntity>(
            this EdmModel model,
            Expression<Func<TEntity, object>> propertyExpression,
            int maxLength)
        {
            model.SetAnnotation(
                ValidationMaxLengthTerm,
                new EdmIntegerConstant(maxLength),
                propertyExpression);
        }

        public static void SetMinLengthValidation<TEntity>(
            this EdmModel model,
            Expression<Func<TEntity, object>> propertyExpression,
            int minLength)
        {
            model.SetAnnotation(
                ValidationMinLengthTerm,
                new EdmIntegerConstant(minLength),
                propertyExpression);
        }

        public static void SetRequiredValidation<TEntity>(
            this EdmModel model,
            Expression<Func<TEntity, object>> propertyExpression,
            bool required)
        {
            model.SetAnnotation(
                ValidationRequiredTerm,
                new EdmBooleanConstant(required),
                propertyExpression
                );
        }

        public static void SetAnnotation<TEntity>(
            this EdmModel model,
            IEdmTerm term,
            IEdmExpression expression,
            Expression<Func<TEntity, object>> propertyExpression = null)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            var type = model.GetEdmType(typeof(TEntity)) as EdmEntityType;
            IEdmVocabularyAnnotatable target = type;
            if (propertyExpression != null)
            {
                var property = type.Properties().Single(p => p.Name == propertyExpression.GetAccessedProperty().Name);
                target = property;
            }
            var label = new EdmLabeledExpression("Value", expression);
            //var coll1 = new EdmCollectionExpression(new EdmStringConstant("test1"), new EdmStringConstant("test2"), new EdmStringConstant("test3"), label);
            //var coll = new EdmCollectionExpression(expression, coll1);
            var annotation = new EdmVocabularyAnnotation(target, term, label);
            annotation.SetSerializationLocation(model, target.ToSerializationLocation());
            model.AddVocabularyAnnotation(annotation);
        }

        public static void SetValidationAnnotation<TEntity>(
            this EdmModel model,
            IEdmTerm term,
            IEdmExpression expression,
            string message,
            Expression<Func<TEntity, bool>> validationExpression = null,
            Expression<Func<TEntity, object>> propertyExpression = null)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            // TODO: If validation expression is null, then compile it from the IQL

            var type = model.GetEdmType(typeof(TEntity)) as EdmEntityType;
            IEdmVocabularyAnnotatable target = type;
            string propertyName = null;
            if (propertyExpression != null)
            {
                propertyName = propertyExpression.GetAccessedProperty().Name;
                var property = type.Properties().Single(p => p.Name == propertyName);
                target = property;
            }
            IqlQueryableAdapter.ExpressionConverter = () => new ExpressionToIqlConverter();
            //var iql = ExpressionToIqlExpressionParser<TEntity>.Parse(validationExpression);
            //var parser =
            //    new ActionParserInstance<ODataIqlData, ODataIqlExpressionAdapter>(new ODataIqlExpressionAdapter());

            var expressionLabel = new EdmLabeledExpression("Expression", expression);
            var messageLabel = new EdmLabeledExpression("Message", new EdmStringConstant(message));
            var coll = new EdmCollectionExpression(expressionLabel, messageLabel);
            var annotation = new EdmVocabularyAnnotation(target, term, coll);
            var validation = ValidationMap.ForModel(model);
            var validationObject = new EntityValidation<TEntity>(validationExpression, message);
            validation.EntityValidation<TEntity>()
                .AddValidation(validationObject, propertyName);
            annotation.SetSerializationLocation(model, target.ToSerializationLocation());
            model.AddVocabularyAnnotation(annotation);
        }

        private static EdmVocabularyAnnotationSerializationLocation ToSerializationLocation(this IEdmVocabularyAnnotatable target)
        {
            return target is IEdmEntityContainer ? EdmVocabularyAnnotationSerializationLocation.OutOfLine : EdmVocabularyAnnotationSerializationLocation.Inline;
        }
    }
}