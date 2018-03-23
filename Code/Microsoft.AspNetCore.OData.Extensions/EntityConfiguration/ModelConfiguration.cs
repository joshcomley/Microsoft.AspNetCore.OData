using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.OData.Edm;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration
{
    public class ModelConfiguration
    {
        public EdmModel Model { get; }
        private static readonly Dictionary<EdmModel, ModelConfiguration> ModelConfigurationMap = new Dictionary<EdmModel, ModelConfiguration>();

        private readonly Dictionary<Type, IEntityTypeConfiguration> _entityTypeConfigurationMap = new Dictionary<Type, IEntityTypeConfiguration>();

        public static EntityTypeConfiguration<T> ForType<T>()
        {
            foreach (var map in ModelConfigurationMap.Values)
            {
                if (map._entityTypeConfigurationMap.ContainsKey(typeof(T)))
                {
                    return (EntityTypeConfiguration<T>) map._entityTypeConfigurationMap[typeof(T)];
                }
            }
            return null;
        }

        public IEnumerable<IEntityTypeConfiguration> All()
        {
            return _entityTypeConfigurationMap.Select(m => m.Value);
        }

        public static ModelConfiguration ForModel(EdmModel model)
        {
            if (!ModelConfigurationMap.ContainsKey(model))
            {
                ModelConfigurationMap.Add(model, new ModelConfiguration(model));
            }
            return ModelConfigurationMap[model];
        }
        private ModelConfiguration(EdmModel model)
        {
            Model = model;
        }

        static ModelConfiguration()
        {
            ForEntityTypeGenericMethod = typeof(ModelConfiguration)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Single(m => m.Name == nameof(ForEntityType) && m.GetGenericArguments().Length > 0);
        }

        public static MethodInfo ForEntityTypeGenericMethod { get; set; }

        public EntityTypeConfiguration<TEntity> ForEntityType<TEntity>()
        {
            var entityType = typeof(TEntity);
            if (!_entityTypeConfigurationMap.ContainsKey(entityType))
            {
                _entityTypeConfigurationMap.Add(entityType, new EntityTypeConfiguration<TEntity>(Model));
            }
            return (EntityTypeConfiguration<TEntity>)_entityTypeConfigurationMap[entityType];
        }

        public IEntityTypeConfiguration ForEntityType(Type entityType)
        {
            return (IEntityTypeConfiguration) ForEntityTypeGenericMethod.MakeGenericMethod(entityType).Invoke(this, new object[]{});
        }
    }
}