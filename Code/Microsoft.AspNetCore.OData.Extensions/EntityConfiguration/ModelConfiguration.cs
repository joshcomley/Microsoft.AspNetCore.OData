using System;
using System.Collections.Generic;
using Microsoft.OData.Edm;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration
{
    public class ModelConfiguration
    {
        public EdmModel Model { get; }
        private static readonly Dictionary<EdmModel, ModelConfiguration> _modelConfigurationMap = new Dictionary<EdmModel, ModelConfiguration>();

        private readonly Dictionary<Type, IEntityTypeConfiguration> _entityTypeConfigurationMap = new Dictionary<Type, IEntityTypeConfiguration>();

        public static EntityTypeConfiguration<T> ForType<T>()
        {
            foreach (var map in _modelConfigurationMap.Values)
            {
                if (map._entityTypeConfigurationMap.ContainsKey(typeof(T)))
                {
                    return (EntityTypeConfiguration<T>) map._entityTypeConfigurationMap[typeof(T)];
                }
            }
            return null;
        }

        public static ModelConfiguration ForModel(EdmModel model)
        {
            if (!_modelConfigurationMap.ContainsKey(model))
            {
                _modelConfigurationMap.Add(model, new ModelConfiguration(model));
            }
            return _modelConfigurationMap[model];
        }
        private ModelConfiguration(EdmModel model)
        {
            Model = model;
        }

        public EntityTypeConfiguration<TEntity> ForEntityType<TEntity>()
        {
            if (!_entityTypeConfigurationMap.ContainsKey(typeof(TEntity)))
            {
                _entityTypeConfigurationMap.Add(typeof(TEntity), new EntityTypeConfiguration<TEntity>(Model));
            }
            return (EntityTypeConfiguration<TEntity>)_entityTypeConfigurationMap[typeof(TEntity)];
        }
    }
}