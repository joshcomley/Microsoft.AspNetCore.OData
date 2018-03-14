using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Brandless.AspNetCore.OData.Extensions.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Builder;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;

namespace Brandless.AspNetCore.OData.Extensions.Configuration
{
    public class ODataMethodBuilder
    {
        private static MethodInfo MethodTypedInfo { get; set; }
        static ODataMethodBuilder()
        {
            MethodTypedInfo = typeof(ODataMethodBuilder)
                .GetMethod(nameof(MethodTyped), BindingFlags.Static | BindingFlags.NonPublic);
        }

        public static OperationConfiguration BuildMethod(MethodInfo method, ODataConventionModelBuilder builder,
            ODataFunctionAttribute functionAttribute, ODataActionAttribute actionAttribute, out string bindingName)
        {
            OperationConfiguration configuration = null;
            bindingName = null;
            if (functionAttribute != null)
            {
                if (functionAttribute.ForType != null || functionAttribute.ForCollection != null)
                {
                    if (functionAttribute.ForType != null && functionAttribute.ForCollection != null)
                    {
                        throw new ArgumentException(
                            $"Must specify either {nameof(ODataFunctionAttribute.ForType)} or {nameof(ODataFunctionAttribute.ForCollection)}, not both");
                    }

                    if (functionAttribute.ForType != null)
                    {
                        bindingName = functionAttribute.BindingName ?? "key";
                    }
                    configuration = (OperationConfiguration)MethodTypedInfo.MakeGenericMethod(
                            functionAttribute.ForType ?? functionAttribute.ForCollection)
                        .Invoke(
                            null,
                            new object[]
                            {
                                builder,
                                method.Name,
                                false,
                                functionAttribute.ForCollection != null
                            });
                }
                else
                {
                    configuration = builder.Function(method.Name);
                }
            }

            if (actionAttribute != null)
            {
                if (actionAttribute.ForType != null || actionAttribute.ForCollection != null)
                {
                    if (actionAttribute.ForType != null && actionAttribute.ForCollection != null)
                    {
                        throw new ArgumentException(
                            $"Must specify either {nameof(ODataActionAttribute.ForType)} or {nameof(ODataActionAttribute.ForCollection)}, not both");
                    }
                    if (actionAttribute.ForType != null)
                    {
                        bindingName = actionAttribute.BindingName ?? "key";
                    }

                    configuration = (OperationConfiguration)MethodTypedInfo.MakeGenericMethod(
                            actionAttribute.ForType ?? actionAttribute.ForCollection)
                        .Invoke(
                            null,
                            new object[]
                            {
                                builder,
                                method.Name,
                                true,
                                actionAttribute.ForCollection != null
                            });
                }
                else
                {
                    configuration = builder.Action(method.Name);
                }
            }

            return configuration;
        }

        private static OperationConfiguration MethodTyped<T>(ODataConventionModelBuilder builder, string name, bool action, bool collection)
        where T : class
        {
            OperationConfiguration configuration;
            var entityType = builder.EntityType<T>();
            if (collection)
            {
                if (action)
                {
                    configuration = entityType.Collection.Action(name);
                }
                else
                {
                    configuration = entityType.Collection.Function(name);
                }
            }
            else
            {
                if (action)
                {
                    configuration = entityType.Action(name);
                }
                else
                {
                    configuration = entityType.Function(name);
                }
            }

            return configuration;
        }
    }
    public class ODataConfigurator<TService>
    {
        public List<Action<EdmModel>> ModelConfigurators { get; } = new List<Action<EdmModel>>();

        public ODataConfigurator(string @namespace, Assembly configurationAssembly)
        {
            Namespace = @namespace;
            ConfigurationAssembly = configurationAssembly;
        }

        public string Namespace { get; }
        public Assembly ConfigurationAssembly { get; }

        //public IEdmModel Configure(IApplicationBuilder app)
        //{
        //    ODataModelBuilder modelBuilder;
        //    return Configure(app, out modelBuilder);
        //}

        //public IEdmModel Configure(IApplicationBuilder app, out ODataModelBuilder modelBuilder)
        //{
        //    return Configure(app, out modelBuilder);
        //}

        public IEdmModel Configure(IAssemblyProvider assemblyProvider, IServiceProvider serviceProvider, out ODataModelBuilder modelBuilder)
        {
            // OData actions are HTTP POST
            // OData functions are HTTP GET
            var builder = new ODataConventionModelBuilder(assemblyProvider);
            builder.Namespace = Namespace;

            var serviceType = typeof(TService);
            var entitySetMethod = builder.GetType().GetMethod(nameof(ODataConventionModelBuilder.EntitySet));
            foreach (var property in serviceType.GetProperties())
            {
                if (typeof(IQueryable).IsAssignableFrom(property.PropertyType) && property.PropertyType.IsGenericType)
                {
                    entitySetMethod.MakeGenericMethod(property.PropertyType.GenericTypeArguments[0])
                        .Invoke(builder, new object[] { property.Name });
                }
            }

            // Get the actions and functions into the model
            ApplyAttributes(assemblyProvider, builder);

            ApplyCustomConfigurators(serviceProvider, builder);

            var model = builder.GetEdmModel() as EdmModel;

            foreach (var modelConfigurator in ModelConfigurators)
            {
                modelConfigurator(model);
            }
            modelBuilder = builder;
            return model;
        }

