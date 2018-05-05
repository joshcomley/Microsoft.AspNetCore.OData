using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Brandless.Data.Contracts;
using Brandless.Data.Entities;
using Iql.DotNet.Extensions;
using Microsoft.AspNetCore.OData.Builder;
using Microsoft.OData.Edm;

namespace Brandless.AspNetCore.OData.Extensions.Configuration
{
    public abstract class AllConfigurator<TService> : IODataEntitySetConfigurator
    {
        public virtual void Configure(ODataModelBuilder builder, Action<Action<EdmModel>> model)
        {
            var entityTypes = typeof(TService)
                .GetProperties()
                .Select(p => p.PropertyType.GetGenericArguments()[0]);
            var allMethods = GetType().GetRuntimeMethods().ToList();
            model(edmModel =>
            {
                foreach (var entityType in entityTypes)
                {
                    var methodInfos = new List<MethodInfo>();
                    foreach (var method in allMethods)
                    {
                        var typeArguments = method.GetGenericArguments();
                        if (typeArguments.Length > 0)
                        {
                            var typeArgument = typeArguments[0];
                            var constraints = typeArgument.GetGenericParameterConstraints();
                            if (constraints.Length == 1)
                            {
                                var constraint = constraints[0];
                                var wasGenericType = constraint.IsGenericType;
                                if (constraint.IsGenericType)
                                {
                                    constraint = constraint.GetGenericTypeDefinition();
                                }
                                entityType.TryGetBaseType(constraint, type =>
                                {
                                    var typeArguments2 = new List<Type>();
                                    typeArguments2.Add(entityType);
                                    if (wasGenericType)
                                    {
                                        typeArguments2.Add(type.Type.GetGenericArguments()[0]);
                                    }
                                    methodInfos.Add(method.MakeGenericMethod(typeArguments2.ToArray()));
                                });
                            }
                        }
                    }

                    foreach (var methodInfo in methodInfos)
                    {
                        methodInfo.Invoke(this, new object[] { edmModel });
                    }
                }
            });
        }

        public void ConfigureIRevisionable<T>(EdmModel model)
            where T : IRevisionable
        {
            model.Property<T>(p => p.RevisionKey).SetReadOnly();
        }

        public void ConfigureICreatedDate<T>(EdmModel model)
            where T : ICreatedDate
        {
            model.Property<T>(p => p.CreatedDate).SetReadOnly();
        }

        public void ConfigureDbObject<T, TUser>(EdmModel model)
            where T : DbObjectBase<TUser>
        {
            model.Property<T>(p => p.Id).SetReadOnly();
            model.Property<T>(p => p.CreatedDate).SetReadOnly();
            model.Property<T>(p => p.CreatedByUser).SetReadOnly();
            model.Property<T>(p => p.CreatedByUserId).SetReadOnly();
            model.Property<T>(p => p.Guid).SetReadOnly();
            model.Property<T>(p => p.PersistenceKey).SetReadOnly();
            model.Property<T>(p => p.Version).SetReadOnly();
        }

        public void ConfigureICreatedBy<T, TUser>(EdmModel model)
            where T : ICreatedBy<TUser>
        {
            model.Property<T>(p => p.CreatedByUser).SetReadOnly();
            model.Property<T>(p => p.CreatedByUserId).SetReadOnly();
        }

        public void ConfigureIDbObject<T, TKey>(EdmModel model)
            where T : IDbObject<TKey>
        {
            model.Property<T>(p => p.Id).SetReadOnly();
        }
    }
}