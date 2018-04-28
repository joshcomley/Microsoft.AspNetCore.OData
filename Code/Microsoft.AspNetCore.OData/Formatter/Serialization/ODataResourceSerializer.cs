﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Builder;
using Microsoft.AspNetCore.OData.Common;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using ODataAction = Microsoft.OData.ODataAction;
using ODataFunction = Microsoft.OData.ODataFunction;
using ODataMessageWriter = Microsoft.OData.ODataMessageWriter;
using ODataNullValue = Microsoft.OData.ODataNullValue;
using ODataOperation = Microsoft.OData.ODataOperation;
using ODataPayloadKind = Microsoft.OData.ODataPayloadKind;
using ODataProperty = Microsoft.OData.ODataProperty;
using ODataWriter = Microsoft.OData.ODataWriter;

namespace Microsoft.AspNetCore.OData.Formatter.Serialization
{
    /// <summary>
    /// ODataSerializer for serializing instances of <see cref="IEdmEntityType"/> or <see cref="IEdmComplexType"/>
    /// </summary>
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Relies on many ODataLib classes.")]
    public class ODataResourceSerializer : ODataEdmTypeSerializer
    {
        private const string Resource = "Resource";

        /// <summary>
        /// Initializes a new instance of <see cref="ODataResourceSerializer"/>.
        /// </summary>
        public ODataResourceSerializer(IODataSerializerProvider serializerProvider)
            : base(ODataPayloadKind.Resource, serializerProvider)
        {
        }

        /// <inheritdoc />
        public override async Task WriteObjectAsync(object graph, Type type, ODataMessageWriter messageWriter,
            ODataSerializerContext writeContext)
        {
            if (messageWriter == null)
            {
                throw Error.ArgumentNull("messageWriter");
            }

            if (writeContext == null)
            {
                throw Error.ArgumentNull("writeContext");
            }

            IEdmNavigationSource navigationSource = writeContext.NavigationSource;
            if (navigationSource == null)
            {
                throw new SerializationException(SRResources.NavigationSourceMissingDuringSerialization);
            }

            var path = writeContext.Path;
            if (path == null)
            {
                throw new SerializationException(SRResources.ODataPathMissing);
            }

            ODataWriter writer = messageWriter.CreateODataResourceWriter(navigationSource, path.EdmType as IEdmEntityType);
            await WriteObjectInlineAsync(graph, navigationSource.EntityType().ToEdmTypeReference(isNullable: false), writer, writeContext);
            writer.Flush();
        }

        /// <inheritdoc />
        public override async Task WriteObjectInlineAsync(object graph, IEdmTypeReference expectedType, ODataWriter writer,
            ODataSerializerContext writeContext)
        {
            if (writer == null)
            {
                throw Error.ArgumentNull("writer");
            }

            if (writeContext == null)
            {
                throw Error.ArgumentNull("writeContext");
            }

            if (graph == null || graph is NullEdmComplexObject)
            {
                throw new SerializationException(Error.Format(SRResources.CannotSerializerNull, Resource));
            }
            else
            {
                await WriteResourceAsync(graph, writer, writeContext, expectedType);
            }
        }

        /// <summary>
        /// Writes the given object specified by the parameter graph as a part of an existing OData message using the given
        /// deltaWriter and the writeContext.
        /// </summary>
        /// <param name="graph">The object to be written.</param>
        /// <param name="expectedType">The expected EDM type of the object represented by <paramref name="graph"/>.</param>
        /// <param name="writer">The <see cref="ODataDeltaWriter" /> to be used for writing.</param>
        /// <param name="writeContext">The <see cref="ODataSerializerContext"/>.</param>
        public virtual async Task WriteDeltaObjectInline(object graph, IEdmTypeReference expectedType, ODataDeltaWriter writer,
           ODataSerializerContext writeContext)
        {
            if (writer == null)
            {
                throw Error.ArgumentNull("writer");
            }

            if (writeContext == null)
            {
                throw Error.ArgumentNull("writeContext");
            }

            if (graph == null)
            {
                throw new SerializationException(Error.Format(SRResources.CannotSerializerNull, Resource));
            }
            else
            {
                await WriteDeltaResource(graph, writer, writeContext);
            }
        }

        private Task WriteDeltaResource(object graph, ODataDeltaWriter writer, ODataSerializerContext writeContext)
        {
            Contract.Assert(writeContext != null);

            IEdmStructuredTypeReference structuredType = GetResourceType(graph, writeContext);
            ResourceContext resourceContext = new ResourceContext(writeContext, structuredType, graph);
            SelectExpandNode selectExpandNode = CreateSelectExpandNode(resourceContext);
            if (selectExpandNode != null)
            {
                ODataResource resource = CreateResource(selectExpandNode, resourceContext);
                if (resource != null)
                {
                    writer.WriteStart(resource);
                    //TODO: Need to add support to write Navigation Links using Delta Writer
                    //https://github.com/OData/odata.net/issues/155
                    writer.WriteEnd();
                }
            }

            return Task.FromResult<object>(null);
        }

