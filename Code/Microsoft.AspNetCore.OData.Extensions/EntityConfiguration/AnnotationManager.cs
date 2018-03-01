using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Display;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Validation;
using Brandless.AspNetCore.OData.Extensions.Extensions;
using Iql.DotNet.Serialization;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Vocabularies;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration
{
    internal class CollectionAnnotation<TEntity>
    {
        private bool _initialized;
        public string Key { get; }
        public ConfigurationAnnotation<TEntity> Root { get; }
        private List<IEdmExpression> ChildExpressions { get; } = new List<IEdmExpression>();
        public EdmModel Model { get; }

        public CollectionAnnotation(string key, ConfigurationAnnotation<TEntity> root, EdmModel model)
        {
            Key = key;
            Root = root;
            Model = model;
        }

        public void Add(IEdmExpression expression)
        {
            if (!_initialized)
            {
                _initialized = true;
                var entityValidationRoot = new EdmLabeledExpression(Key,
                    new EdmCollectionExpression(ChildExpressions));
                Root.ChildExpressions.Add(entityValidationRoot);
            }
            ChildExpressions.Add(expression);
        }
    }
    internal class ConfigurationAnnotation<TEntity>
    {
        public EdmModel Model { get; }
        public List<IEdmExpression> ChildExpressions { get; } = new List<IEdmExpression>();
        public CollectionAnnotation<TEntity> ValidationAnnotation { get; }
        public ConfigurationAnnotation(EdmModel model, string propertyName = null)
        {
            Model = model;
            var type = Model.GetEdmType(typeof(TEntity)) as EdmEntityType;
            IEdmVocabularyAnnotatable target = type;
            if (propertyName != null)
            {
                target = type.Properties().Single(p => p.Name == propertyName);
            }
            var coll = new EdmCollectionExpression(ChildExpressions);
            var annotation = new EdmVocabularyAnnotation(target, AnnotationManagerBase.IqlConfigurationTerm, coll);
            annotation.SetSerializationLocation(Model, target.ToSerializationLocation());
            Model.AddVocabularyAnnotation(annotation);

            ValidationAnnotation = new CollectionAnnotation<TEntity>("Validations", this, model);
            DisplayFormattingAnnotation = new CollectionAnnotation<TEntity>("DisplayFormatters", this, model);
        }

        public CollectionAnnotation<TEntity> DisplayFormattingAnnotation { get; set; }
    }
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
            PropertyConfigurationAnnotations.Add(propertyName, configurationAnnotation);
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
            var type = Model.GetEdmType(typeof(TEntity)) as EdmEntityType;
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