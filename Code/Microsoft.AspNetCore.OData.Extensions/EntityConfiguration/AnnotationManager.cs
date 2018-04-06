using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Display;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Metadata;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Validation;
using Brandless.AspNetCore.OData.Extensions.Extensions;
using Iql.DotNet.Serialization;
using Iql.Queryable.Data.EntityConfiguration;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Vocabularies;
using EdmModelExtensions = Microsoft.AspNetCore.OData.Extensions.EdmModelExtensions;

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

        public void AddDisplayTextFormatterAnnotation(
            Expression<Func<TEntity, string>> formatterExpression,
            string key = null)
        {
            var iql = IqlSerializer.SerializeToXml(formatterExpression);
            // TODO: If validation expression is null, then compile it from the IQL
            //var iql = ExpressionToIqlExpressionParser<TEntity>.Parse(validationExpression);
            //var parser =
            //    new ActionParserInstance<ODataIqlData, ODataIqlExpressionAdapter>(new ODataIqlExpressionAdapter());
            var isDefault = key == null;
            if (key == null)
            {
                key = EntityDisplayTextFormatterMap<object>.DefaultKey;
            }

            var modelConfiguration = Model.ModelConfiguration();
            var map = modelConfiguration.ForEntityType<TEntity>().DisplayTextFormatterMap;
            if (isDefault)
            {
                map.Set(formatterExpression, key);
            }
            else
            {
                map.SetDefault(formatterExpression);
            }

            var config = GetConfigurationAnnotation();

            var expressionLabel = new EdmLabeledExpression(key, new EdmStringConstant(iql));
            config.DisplayFormattingAnnotation.Add(expressionLabel);
        }

        //private void Initialize()
        //{
        //    if (!_initialized)
        //    {
        //        _initialized = true;
        //        // Initialize!
        //        var type = Model.GetEdmType(typeof(TEntity)) as EdmEntityType;
        //        IEdmVocabularyAnnotatable target = type;
        //        var coll = new EdmCollectionExpression(EntityConfigurationExpressions);
        //        var annotation = new EdmVocabularyAnnotation(target, IqlConfigurationTerm, coll);
        //        annotation.SetSerializationLocation(Model, target.ToSerializationLocation());
        //        Model.AddVocabularyAnnotation(annotation);
        //    }
        //}

        public void AddValidationAnnotation(
            Expression<Func<TEntity, bool>> validationExpression,
            string message,
            string key,
            Expression<Func<TEntity, object>> propertyExpression = null)
        {
            var iql = IqlSerializer.SerializeToXml(validationExpression);
            string propertyName = null;

            if (propertyExpression != null)
            {
                propertyName = propertyExpression.GetAccessedProperty().Name;
                //InitializeProperty(propertyName);
            }

            var expression = new EdmStringConstant(iql);
            var expressionLabel = new EdmLabeledExpression("Expression", expression);
            var messageLabel = new EdmLabeledExpression("Message", new EdmStringConstant(message));
            var keyLabel = new EdmLabeledExpression("Key", new EdmStringConstant(key));
            var coll = new EdmCollectionExpression(keyLabel, expressionLabel, messageLabel);

            var container = new EdmLabeledExpression("Validation", coll);
            var config = GetConfigurationAnnotation(propertyName);
            config.ValidationAnnotation.Add(container);
            //EntityConfigurationExpressions.Add(container);
            //var annotation = new EdmVocabularyAnnotation(target, ValidationTerm, coll);
            //annotation.SetSerializationLocation(Model, target.ToSerializationLocation());
            //Model.AddVocabularyAnnotation(annotation);

            var modelConfiguration = Model.ModelConfiguration();
            var validationObject = new EntityValidation<TEntity>(validationExpression, key, message);
            modelConfiguration.ForEntityType<TEntity>().ValidationMap.AddValidation(
                validationObject, propertyName);
        }

        private IEdmExpression GetExpression(object obj, Type objectType)
        {
            if (obj == null)
            {
                return null;
            }

            IEdmExpression expression = null;
            objectType = objectType == null ? obj.GetType() : objectType;
            var nullableUnderlyingType = Nullable.GetUnderlyingType(objectType);
            if (objectType == typeof(string))
            {
                expression = new EdmStringConstant(obj as string);
            }
            else if ((objectType == typeof(bool) && !Equals(false, obj)) || nullableUnderlyingType == typeof(bool))
            {
                expression = new EdmBooleanConstant((bool)obj);
            }
            else if (objectType.IsEnum)
            {
                //var t = EdmLibHelpers.GetEdmType(Model, objectType);
                if (EnumExtensions.IsValidEnumValue(obj))
                {
                    expression = new EdmStringConstant(obj.ToString());
                }
                //var edmEnumType = EnsureEdmType(objectType);
                //expression = new EdmEnumMemberExpression(
                //    new EdmEnumMember(
                //        edmEnumType,
                //        obj.ToString(),
                //        new EdmEnumMemberValue((int)obj)
                //        ));
            }
            else if (objectType.IsCollection())
            {
                var enumerable = obj as IEnumerable;
                if (enumerable != null)
                {
                    var arr = new List<IEdmExpression>();
                    foreach (var value in enumerable)
                    {
                        var edmExpression = GetExpression(value, null);
                        if (edmExpression != null)
                        {
                            arr.Add(edmExpression);
                        }
                    }

                    if (arr.Any())
                    {
                        var collectionExpression = new EdmCollectionExpression(arr);
                        expression = collectionExpression;
                    }
                }
            }

            return expression;
        }

        private IEdmEnumType EnsureEdmType(Type objectType)
        {
            var edmEnumType = (IEdmEnumType)EdmModelExtensions.GetEdmType(Model, objectType);
            if (edmEnumType == null)
            {
                var propertyType = new EdmEnumType(objectType.Namespace, objectType.Name, false);
                Model.AddElement(propertyType);
                var names = Enum.GetNames(objectType);
                var values = Enum.GetValues(objectType).Cast<int>().ToArray();
                for (var i = 0; i < names.Length; i++)
                {
                    var name = names[i];
                    var value = values[i];
                    propertyType.AddMember(name, new EdmEnumMemberValue(value));
                }

                edmEnumType = (IEdmEnumType)EdmModelExtensions.GetEdmType(Model, objectType);
                //t = Microsoft.AspNetCore.OData.Formatter.EdmLibHelpers.GetEdmType(Model, objectType);
            }

            return edmEnumType;
        }

        public override void SetMetadataAnnotation(
            IMetadata metadata = null, string property = null)
        {
            SetMetadataAnnotationInternal(
                metadata,
                property);
        }

        public void SetMetadataAnnotation<T>(
            T metadata = null,
            Expression<Func<TEntity, object>> propertyExpression = null)
        where T : class, IMetadata
        {
            string propertyName = null;

            if (propertyExpression != null)
            {
                propertyName = propertyExpression.GetAccessedProperty().Name;
            }

            SetMetadataAnnotationInternal(metadata, propertyName);
        }

        private void SetMetadataAnnotationInternal<T>(T metadata, string propertyName) where T : class, IMetadata
        {
            //var container2 = new EdmLabeledExpression("Metadata", new EdmStringConstant("Heyyy"));

            //var config2 = GetConfigurationAnnotation(propertyName);
            //config2.MetadataAnnotation = container2;
            //return;

            if (metadata != null)
            {
                var expressions = new List<IEdmExpression>();
                var type = metadata.GetType();
                var runtimeProperties = type.GetTypeInfo().GetRuntimeProperties();
                foreach (var property in runtimeProperties)
                {
                    var edmExpression = GetExpression(property.GetValue(metadata), property.PropertyType);
                    if (edmExpression != null)
                    {
                        var label = new EdmLabeledExpression(property.Name, edmExpression);
                        expressions.Add(label);
                    }
                }

                if (expressions.Any())
                {
                    var coll = new EdmCollectionExpression(expressions);
                    var container = new EdmLabeledExpression("Metadata", coll);

                    //EntityConfigurationExpressions.Add(container);
                    //var annotation = new EdmVocabularyAnnotation(target, ValidationTerm, coll);
                    //annotation.SetSerializationLocation(Model, target.ToSerializationLocation());
                    //Model.AddVocabularyAnnotation(annotation);

                    //var modelConfiguration = Model.ModelConfiguration();

                    //modelConfiguration.ForEntityType<TEntity>().Metadata = metadata;

                    var config = GetConfigurationAnnotation(propertyName);
                    config.MetadataAnnotation = container;
                }
            }
        }

        public ConfigurationAnnotation<TEntity> GetConfigurationAnnotation(string propertyName = null)
        {
            if (propertyName == null)
            {
                return EntityConfigurationAnnotation;
            }

            if (PropertyConfigurationAnnotations.ContainsKey(propertyName))
            {
                return PropertyConfigurationAnnotations[propertyName];
            }

            var configurationAnnotation = new ConfigurationAnnotation<TEntity>(Model, propertyName);
            if (configurationAnnotation.Valid)
            {
                PropertyConfigurationAnnotations.Add(propertyName, configurationAnnotation);
            }

            return configurationAnnotation;
        }

        //private List<IEdmExpression> InitializeProperty(string propertyName)
        //{
        //    if (PropertyRootConfigurationExpressions.ContainsKey(propertyName))
        //    {
        //        return PropertyRootConfigurationExpressions[propertyName];
        //    }
        //    var list = new List<IEdmExpression>();
        //    PropertyRootConfigurationExpressions.Add(propertyName, list);
        //    var type = Model.GetEdmType(typeof(TEntity)) as EdmEntityType;
        //    var property = type.Properties().Single(p => p.Name == propertyName);
        //    IEdmVocabularyAnnotatable target = property;
        //    var coll = new EdmCollectionExpression(list);
        //    var annotation = new EdmVocabularyAnnotation(target, IqlConfigurationTerm, coll);
        //    Model.AddVocabularyAnnotation(annotation);
        //    return list;
        //}

        public void SetRequiredAnnotation(Expression<Func<TEntity, object>> propertyExpression, bool required)
        {
            SetSimpleAnnotation(
                propertyExpression,
                new EdmBooleanConstant(required),
                ValidationRequiredTerm);
        }

        public void SetRegexValidation(
            Expression<Func<TEntity, object>> propertyExpression,
            string regex)
        {
            SetSimpleAnnotation(
                propertyExpression,
                new EdmStringConstant(regex),
                ValidationRegexTerm
            );
        }

        public void SetMinLengthValidation(
            Expression<Func<TEntity, object>> propertyExpression,
            int minLength)
        {
            SetSimpleAnnotation(
                propertyExpression,
                new EdmIntegerConstant(minLength),
                ValidationMinLengthTerm
            );
        }

        public void SetMaxLengthValidation(
            Expression<Func<TEntity, object>> propertyExpression,
            int maxLength)
        {
            SetSimpleAnnotation(
                propertyExpression,
                new EdmIntegerConstant(maxLength),
                ValidationMaxLengthTerm
            );
        }

        private void SetSimpleAnnotation(Expression<Func<TEntity, object>> propertyExpression,
            IEdmExpression expression, IEdmTerm term)
        {
            var type = EdmModelExtensions.GetEdmType(Model, typeof(TEntity)) as EdmEntityType;
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
            annotation.SetSerializationLocation(Model, target.ToSerializationLocation());
            Model.AddVocabularyAnnotation(annotation);
        }
    }
}