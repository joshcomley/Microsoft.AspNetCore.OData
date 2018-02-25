using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;

namespace Brandless.AspNetCore.OData.Extensions.Configuration
{
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

            var model = builder.GetEdmModel() as EdmModel;
            foreach (var modelConfigurator in ModelConfigurators)
            {
                modelConfigurator(model);
            }
            modelBuilder = builder;
            return model;
        }
    }
}