        private IEnumerable<ODataProperty> CreateODataPropertiesFromDynamicType(EdmEntityType entityType, object graph, Dictionary<IEdmTypeReference, object> navigationProperties = null)
        {
            var properties = new List<ODataProperty>();
            var dynamicObject = graph as DynamicTypeWrapper;
            var entityProperties = entityType.Properties().ToDictionary(p => p.Name);
            foreach (var keyValue in dynamicObject.Values)
            {
                object value;
                ODataProperty property = null;
                if (entityProperties.ContainsKey(keyValue.Key))
                {
                    var prop = entityProperties[keyValue.Key];
                    if (dynamicObject.TryGetPropertyValue(prop.Name, out value))
                    {
                        bool isNavigationProperty = false;
                        if (navigationProperties != null)
                        {
                            if (entityType != null)
                            {
                                var navigationProperty =
                                    entityType.NavigationProperties().FirstOrDefault(p => p.Name.Equals(prop.Name));
                                if (navigationProperty != null)
                                {
                                    navigationProperties.Add(navigationProperty.Type, value);
                                    isNavigationProperty = true;
                                }
                            }
                        }

                        if (!isNavigationProperty)
                        {
                            if (value != null && value.GetType().IsDynamicTypeWrapper())
                            {
                                property = new ODataProperty
                                {
                                    Name = prop.Name,
                                    Value = new ODataResource
                                    {
                                        Properties = CreateODataPropertiesFromDynamicType(entityType, value)
                                    }
                                };
                            }
                            else
                            {
                                property = new ODataProperty
                                {
                                    Name = prop.Name,
                                    Value = value
                                };
                            }
                        }
                    }
                }
                else
                {
                    property = new ODataProperty
                    {
                        Name = keyValue.Key,
                        Value = keyValue.Value,
                    };
                }

                if (property != null)
                {
                    properties.Add(property);
                }
            }
            return properties;
        }

        private async Task WriteDynamicTypeEntry(object graph, ODataWriter writer, IEdmTypeReference expectedType,
            ODataSerializerContext writeContext)
        {
            var navigationProperties = new Dictionary<IEdmTypeReference, object>();
            var entityType = expectedType.Definition as EdmEntityType;
            var entry = new ODataResource
            {
                TypeName = expectedType.FullName(),
                Properties = CreateODataPropertiesFromDynamicType(entityType, graph, navigationProperties)
            };

            entry.IsTransient = true;
            writer.WriteStart(entry);
            foreach (IEdmTypeReference type in navigationProperties.Keys)
            {
                var entityContext = new ResourceContext(writeContext, expectedType.AsEntity(), graph);
                var navigationProperty = entityType.NavigationProperties().FirstOrDefault(p => p.Type.Equals(type));
                var navigationLink = await CreateNavigationLinkAsync(navigationProperty, entityContext);
                if (navigationLink != null)
                {
                    writer.WriteStart(navigationLink);
                    await WriteDynamicTypeEntry(navigationProperties[type], writer, type, writeContext);
                    writer.WriteEnd();
                }
            }

            writer.WriteEnd();
        }

        private async Task WriteResourceAsync(object graph, ODataWriter writer, ODataSerializerContext writeContext,
             IEdmTypeReference expectedType)
        {
            Contract.Assert(writeContext != null);


            if (graph.GetType().IsDynamicTypeWrapper())
            {
                await WriteDynamicTypeEntry(graph, writer, expectedType, writeContext);
                return;
            }

            IEdmStructuredTypeReference structuredType = GetResourceType(graph, writeContext);
            var selectExpandWrapper = graph as ISelectExpandWrapper;
            if (selectExpandWrapper != null && selectExpandWrapper.Instance == null)
            {
                return;
            }
            await BeforeWriteAsync(writeContext, selectExpandWrapper?.Instance ?? graph, structuredType);
            ResourceContext resourceContext = new ResourceContext(writeContext, structuredType, graph);
            SelectExpandNode selectExpandNode = CreateSelectExpandNode(resourceContext);
            if (selectExpandNode != null)
            {
                ODataResource resource = CreateResource(selectExpandNode, resourceContext);
                if (resource != null)
                {
                    writer.WriteStart(resource);
                    await WriteComplexProperties(selectExpandNode.SelectedComplexProperties, resourceContext, writer);
                    await WriteDynamicComplexPropertiesAsync(resourceContext, writer);
                    await WriteNavigationLinksAsync(selectExpandNode.SelectedNavigationProperties, resourceContext, writer);
                    await WriteExpandedNavigationPropertiesAsync(selectExpandNode.ExpandedNavigationProperties,
                        resourceContext, writer);
                    writer.WriteEnd();
                }
            }
        }

        public virtual Task BeforeWriteAsync(ODataSerializerContext context,
            object entity,
            IEdmStructuredTypeReference structuredType)
        {
            return Task.FromResult(false);
        }

        /// <summary>
        /// Creates the <see cref="SelectExpandNode"/> that describes the set of properties and actions to select and expand while writing this entity.
        /// </summary>
        /// <param name="resourceContext">Contains the entity instance being written and the context.</param>
        /// <returns>
        /// The <see cref="SelectExpandNode"/> that describes the set of properties and actions to select and expand while writing this entity.
        /// </returns>
        public virtual SelectExpandNode CreateSelectExpandNode(ResourceContext resourceContext)
        {
            if (resourceContext == null)
            {
                throw Error.ArgumentNull("resourceContext");
            }

            ODataSerializerContext writeContext = resourceContext.SerializerContext;
            IEdmStructuredType structuredType = resourceContext.StructuredType;

            object selectExpandNode;
            Tuple<SelectExpandClause, IEdmStructuredType> key = Tuple.Create(writeContext.SelectExpandClause, structuredType);
            if (!writeContext.Items.TryGetValue(key, out selectExpandNode))
            {
                // cache the selectExpandNode so that if we are writing a feed we don't have to construct it again.
                selectExpandNode = new SelectExpandNode(structuredType, writeContext);
                writeContext.Items[key] = selectExpandNode;
            }

            return selectExpandNode as SelectExpandNode;
        }

