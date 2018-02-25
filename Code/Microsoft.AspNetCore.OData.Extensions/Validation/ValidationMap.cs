using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.OData.Edm;

namespace Brandless.AspNetCore.OData.Extensions.Validation
{
    public class ValidationMap
    {
        public IEdmModel Model { get; }
        private static readonly Dictionary<IEdmModel, ValidationMap> _validationMap = new Dictionary<IEdmModel, ValidationMap>();

        private Dictionary<Type, IEntityValidationMap> _entityValidationMaps = new Dictionary<Type, IEntityValidationMap>();

        public static EntityValidationMap<T> ForType<T>()
        {
            foreach (var map in _validationMap.Values)
            {
                if (map._entityValidationMaps.ContainsKey(typeof(T)))
                {
                    return (EntityValidationMap<T>) map._entityValidationMaps[typeof(T)];
                }
            }
            return null;
        }

        public static ValidationMap ForModel(IEdmModel model)
        {
            if (!_validationMap.ContainsKey(model))
            {
                _validationMap.Add(model, new ValidationMap(model));
            }
            return _validationMap[model];
        }
        private ValidationMap(IEdmModel model)
        {
            Model = model;
        }

        public EntityValidationMap<TEntity> EntityValidation<TEntity>()
        {
            if (!_entityValidationMaps.ContainsKey(typeof(TEntity)))
            {
                _entityValidationMaps.Add(typeof(TEntity), new EntityValidationMap<TEntity>());
            }
            return (EntityValidationMap<TEntity>)_entityValidationMaps[typeof(TEntity)];
        }
    }

    public class EntityValidation
    {
    }

    //        public Expression ValidationExpression { get; }
    public class EntityValidation<TEntity> : IEntityValidation
    {
        private Func<TEntity, bool> _validationFunction;
        public string Message { get; }
        public Expression<Func<TEntity, bool>> ValidationExpression { get; }

        public Func<TEntity, bool> ValidationFunction => _validationFunction ?? (_validationFunction = ValidationExpression.Compile());

        Expression IEntityValidation.ValidationExpression => ValidationExpression;

        public EntityValidation(Expression<Func<TEntity, bool>> validationExpression, string message)
        {
            ValidationExpression = validationExpression;
            Message = message;
        }
    }

    public interface IEntityValidation
    {
        string Message { get; }
        Expression ValidationExpression { get; }
    }

    public class EntityValidationMap<TEntity> : IEntityValidationMap
    {
        private readonly Dictionary<string, EntityPropertyValidationMap<TEntity>> _properties = new Dictionary<string, EntityPropertyValidationMap<TEntity>>();
        private readonly List<EntityValidation<TEntity>> _validationExpressions = new List<EntityValidation<TEntity>>();

        public IEnumerable<EntityPropertyValidationMap<TEntity>> PropertyValidations => _properties.Values;
        public IReadOnlyList<EntityValidation<TEntity>> EntityValidations => _validationExpressions.AsReadOnly();

        public void AddValidation(EntityValidation<TEntity> validationExpression, string propertyName = null)
        {
            if (propertyName == null)
            {
                _validationExpressions.Add(validationExpression);
                return;
            }
            if (typeof(TEntity).GetRuntimeProperties().All(p => p.Name != propertyName))
            {
                throw new ArgumentException($"No property \"{propertyName}\" found on type \"{typeof(TEntity).Name}\"");
            }
            if (!_properties.ContainsKey(propertyName))
            {
                _properties.Add(propertyName, new EntityPropertyValidationMap<TEntity>(propertyName));
            }
            _properties[propertyName].AddValidation(validationExpression);
        }

        void IEntityValidationMap.AddValidation(IEntityValidation validationExpression, string propertyName = null)
        {
            AddValidation((EntityValidation<TEntity>)validationExpression, propertyName);
        }
    }

    public interface IEntityValidationMap
    {
        void AddValidation(IEntityValidation validationExpression, string propertyName = null);
    }

    public class EntityPropertyValidationMap<TEntity>
    {
        public IReadOnlyList<EntityValidation<TEntity>> Validations => _validationExpressions.AsReadOnly();
        public string PropertyName { get; }
        public Type EntityType => typeof(TEntity);

        private readonly List<EntityValidation<TEntity>> _validationExpressions = new List<EntityValidation<TEntity>>();

        public EntityPropertyValidationMap(string propertyName)
        {
            PropertyName = propertyName;
        }
        public void AddValidation(EntityValidation<TEntity> validationExpression)
        {
            _validationExpressions.Add(validationExpression);
        }
    }
}