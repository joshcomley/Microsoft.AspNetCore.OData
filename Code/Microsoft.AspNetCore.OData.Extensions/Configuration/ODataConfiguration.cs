using System;
using System.Reflection;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Brandless.AspNetCore.OData.Extensions.Configuration
{
    public class ODataConfiguration
    {
        public static ODataConfigurationResult GetEdmModel<TService, TAssemblySource>(IServiceProvider serviceProvider,
            string @namespace)
        {
            return GetEdmModel<TService>(serviceProvider, @namespace, typeof(TAssemblySource).Assembly);
        }

        public static ODataConfigurationResult GetEdmModel<TService>(IServiceProvider serviceProvider, string @namespace, Assembly assembly)
        {
            var assemblyProvider = serviceProvider.GetService<IAssemblyProvider>();
            ODataModelBuilder modelBuilder;
            var model = new ODataConfigurator<TService>(@namespace, assembly).Configure(assemblyProvider, serviceProvider, out modelBuilder);
            return new ODataConfigurationResult(model, modelBuilder);
        }
    }
}