        /// <summary>
        /// Creates the <see cref="ODataResource"/> to be written while writing this resource.
        /// </summary>
        /// <param name="selectExpandNode">The <see cref="SelectExpandNode"/> describing the response graph.</param>
        /// <param name="resourceContext">The context for the resource instance being written.</param>
        /// <returns>The created <see cref="ODataResource"/>.</returns>
        public virtual ODataResource CreateResource(SelectExpandNode selectExpandNode, ResourceContext resourceContext)
        {
            if (selectExpandNode == null)
            {
                throw Error.ArgumentNull("selectExpandNode");
            }

            if (resourceContext == null)
            {
                throw Error.ArgumentNull("resourceContext");
            }

            string typeName = resourceContext.StructuredType.FullTypeName();

            ODataResource resource = new ODataResource
            {
                TypeName = typeName,
                Properties = CreateStructuralPropertyBag(selectExpandNode.SelectedStructuralProperties, resourceContext),
            };

            // Try to add the dynamic properties if the structural type is open.
            AppendDynamicProperties(resource, selectExpandNode, resourceContext);

            IEnumerable<ODataAction> actions = CreateODataActions(selectExpandNode.SelectedActions, resourceContext);
            foreach (ODataAction action in actions)
            {
                resource.AddAction(action);
            }

            IEnumerable<ODataFunction> functions = CreateODataFunctions(selectExpandNode.SelectedFunctions, resourceContext);
            foreach (ODataFunction function in functions)
            {
                resource.AddFunction(function);
            }

            IEdmStructuredType pathType = GetODataPathType(resourceContext.SerializerContext);
            if (resourceContext.StructuredType.TypeKind == EdmTypeKind.Complex)
            {
                AddTypeNameAnnotationAsNeededForComplex(resource, resourceContext.SerializerContext.MetadataLevel);
            }
            else
            {
                AddTypeNameAnnotationAsNeeded(resource, pathType, resourceContext.SerializerContext.MetadataLevel);
            }

            if (resourceContext.StructuredType.TypeKind == EdmTypeKind.Entity && resourceContext.NavigationSource != null)
            {
                if (!(resourceContext.NavigationSource is IEdmContainedEntitySet))
                {
                    IEdmModel model = resourceContext.SerializerContext.Model;
                    NavigationSourceLinkBuilderAnnotation linkBuilder = model.GetNavigationSourceLinkBuilder(resourceContext.NavigationSource);
                    EntitySelfLinks selfLinks = linkBuilder.BuildEntitySelfLinks(resourceContext, resourceContext.SerializerContext.MetadataLevel);

                    if (selfLinks.IdLink != null)
                    {
                        resource.Id = selfLinks.IdLink;
                    }

                    if (selfLinks.ReadLink != null)
                    {
                        resource.ReadLink = selfLinks.ReadLink;
                    }

                    if (selfLinks.EditLink != null)
                    {
                        resource.EditLink = selfLinks.EditLink;
                    }
                }

                string etag = CreateETag(resourceContext);
                if (etag != null)
                {
                    resource.ETag = etag;
                }
            }

            return resource;
        }

