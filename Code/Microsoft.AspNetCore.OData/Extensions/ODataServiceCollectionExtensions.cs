// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Routing;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.OData.Abstracts;
using Microsoft.AspNetCore.OData.Common;
using Microsoft.AspNetCore.OData.Formatter.Deserialization;
using Microsoft.AspNetCore.OData.Formatter.Serialization;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.OData;

namespace Microsoft.AspNetCore.OData.Extensions
{
    public interface IODataOutputFormatterProvider
    {
        IOutputFormatter OutputFormatter { get; }
    }

    public class DefaultODataOutputFormatterProvider<TOutputFormatter> : IODataOutputFormatterProvider
        where TOutputFormatter : IOutputFormatter, new()
    {
        public IOutputFormatter OutputFormatter { get; } = new TOutputFormatter();
    }

    /// <summary>
    /// Provides extension methods to add odata services.
    /// </summary>
    public static class ODataServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureODataOutputFormatterProvider(
            [NotNull] this IServiceCollection services,
            Func<IServiceProvider, IODataOutputFormatterProvider> resolver)
        {
            return services.AddTransient(resolver);
        }

        public static IServiceCollection ConfigureODataSerializerProvider<TODataSerializerProvider>(
            [NotNull] this IServiceCollection services)
            where TODataSerializerProvider : class, IODataSerializerProvider
        {
            return services.AddTransient<IODataSerializerProvider, TODataSerializerProvider>();
        }

        public static IServiceCollection ConfigureODataSerializerProvider(
            [NotNull] this IServiceCollection services,
            Func<IServiceProvider, IODataSerializerProvider> provider)
        {
            return services.AddTransient(provider);
        }

        public static IServiceCollection ConfigureODataOutputFormatter<TOutputFormatter>(
            [NotNull] this IServiceCollection services)
            where TOutputFormatter : class, IOutputFormatter, new()
        {
            return
                services.ConfigureODataOutputFormatterProvider<DefaultODataOutputFormatterProvider<TOutputFormatter>>();
        }

        public static IServiceCollection ConfigureODataOutputFormatterProvider<TOutputFormatterProvider>(
            [NotNull] this IServiceCollection services)
            where TOutputFormatterProvider : class, IODataOutputFormatterProvider
        {
            return services.AddTransient<IODataOutputFormatterProvider, TOutputFormatterProvider>();
        }

        /// <summary>
        /// Adds essential OData services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>An <see cref="IODataCoreBuilder"/> that can be used to further configure the OData services.</returns>
        public static IODataCoreBuilder AddOData(this IServiceCollection services)
        {
            if (services == null)
            {
                throw Error.ArgumentNull(nameof(services));
            }

            // add the default OData lib services into service collection.
            IContainerBuilder builder = new DefaultContainerBuilder(services);
            builder.AddDefaultODataServices();
            var currentServices = services.BuildServiceProvider();
            if (currentServices.GetService<IODataOutputFormatterProvider>() == null)
            {
                services.ConfigureODataOutputFormatter<ModernOutputFormatter>();
            }

            // Options
            services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<ODataOptions>, ODataOptionsSetup>());

            // SerializerProvider
            services.AddSingleton<IODataSerializerProvider, DefaultODataSerializerProvider>();

            // Deserializer provider
            services.AddSingleton<IODataDeserializerProvider, DefaultODataDeserializerProvider>();

            services.AddMvcCore(options =>
            {
                options.InputFormatters.Insert(0, new ModernInputFormatter());

                foreach (var outputFormatter in ODataOutputFormatters.Create())
                {
                    options.OutputFormatters.Insert(0, outputFormatter);
                }
                var formatter = services.BuildServiceProvider().GetService<IODataOutputFormatterProvider>().OutputFormatter;
                options.OutputFormatters.Insert(0, formatter);
            });

            services.AddSingleton<IActionSelector, ODataActionSelector>();
            services.AddSingleton<IETagHandler, DefaultODataETagHandler>();

            // Routing
            services.AddSingleton<IODataPathHandler, DefaultODataPathHandler>();
            services.AddSingleton<IODataPathTemplateHandler, DefaultODataPathTemplateHandler>();

            // Assembly
            services.AddSingleton<IAssemblyProvider, DefaultAssemblyProvider>();

            services.AddTransient<FilterBinder>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            //serviceCollection.AddSingleton(model);
            services.AddTransient<ODataQuerySettings>();
            // Serializers
            AddDefaultSerializers(services);

            // Deserializers
            AddDefaultDeserializers(services);
            //EnableQueryAttribute.ServiceProvider = services.BuildServiceProvider();

            return new ODataCoreBuilder(services);
        }

        /// <summary>
        /// Adds essential OData services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="setupAction">An <see cref="Action{MvcOptions}"/> to configure the provided <see cref="ODataOptions"/>.</param>
        /// <returns>An <see cref="IODataCoreBuilder"/> that can be used to further configure the MVC services.</returns>
        public static IODataCoreBuilder AddOData([NotNull] this IServiceCollection services,
            Action<ODataOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            IODataCoreBuilder builder = services.AddOData();
            builder.Services.Configure(setupAction);
            return builder;
        }

        public static IODataCoreBuilder AddOData([NotNull] this IServiceCollection services,
            Action<IServiceCollection> configSerivces)
        {
            IODataCoreBuilder builder = services.AddOData();
            configSerivces(services); // for customers override services
            return builder;
        }

        public static void AddApiContext<T>(
           [NotNull] this ODataCoreBuilder builder,
           [NotNull] string prefix)
            where T : class
        {
            builder.Register<T>(prefix);
        }

        internal static void AddODataCoreServices(this IServiceCollection services)
        {

        }

        private static void AddDefaultSerializers(IServiceCollection services)
        {
            services.AddSingleton<ODataMetadataSerializer>();
            services.AddSingleton<ODataServiceDocumentSerializer>();

            services.AddSingleton<ODataEnumSerializer>();
            services.AddSingleton<ODataPrimitiveSerializer>();
            services.AddSingleton<ODataDeltaFeedSerializer>();
            services.AddSingleton<ODataResourceSetSerializer>();
            services.AddSingleton<ODataCollectionSerializer>();
            services.AddSingleton<ODataResourceSerializer>();
            services.AddSingleton<ODataServiceDocumentSerializer>();
            services.AddSingleton<ODataEntityReferenceLinkSerializer>();
            services.AddSingleton<ODataEntityReferenceLinksSerializer>();
            services.AddSingleton<ODataErrorSerializer>();
            services.AddSingleton<ODataRawValueSerializer>();
        }

        private static void AddDefaultDeserializers(IServiceCollection services)
        {
            services.AddSingleton<ODataResourceDeserializer>();
            services.AddSingleton<ODataEnumDeserializer>();
            services.AddSingleton<ODataPrimitiveDeserializer>();
            services.AddSingleton<ODataResourceSetDeserializer>();
            services.AddSingleton<ODataCollectionDeserializer>();
            services.AddSingleton<ODataEntityReferenceLinkDeserializer>();
            services.AddSingleton<ODataActionPayloadDeserializer>();
        }
    }
}