        private static void ApplyAttributes(IAssemblyProvider assemblyProvider, ODataConventionModelBuilder builder)
        {
            var allTypes = assemblyProvider.CandidateAssemblies.SelectMany(a => a.ExportedTypes);
            foreach (var type in allTypes)
            {
                MethodInfo[] publicMethods = null;
                try
                {
                    publicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                }
                catch (FileLoadException) { }

                if (publicMethods != null)
                {
                    foreach (var method in publicMethods)
                    {
                        if (!method.IsSpecialName)
                        {
                            try
                            {
                                var functionAttribute = method.GetCustomAttribute<ODataFunctionAttribute>();
                                var actionAttribute = method.GetCustomAttribute<ODataActionAttribute>();
                                if (functionAttribute == null && actionAttribute == null)
                                {
                                    continue;
                                }


                                var entityClrType = TypeHelper.GetImplementedIEnumerableType(method.ReturnType) ??
                                                    method.ReturnType;
                                entityClrType = entityClrType.UnwrapTask();
                                PrimitiveTypeConfiguration primitiveEntityType = null;
                                EntityTypeConfiguration entityType = null;
                                var isActionResult = typeof(IActionResult).IsAssignableFrom(entityClrType);
                                if (!isActionResult)
                                {
                                    if (entityClrType.IsPrimitiveType())
                                    {
                                        primitiveEntityType = builder.AddPrimitiveType(entityClrType);
                                    }
                                    else
                                    {
                                        entityType = builder.AddEntityType(entityClrType);
                                    }
                                }

                                var configuration =
                                    ODataMethodBuilder.BuildMethod(
                                        method,
                                        builder,
                                        functionAttribute,
                                        actionAttribute,
                                        out var bindingName);

                                if (configuration != null)
                                {
                                    if (!isActionResult)
                                    {
                                        if (primitiveEntityType == null)
                                        {
                                            configuration.ReturnType = entityType;
                                        }
                                        else
                                        {
                                            configuration.ReturnType = primitiveEntityType;
                                        }
                                    }
                                    configuration.IsComposable = true;
                                    configuration.NavigationSource =
                                        builder.NavigationSources.FirstOrDefault(n => n.EntityType == entityType);

                                    foreach (var parameterInfo in method.GetParameters())
                                    {
                                        if (!string.IsNullOrWhiteSpace(bindingName) &&
                                            parameterInfo.Name == bindingName)
                                        {
                                            continue;
                                        }

                                        if (actionAttribute != null)
                                        {
                                            if (parameterInfo.GetCustomAttributes(typeof(FromBodyAttribute)) != null)
                                            {
                                                if (parameterInfo.ParameterType.IsPrimitiveType())
                                                {
                                                    AddParameter(builder, configuration, parameterInfo.ParameterType, parameterInfo.Name);
                                                }
                                                else
                                                {
                                                    foreach (var property in parameterInfo.ParameterType
                                                        .GetProperties())
                                                    {
                                                        AddParameter(builder, configuration, property.PropertyType, property.Name);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            AddParameter(builder, configuration, parameterInfo.ParameterType, parameterInfo.Name);
                                        }
                                    }
                                }
                            }
                            catch (FileLoadException) { }
                        }
                    }
                }
            }
        }

        private static void AddParameter(
            ODataConventionModelBuilder builder, 
            OperationConfiguration configuration,
            Type pType, 
            string name)
        {
            if (pType.IsPrimitiveType())
            {
                var primitiveType = builder.AddPrimitiveType(pType);
                configuration.AddParameter(name, primitiveType);
            }
            else
            {
                if (pType.IsCollection())
                {
                    if (pType.GenericTypeArguments[0].GetTypeInfo()
                        .IsPrimitive)
                    {
                        var parameterType = builder.AddPrimitiveType(
                            pType.GenericTypeArguments[0]);
                        var collectionTypeConfig = new CollectionTypeConfiguration(
                            parameterType,
                            pType.GenericTypeArguments[0]);
                        configuration.AddParameter(name, collectionTypeConfig);
                    }
                    else
                    {
                        var parameterType = builder.AddEntityType(
                            pType.GenericTypeArguments[0]);
                        var collectionTypeConfig = new CollectionTypeConfiguration(
                            parameterType,
                            pType.GenericTypeArguments[0]);
                        configuration.AddParameter(name, collectionTypeConfig);
                    }
                }
                else
                {
                    var parameterType = builder.AddEntityType(pType);
                    configuration.AddParameter(name, parameterType);
                }
            }
        }

        private void ApplyCustomConfigurators(IServiceProvider serviceProvider, ODataConventionModelBuilder builder)
        {
            var configurators = new List<IODataEntitySetConfigurator>();
            var types = ConfigurationAssembly.DefinedTypes;
            foreach (var type in types)
            {
                if (typeof(IODataEntitySetConfigurator).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                {
                    configurators.Add((IODataEntitySetConfigurator)ActivatorUtilities.CreateInstance(serviceProvider, type));
                }
            }

            foreach (var configurator in configurators)
            {
                configurator.Configure(builder, action => ModelConfigurators.Add(action));
            }
        }
    }
}