        /// <summary>
        /// Appends the dynamic properties of primitive, enum or the collection of them into the given <see cref="ODataResource"/>.
        /// If the dynamic property is a property of the complex or collection of complex, it will be saved into
        /// the dynamic complex properties dictionary of <paramref name="resourceContext"/> and be written later.
        /// </summary>
        /// <param name="resource">The <see cref="ODataResource"/> describing the resource.</param>
        /// <param name="selectExpandNode">The <see cref="SelectExpandNode"/> describing the response graph.</param>
        /// <param name="resourceContext">The context for the resource instance being written.</param>
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Relies on many classes.")]
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "These are simple conversion function and cannot be split up.")]
        public virtual void AppendDynamicProperties(ODataResource resource, SelectExpandNode selectExpandNode,
            ResourceContext resourceContext)
        {
            Contract.Assert(resource != null);
            Contract.Assert(selectExpandNode != null);
            Contract.Assert(resourceContext != null);

            if (!resourceContext.StructuredType.IsOpen || // non-open type
                (!selectExpandNode.SelectAllDynamicProperties && !selectExpandNode.SelectedDynamicProperties.Any()))
            {
                return;
            }

            bool nullDynamicPropertyEnabled = false;
            if (resourceContext.Request != null)
            {
                // TODO: 
                /*
                HttpConfiguration configuration = resourceContext.Request.GetConfiguration();
                if (configuration != null)
                {
                    nullDynamicPropertyEnabled = configuration.HasEnabledNullDynamicProperty();
                }*/
            }

            IEdmStructuredType structuredType = resourceContext.StructuredType;
            IEdmStructuredObject structuredObject = resourceContext.EdmObject;
            object value;
            IDelta delta = structuredObject as IDelta;
            if (delta == null)
            {
                PropertyInfo dynamicPropertyInfo = EdmLibHelpers.GetDynamicPropertyDictionary(structuredType,
                    resourceContext.EdmModel);
                if (dynamicPropertyInfo == null || structuredObject == null ||
                    !structuredObject.TryGetPropertyValue(dynamicPropertyInfo.Name, out value) || value == null)
                {
                    return;
                }
            }
            else
            {
                value = ((EdmStructuredObject)structuredObject).TryGetDynamicProperties();
            }

            IDictionary<string, object> dynamicPropertyDictionary = (IDictionary<string, object>)value;

            // Build a HashSet to store the declared property names.
            // It is used to make sure the dynamic property name is different from all declared property names.
            HashSet<string> declaredPropertyNameSet = new HashSet<string>(resource.Properties.Select(p => p.Name));
            List<ODataProperty> dynamicProperties = new List<ODataProperty>();
            IEnumerable<KeyValuePair<string, object>> dynamicPropertiesToSelect =
                dynamicPropertyDictionary.Where(
                    x =>
                        !selectExpandNode.SelectedDynamicProperties.Any() ||
                        selectExpandNode.SelectedDynamicProperties.Contains(x.Key));
            foreach (KeyValuePair<string, object> dynamicProperty in dynamicPropertiesToSelect)
            {
                if (String.IsNullOrEmpty(dynamicProperty.Key))
                {
                    continue;
                }

                if (dynamicProperty.Value == null)
                {
                    if (nullDynamicPropertyEnabled)
                    {
                        dynamicProperties.Add(new ODataProperty
                        {
                            Name = dynamicProperty.Key,
                            Value = new ODataNullValue()
                        });
                    }

                    continue;
                }

                if (declaredPropertyNameSet.Contains(dynamicProperty.Key))
                {
                    throw Error.InvalidOperation(SRResources.DynamicPropertyNameAlreadyUsedAsDeclaredPropertyName,
                        dynamicProperty.Key, structuredType.FullTypeName());
                }

                IEdmTypeReference edmTypeReference = resourceContext.SerializerContext.GetEdmType(dynamicProperty.Value,
                    dynamicProperty.Value.GetType());
                if (edmTypeReference == null)
                {
                    throw Error.NotSupported(SRResources.TypeOfDynamicPropertyNotSupported,
                        dynamicProperty.Value.GetType().FullName, dynamicProperty.Key);
                }

                if (edmTypeReference.IsStructured() ||
                    (edmTypeReference.IsCollection() && edmTypeReference.AsCollection().ElementType().IsStructured()))
                {
                    if (resourceContext.DynamicComplexProperties == null)
                    {
                        resourceContext.DynamicComplexProperties = new ConcurrentDictionary<string, object>();
                    }

                    resourceContext.DynamicComplexProperties.Add(dynamicProperty);
                }
                else
                {
                    ODataEdmTypeSerializer propertySerializer = SerializerProvider.GetEdmTypeSerializer(resourceContext.Context, edmTypeReference);
                    if (propertySerializer == null)
                    {
                        throw Error.NotSupported(SRResources.DynamicPropertyCannotBeSerialized, dynamicProperty.Key,
                            edmTypeReference.FullName());
                    }

                    dynamicProperties.Add(propertySerializer.CreateProperty(
                        dynamicProperty.Value, edmTypeReference, dynamicProperty.Key, resourceContext.SerializerContext));
                }
            }

            if (dynamicProperties.Any())
            {
                resource.Properties = resource.Properties.Concat(dynamicProperties);
            }
        }

        /// <summary>
        /// Creates the ETag for the given entity.
        /// </summary>
        /// <param name="resourceContext">The context for the resource instance being written.</param>
        /// <returns>The created ETag.</returns>
        public virtual string CreateETag(ResourceContext resourceContext)
        {
            if (resourceContext.Request != null)
            {
                HttpContext httpContext = resourceContext.Request.HttpContext;
                if (httpContext == null)
                {
                    throw Error.InvalidOperation("TODO: "/*SRResources.RequestMustContainConfiguration*/);
                }

                IEdmModel model = resourceContext.EdmModel;
                IEdmEntitySet entitySet = resourceContext.NavigationSource as IEdmEntitySet;

                IEnumerable<IEdmStructuralProperty> concurrencyProperties;
                if (model != null && entitySet != null)
                {
                    concurrencyProperties = model.GetConcurrencyProperties(entitySet).OrderBy(c => c.Name);
                }
                else
                {
                    concurrencyProperties = Enumerable.Empty<IEdmStructuralProperty>();
                }

                IDictionary<string, object> properties = new Dictionary<string, object>();
                foreach (IEdmStructuralProperty etagProperty in concurrencyProperties)
                {
                    properties.Add(etagProperty.Name, resourceContext.GetPropertyValue(etagProperty.Name));
                }
                EntityTagHeaderValue etagHeaderValue = httpContext.RequestServices.GetRequiredService<IETagHandler>().CreateETag(properties);
                if (etagHeaderValue != null)
                {
                    return etagHeaderValue.ToString();
                }
            }

            return null;
        }

        private async Task WriteNavigationLinksAsync(
            IEnumerable<IEdmNavigationProperty> navigationProperties, ResourceContext resourceContext, ODataWriter writer)
        {
            Contract.Assert(resourceContext != null);

            IEnumerable<ODataNestedResourceInfo> navigationLinks = await CreateNavigationLinksAsync(navigationProperties, resourceContext);
            foreach (ODataNestedResourceInfo navigationLink in navigationLinks)
            {
                writer.WriteStart(navigationLink);
                writer.WriteEnd();
            }
        }

        private async Task WriteComplexProperties(IEnumerable<IEdmStructuralProperty> complexProperties,
            ResourceContext resourceContext, ODataWriter writer)
        {
            Contract.Assert(complexProperties != null);
            Contract.Assert(resourceContext != null);
            Contract.Assert(writer != null);

            foreach (IEdmStructuralProperty complexProperty in complexProperties)
            {
                ODataNestedResourceInfo nestedResourceInfo = new ODataNestedResourceInfo
                {
                    IsCollection = complexProperty.Type.IsCollection(),
                    Name = complexProperty.Name
                };

                writer.WriteStart(nestedResourceInfo);
                await WriteComplexAndExpandedNavigationPropertyAsync(complexProperty, null, resourceContext, writer);
                writer.WriteEnd();
            }
        }

        private async Task WriteDynamicComplexPropertiesAsync(ResourceContext resourceContext, ODataWriter writer)
        {
            Contract.Assert(resourceContext != null);
            Contract.Assert(resourceContext.EdmModel != null);

            if (resourceContext.DynamicComplexProperties == null)
            {
                return;
            }

            foreach (var dynamicComplexProperty in resourceContext.DynamicComplexProperties)
            {
                // If the dynamic property is "null", it should be treated ahead by creating an ODataProperty with ODataNullValue.
                // However, it's safety here to skip the null dynamic property.
                if (String.IsNullOrEmpty(dynamicComplexProperty.Key) || dynamicComplexProperty.Value == null)
                {
                    continue;
                }

                IEdmTypeReference edmTypeReference =
                    resourceContext.SerializerContext.GetEdmType(dynamicComplexProperty.Value,
                        dynamicComplexProperty.Value.GetType());

                if (edmTypeReference.IsStructured() ||
                    (edmTypeReference.IsCollection() && edmTypeReference.AsCollection().ElementType().IsStructured()))
                {
                    ODataNestedResourceInfo nestedResourceInfo = new ODataNestedResourceInfo
                    {
                        IsCollection = edmTypeReference.IsCollection(),
                        Name = dynamicComplexProperty.Key,
                    };

                    writer.WriteStart(nestedResourceInfo);
                    await WriteDynamicComplexPropertyAsync(dynamicComplexProperty.Value, edmTypeReference, resourceContext, writer);
                    writer.WriteEnd();
                }
            }
        }

        private async Task WriteDynamicComplexPropertyAsync(object propertyValue, IEdmTypeReference edmType, ResourceContext resourceContext, ODataWriter writer)
        {
            Contract.Assert(resourceContext != null);
            Contract.Assert(writer != null);

            // If the dynamic property is "null", it should be treated ahead by creating an ODataProperty with ODataNullValue.
            Contract.Assert(propertyValue != null);

            // Create the serializer context for the nested and expanded item.
            ODataSerializerContext nestedWriteContext = new ODataSerializerContext(resourceContext, null, null);

            // Write object.
            ODataEdmTypeSerializer serializer = SerializerProvider.GetEdmTypeSerializer(resourceContext.Context, edmType);
            if (serializer == null)
            {
                throw new SerializationException(
                    Error.Format(SRResources.TypeCannotBeSerialized, edmType.ToTraceString(),
                        typeof(ODataResourceSerializer).Name));
            }

            await serializer.WriteObjectInlineAsync(propertyValue, edmType, writer, nestedWriteContext);
        }

        private async Task WriteExpandedNavigationPropertiesAsync(
            IDictionary<IEdmNavigationProperty, SelectExpandClause> navigationPropertiesToExpand,
            ResourceContext resourceContext,
            ODataWriter writer)
        {
            Contract.Assert(navigationPropertiesToExpand != null);
            Contract.Assert(resourceContext != null);
            Contract.Assert(writer != null);

            foreach (KeyValuePair<IEdmNavigationProperty, SelectExpandClause> navPropertyToExpand in navigationPropertiesToExpand)
            {
                IEdmNavigationProperty navigationProperty = navPropertyToExpand.Key;

                ODataNestedResourceInfo navigationLink = await CreateNavigationLinkAsync(navigationProperty, resourceContext);
                if (navigationLink != null)
                {
                    writer.WriteStart(navigationLink);
                    await WriteComplexAndExpandedNavigationPropertyAsync(navPropertyToExpand.Key, navPropertyToExpand.Value, resourceContext, writer);
                    writer.WriteEnd();
                }
            }
        }

        private async Task WriteComplexAndExpandedNavigationPropertyAsync(IEdmProperty edmProperty, SelectExpandClause selectExpandClause,
            ResourceContext resourceContext,
            ODataWriter writer)
        {
            Contract.Assert(edmProperty != null);
            Contract.Assert(resourceContext != null);
            Contract.Assert(writer != null);

            object propertyValue = resourceContext.GetPropertyValue(edmProperty.Name);

            if (propertyValue == null || propertyValue is NullEdmComplexObject)
            {
                if (edmProperty.Type.IsCollection())
                {
                    // A complex or navigation property whose Type attribute specifies a collection, the collection always exists,
                    // it may just be empty.
                    // If a collection of complex or entities can be related, it is represented as a JSON array. An empty
                    // collection of resources (one that contains no resource) is represented as an empty JSON array.
                    writer.WriteStart(new ODataResourceSet
                    {
                        TypeName = edmProperty.Type.FullName()
                    });
                }
                else
                {
                    // If at most one resource can be related, the value is null if no resource is currently related.
                    writer.WriteStart(resource: null);
                }

                writer.WriteEnd();
            }
            else
            {
                // create the serializer context for the complex and expanded item.
                ODataSerializerContext nestedWriteContext = new ODataSerializerContext(resourceContext, selectExpandClause, edmProperty);

                // write object.
                ODataEdmTypeSerializer serializer = SerializerProvider.GetEdmTypeSerializer(resourceContext.Context, edmProperty.Type);
                if (serializer == null)
                {
                    throw new SerializationException(
                        Error.Format(SRResources.TypeCannotBeSerialized, edmProperty.Type.ToTraceString(), typeof(ODataResourceSerializer).Name));
                }

                await serializer.WriteObjectInlineAsync(propertyValue, edmProperty.Type, writer, nestedWriteContext);
            }
        }


        private async Task<IEnumerable<ODataNestedResourceInfo>> CreateNavigationLinksAsync(
            IEnumerable<IEdmNavigationProperty> navigationProperties, ResourceContext resourceContext)
        {
            Contract.Assert(navigationProperties != null);
            Contract.Assert(resourceContext != null);
            var links = new List<ODataNestedResourceInfo>();
            foreach (var navProperty in navigationProperties)
            {
                links.Add(await CreateNavigationLinkAsync(navProperty, resourceContext));
            }
            return links;
            //foreach (IEdmNavigationProperty navProperty in navigationProperties)
            //{
            //    ODataNestedResourceInfo navigationLink = await CreateNavigationLinkAsync(navProperty, resourceContext);
            //    if (navigationLink != null)
            //    {
            //        yield return navigationLink;
            //    }
            //}
        }

        /// <summary>
        /// Creates the <see cref="ODataNestedResourceInfo"/> to be written while writing this entity.
        /// </summary>
        /// <param name="navigationProperty">The navigation property for which the navigation link is being created.</param>
        /// <param name="resourceContext">The context for the entity instance being written.</param>
        /// <returns>The navigation link to be written.</returns>
        public virtual async Task<ODataNestedResourceInfo> CreateNavigationLinkAsync(IEdmNavigationProperty navigationProperty, ResourceContext resourceContext)
        {
            if (navigationProperty == null)
            {
                throw Error.ArgumentNull("navigationProperty");
            }
            if (resourceContext == null)
            {
                throw Error.ArgumentNull("resourceContext");
            }

            ODataSerializerContext writeContext = resourceContext.SerializerContext;
            ODataNestedResourceInfo navigationLink = null;

            if (writeContext.NavigationSource != null)
            {
                IEdmTypeReference propertyType = navigationProperty.Type;
                IEdmModel model = writeContext.Model;
                NavigationSourceLinkBuilderAnnotation linkBuilder = model.GetNavigationSourceLinkBuilder(writeContext.NavigationSource);
                Uri navigationUrl = await linkBuilder.BuildNavigationLinkAsync(resourceContext, navigationProperty, writeContext.MetadataLevel);

                navigationLink = new ODataNestedResourceInfo
                {
                    IsCollection = propertyType.IsCollection(),
                    Name = navigationProperty.Name,
                };

                if (navigationUrl != null)
                {
                    navigationLink.Url = navigationUrl;
                }
            }

            return navigationLink;
        }

        private IEnumerable<ODataProperty> CreateStructuralPropertyBag(
            IEnumerable<IEdmStructuralProperty> structuralProperties, ResourceContext resourceContext)
        {
            Contract.Assert(structuralProperties != null);
            Contract.Assert(resourceContext != null);

            List<ODataProperty> properties = new List<ODataProperty>();
            foreach (IEdmStructuralProperty structuralProperty in structuralProperties)
            {
                ODataProperty property = CreateStructuralProperty(structuralProperty, resourceContext);
                if (property != null)
                {
                    properties.Add(property);
                }
            }

            return properties;
        }

        /// <summary>
        /// Creates the <see cref="ODataProperty"/> to be written for the given entity and the structural property.
        /// </summary>
        /// <param name="structuralProperty">The EDM structural property being written.</param>
        /// <param name="resourceContext">The context for the entity instance being written.</param>
        /// <returns>The <see cref="ODataProperty"/> to write.</returns>
        public virtual ODataProperty CreateStructuralProperty(IEdmStructuralProperty structuralProperty, ResourceContext resourceContext)
        {
            if (structuralProperty == null)
            {
                throw Error.ArgumentNull("structuralProperty");
            }
            if (resourceContext == null)
            {
                throw Error.ArgumentNull("resourceContext");
            }

            ODataSerializerContext writeContext = resourceContext.SerializerContext;

            ODataEdmTypeSerializer serializer = SerializerProvider.GetEdmTypeSerializer(resourceContext.Context, structuralProperty.Type);
            if (serializer == null)
            {
                throw new SerializationException(
                    Error.Format(SRResources.TypeCannotBeSerialized, structuralProperty.Type.FullName(), typeof(ODataOutputFormatter).Name));
            }

            object propertyValue = resourceContext.GetPropertyValue(structuralProperty.Name);

            IEdmTypeReference propertyType = structuralProperty.Type;
            if (propertyValue != null)
            {
                if (!propertyType.IsPrimitive() && !propertyType.IsEnum())
                {
                    IEdmTypeReference actualType = writeContext.GetEdmType(propertyValue, propertyValue.GetType());
                    if (propertyType != null && propertyType != actualType)
                    {
                        propertyType = actualType;
                    }
                }
            }

            return serializer.CreateProperty(propertyValue, propertyType, structuralProperty.Name, writeContext);
        }

        private IEnumerable<ODataAction> CreateODataActions(
            IEnumerable<IEdmAction> actions, ResourceContext resourceContext)
        {
            Contract.Assert(actions != null);
            Contract.Assert(resourceContext != null);

            foreach (IEdmAction action in actions)
            {
                ODataAction oDataAction = CreateODataAction(action, resourceContext);
                if (oDataAction != null)
                {
                    yield return oDataAction;
                }
            }
        }

        private IEnumerable<ODataFunction> CreateODataFunctions(
            IEnumerable<IEdmFunction> functions, ResourceContext resourceContext)
        {
            Contract.Assert(functions != null);
            Contract.Assert(resourceContext != null);

            foreach (IEdmFunction function in functions)
            {
                ODataFunction oDataFunction = CreateODataFunction(function, resourceContext);
                if (oDataFunction != null)
                {
                    yield return oDataFunction;
                }
            }
        }

        /// <summary>
        /// Creates an <see cref="ODataAction" /> to be written for the given action and the entity instance.
        /// </summary>
        /// <param name="action">The OData action.</param>
        /// <param name="resourceContext">The context for the entity instance being written.</param>
        /// <returns>The created action or null if the action should not be written.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2234: Pass System.Uri objects instead of strings", Justification = "This overload is equally good")]
        public virtual ODataAction CreateODataAction(IEdmAction action, ResourceContext resourceContext)
        {
            if (action == null)
            {
                throw Error.ArgumentNull("action");
            }

            if (resourceContext == null)
            {
                throw Error.ArgumentNull("resourceContext");
            }

            IEdmModel model = resourceContext.EdmModel;
            OperationLinkBuilder builder = model.GetOperationLinkBuilder(action);

            if (builder == null)
            {
                return null;
            }

            return CreateODataOperation(action, builder, resourceContext) as ODataAction;
        }

        /// <summary>
        /// Creates an <see cref="ODataFunction" /> to be written for the given action and the entity instance.
        /// </summary>
        /// <param name="function">The OData function.</param>
        /// <param name="resourceContext">The context for the entity instance being written.</param>
        /// <returns>The created function or null if the action should not be written.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2234: Pass System.Uri objects instead of strings",
            Justification = "This overload is equally good")]
        [SuppressMessage("Microsoft.Naming", "CA1716: Use function as parameter name", Justification = "Function")]
        public virtual ODataFunction CreateODataFunction(IEdmFunction function, ResourceContext resourceContext)
        {
            if (function == null)
            {
                throw Error.ArgumentNull("function");
            }

            if (resourceContext == null)
            {
                throw Error.ArgumentNull("resourceContext");
            }

            IEdmModel model = resourceContext.EdmModel;
            OperationLinkBuilder builder = model.GetOperationLinkBuilder(function);

            if (builder == null)
            {
                return null;
            }

            return CreateODataOperation(function, builder, resourceContext) as ODataFunction;
        }

        private static ODataOperation CreateODataOperation(IEdmOperation operation, OperationLinkBuilder builder, ResourceContext resourceContext)
        {
            Contract.Assert(operation != null);
            Contract.Assert(builder != null);
            Contract.Assert(resourceContext != null);

            ODataMetadataLevel metadataLevel = resourceContext.SerializerContext.MetadataLevel;
            IEdmModel model = resourceContext.EdmModel;

            if (ShouldOmitOperation(operation, builder, metadataLevel))
            {
                return null;
            }

            Uri target = builder.BuildLink(resourceContext);
            if (target == null)
            {
                return null;
            }

            Uri baseUri = new Uri(resourceContext.Url.CreateODataLink(MetadataSegment.Instance));
            Uri metadata = new Uri(baseUri, "#" + CreateMetadataFragment(operation));

            ODataOperation odataOperation;
            if (operation is IEdmAction)
            {
                odataOperation = new ODataAction();
            }
            else
            {
                odataOperation = new ODataFunction();
            }
            odataOperation.Metadata = metadata;

            // Always omit the title in minimal/no metadata modes.
            if (metadataLevel == ODataMetadataLevel.FullMetadata)
            {
                EmitTitle(model, operation, odataOperation);
            }

            // Omit the target in minimal/no metadata modes unless it doesn't follow conventions.
            if (!builder.FollowsConventions || metadataLevel == ODataMetadataLevel.FullMetadata)
            {
                odataOperation.Target = target;
            }

            return odataOperation;
        }

        internal static void EmitTitle(IEdmModel model, IEdmOperation operation, ODataOperation odataOperation)
        {
            // The title should only be emitted in full metadata.
            OperationTitleAnnotation titleAnnotation = model.GetOperationTitleAnnotation(operation);
            if (titleAnnotation != null)
            {
                odataOperation.Title = titleAnnotation.Title;
            }
            else
            {
                odataOperation.Title = operation.Name;
            }
        }

        internal static string CreateMetadataFragment(IEdmOperation operation)
        {
            // There can only be one entity container in OData V4.
            string actionName = operation.Name;
            string fragment = operation.Namespace + "." + actionName;

            return fragment;
        }

        private static IEdmStructuredType GetODataPathType(ODataSerializerContext serializerContext)
        {
            Contract.Assert(serializerContext != null);
            if (serializerContext.EdmProperty != null)
            {
                // we are in an nested complex or expanded navigation property.
                if (serializerContext.EdmProperty.Type.IsCollection())
                {
                    return serializerContext.EdmProperty.Type.AsCollection().ElementType().ToStructuredType();
                }
                else
                {
                    return serializerContext.EdmProperty.Type.AsStructured().StructuredDefinition();
                }
            }
            else
            {
                if (serializerContext.ExpandedResource != null)
                {
                    // we are in dynamic complex.
                    return null;
                }

                // figure out the type from the path.
                if (serializerContext.Path != null)
                {
                    IEdmType edmType = serializerContext.Path.EdmType;
                    if (edmType.TypeKind == EdmTypeKind.Collection)
                    {
                        edmType = (edmType as IEdmCollectionType).ElementType.Definition;
                    }

                    return edmType as IEdmStructuredType;
                }

                return null;
            }
        }

        internal static void AddTypeNameAnnotationAsNeeded(ODataResource resource, IEdmStructuredType odataPathType,
            ODataMetadataLevel metadataLevel)
        {
            // ODataLib normally has the caller decide whether or not to serialize properties by leaving properties
            // null when values should not be serialized. The TypeName property is different and should always be
            // provided to ODataLib to enable model validation. A separate annotation is used to decide whether or not
            // to serialize the type name (a null value prevents serialization).

            // Note: In the current version of ODataLib the default behavior likely now matches the requirements for
            // minimal metadata mode. However, there have been behavior changes/bugs there in the past, so the safer
            // option is for this class to take control of type name serialization in minimal metadata mode.

            Contract.Assert(resource != null);

            string typeName = null; // Set null to force the type name not to serialize.

            // Provide the type name to serialize.
            if (!ShouldSuppressTypeNameSerialization(resource, odataPathType, metadataLevel))
            {
                typeName = resource.TypeName;
            }

            resource.TypeAnnotation = new ODataTypeAnnotation(typeName);
        }

        internal static void AddTypeNameAnnotationAsNeededForComplex(ODataResource resource, ODataMetadataLevel metadataLevel)
        {
            // ODataLib normally has the caller decide whether or not to serialize properties by leaving properties
            // null when values should not be serialized. The TypeName property is different and should always be
            // provided to ODataLib to enable model validation. A separate annotation is used to decide whether or not
            // to serialize the type name (a null value prevents serialization).
            Contract.Assert(resource != null);

            // Only add an annotation if we want to override ODataLib's default type name serialization behavior.
            if (ShouldAddTypeNameAnnotationForComplex(metadataLevel))
            {
                string typeName;

                // Provide the type name to serialize (or null to force it not to serialize).
                if (ShouldSuppressTypeNameSerializationForComplex(metadataLevel))
                {
                    typeName = null;
                }
                else
                {
                    typeName = resource.TypeName;
                }

                resource.TypeAnnotation = new ODataTypeAnnotation(typeName);
            }
        }

        internal static bool ShouldAddTypeNameAnnotationForComplex(ODataMetadataLevel metadataLevel)
        {
            switch (metadataLevel)
            {
                // For complex types, the default behavior matches the requirements for minimal metadata mode, so no
                // annotation is necessary.
                case ODataMetadataLevel.MinimalMetadata:
                    return false;
                // In other cases, this class must control the type name serialization behavior.
                case ODataMetadataLevel.FullMetadata:
                case ODataMetadataLevel.NoMetadata:
                default: // All values already specified; just keeping the compiler happy.
                    return true;
            }
        }

        internal static bool ShouldSuppressTypeNameSerializationForComplex(ODataMetadataLevel metadataLevel)
        {
            Contract.Assert(metadataLevel != ODataMetadataLevel.MinimalMetadata);

            switch (metadataLevel)
            {
                case ODataMetadataLevel.NoMetadata:
                    return true;
                case ODataMetadataLevel.FullMetadata:
                default: // All values already specified; just keeping the compiler happy.
                    return false;
            }
        }

        internal static bool ShouldOmitOperation(IEdmOperation operation, OperationLinkBuilder builder,
            ODataMetadataLevel metadataLevel)
        {
            Contract.Assert(builder != null);

            switch (metadataLevel)
            {
                case ODataMetadataLevel.MinimalMetadata:
                case ODataMetadataLevel.NoMetadata:
                    return operation.IsBound && builder.FollowsConventions;

                case ODataMetadataLevel.FullMetadata:
                default: // All values already specified; just keeping the compiler happy.
                    return false;
            }
        }

        internal static bool ShouldSuppressTypeNameSerialization(ODataResource resource, IEdmStructuredType edmType,
            ODataMetadataLevel metadataLevel)
        {
            Contract.Assert(resource != null);

            switch (metadataLevel)
            {
                case ODataMetadataLevel.NoMetadata:
                    return true;
                case ODataMetadataLevel.FullMetadata:
                    return false;
                case ODataMetadataLevel.MinimalMetadata:
                default: // All values already specified; just keeping the compiler happy.
                    string pathTypeName = null;
                    if (edmType != null)
                    {
                        pathTypeName = edmType.FullTypeName();
                    }
                    string resourceTypeName = resource.TypeName;
                    return String.Equals(resourceTypeName, pathTypeName, StringComparison.Ordinal);
            }
        }

        private IEdmStructuredTypeReference GetResourceType(object graph, ODataSerializerContext writeContext)
        {
            Contract.Assert(graph != null);

            IEdmTypeReference edmType = writeContext.GetEdmType(graph, graph.GetType());
            Contract.Assert(edmType != null);

            if (!edmType.IsStructured())
            {
                throw new SerializationException(
                    Error.Format(SRResources.CannotWriteType, GetType().Name, edmType.FullName()));
            }

            return edmType.AsStructured();
        }
    }
}
