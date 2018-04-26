﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData.Common;
using Microsoft.OData.Edm;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Microsoft.AspNetCore.OData.Builder;
using Microsoft.AspNetCore.OData.Formatter;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNetCore.OData.Extensions
{
    public static class EdmModelExtensions
    {
        /// <summary>
        /// Gets the <see cref="EntitySetLinkBuilderAnnotation"/> to be used while generating self and navigation links for the given entity set.
        /// </summary>
        /// <param name="model">The <see cref="IEdmModel"/> containing the entity set.</param>
        /// <param name="entitySet">The entity set.</param>
        /// <returns>The <see cref="EntitySetLinkBuilderAnnotation"/> if set for the given the entity set; otherwise, a new 
        /// <see cref="EntitySetLinkBuilderAnnotation"/> that generates URLs that follow OData URL conventions.</returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "IEdmEntitySet is more relevant here.")]
        public static NavigationSourceLinkBuilderAnnotation GetEntitySetLinkBuilder(this IEdmModel model, IEdmEntitySet entitySet)
        {
            if (model == null)
            {
                throw Error.ArgumentNull("model");
            }

            NavigationSourceLinkBuilderAnnotation annotation = model.GetAnnotationValue<NavigationSourceLinkBuilderAnnotation>(entitySet);
            if (annotation == null)
            {
                // construct and set an entity set link builder that follows OData URL conventions.
                annotation = new NavigationSourceLinkBuilderAnnotation(entitySet, model);
                model.SetEntitySetLinkBuilder(entitySet, annotation);
            }

            return annotation;
        }

        /// <summary>
        /// Sets the <see cref="EntitySetLinkBuilderAnnotation"/> to be used while generating self and navigation links for the given entity set.
        /// </summary>
        /// <param name="model">The <see cref="IEdmModel"/> containing the entity set.</param>
        /// <param name="entitySet">The entity set.</param>
        /// <param name="entitySetLinkBuilder">The <see cref="EntitySetLinkBuilderAnnotation"/> to set.</param>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "IEdmEntitySet is more relevant here.")]
        public static void SetEntitySetLinkBuilder(this IEdmModel model, IEdmEntitySet entitySet, NavigationSourceLinkBuilderAnnotation entitySetLinkBuilder)
        {
            if (model == null)
            {
                throw Error.ArgumentNull("model");
            }

            model.SetAnnotationValue(entitySet, entitySetLinkBuilder);
        }

        public static bool HasProperty<T, TPropertyType>(this IEdmModel model,
            Expression<Func<T, TPropertyType>> propertyExpression)
        {
            return model.HasProperty(PropertySelectorVisitor.GetSelectedProperty(propertyExpression));
        }

        public static IEdmProperty GetProperty<T, TPropertyType>(this IEdmModel model,
            Expression<Func<T, TPropertyType>> propertyExpression)
        {
            return model.GetProperty(PropertySelectorVisitor.GetSelectedProperty(propertyExpression));
        }

        public static bool HasProperty(this IEdmModel model, PropertyInfo propertyInfo)
        {
            return model.GetProperty(propertyInfo) != null;
        }

        public static bool HasProperty(this IEdmModel model, Type type, string propertyName)
        {
            return model.GetProperty(type, propertyName) != null;
            ;
        }

        public static IEdmProperty GetProperty(this IEdmModel model, Type type, string propertyName)
        {
            var entityType = model.GetEdmType(type) as EdmEntityType;
            var edmProperty = entityType?.Properties().SingleOrDefault(p =>
                    p.Name == propertyName
                );
            return edmProperty;
        }

        public static IEdmProperty GetProperty(this IEdmModel model, PropertyInfo propertyInfo)
        {
            var entityType = model.GetEdmType(propertyInfo.DeclaringType) as EdmEntityType;
            var edmProperty = entityType?.Properties().SingleOrDefault(p =>
                    p.Name == propertyInfo.Name &&
                    p.Type.Definition.IsEquivalentTo(model.GetEdmType(propertyInfo.PropertyType))
                );
            return edmProperty;
        }

        public static IEdmType GetEdmType(this IEdmModel model, Type clrType)
        {
            if (model == null)
            {
                throw Error.ArgumentNull("model");
            }

            return model.FindDeclaredType(clrType.EdmFullName());
        }

        /// <summary>
        /// Gets the <see cref="OperationLinkBuilder"/> to be used while generating operation links for the given action.
        /// </summary>
        /// <param name="model">The <see cref="IEdmModel"/> containing the operation.</param>
        /// <param name="operation">The operation for which the link builder is needed.</param>
        /// <returns>The <see cref="OperationLinkBuilder"/> for the given operation if one is set; otherwise, a new
        /// <see cref="OperationLinkBuilder"/> that generates operation links following OData URL conventions.</returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters",
            Justification = "IEdmActionImport is more relevant here.")]
        public static OperationLinkBuilder GetOperationLinkBuilder(this IEdmModel model, IEdmOperation operation)
        {
            if (model == null)
            {
                throw Error.ArgumentNull("model");
            }
            if (operation == null)
            {
                throw Error.ArgumentNull("operation");
            }

            OperationLinkBuilder linkBuilder = model.GetAnnotationValue<OperationLinkBuilder>(operation);
            if (linkBuilder == null)
            {
                linkBuilder = GetDefaultOperationLinkBuilder(operation);
                model.SetOperationLinkBuilder(operation, linkBuilder);
            }

            return linkBuilder;
        }

        /// <summary>
        /// Sets the <see cref="OperationLinkBuilder"/> to be used for generating the OData operation link for the given operation.
        /// </summary>
        /// <param name="model">The <see cref="IEdmModel"/> containing the entity set.</param>
        /// <param name="operation">The operation for which the operation link is to be generated.</param>
        /// <param name="operationLinkBuilder">The <see cref="OperationLinkBuilder"/> to set.</param>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters",
            Justification = "IEdmActionImport is more relevant here.")]
        public static void SetOperationLinkBuilder(this IEdmModel model, IEdmOperation operation, OperationLinkBuilder operationLinkBuilder)
        {
            if (model == null)
            {
                throw Error.ArgumentNull("model");
            }

            model.SetAnnotationValue(operation, operationLinkBuilder);
        }

        /// <summary>
        /// Gets the <see cref="NavigationSourceLinkBuilderAnnotation"/> to be used while generating self and navigation
        /// links for the given navigation source.
        /// </summary>
        /// <param name="model">The <see cref="IEdmModel"/> containing the navigation source.</param>
        /// <param name="navigationSource">The navigation source.</param>
        /// <returns>The <see cref="NavigationSourceLinkBuilderAnnotation"/> if set for the given the singleton; otherwise,
        /// a new <see cref="NavigationSourceLinkBuilderAnnotation"/> that generates URLs that follow OData URL conventions.
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters",
            Justification = "IEdmNavigationSource is more relevant here.")]
        public static NavigationSourceLinkBuilderAnnotation GetNavigationSourceLinkBuilder(this IEdmModel model,
            IEdmNavigationSource navigationSource)
        {
            if (model == null)
            {
                throw Error.ArgumentNull("model");
            }

            NavigationSourceLinkBuilderAnnotation annotation = model
                .GetAnnotationValue<NavigationSourceLinkBuilderAnnotation>(navigationSource);
            if (annotation == null)
            {
                // construct and set a navigation source link builder that follows OData URL conventions.
                annotation = new NavigationSourceLinkBuilderAnnotation(navigationSource, model);
                model.SetNavigationSourceLinkBuilder(navigationSource, annotation);
            }

            return annotation;
        }

        /// <summary>
        /// Sets the <see cref="NavigationSourceLinkBuilderAnnotation"/> to be used while generating self and navigation
        /// links for the given navigation source.
        /// </summary>
        /// <param name="model">The <see cref="IEdmModel"/> containing the navigation source.</param>
        /// <param name="navigationSource">The navigation source.</param>
        /// <param name="navigationSourceLinkBuilder">The <see cref="NavigationSourceLinkBuilderAnnotation"/> to set.</param>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters",
            Justification = "IEdmNavigationSource is more relevant here.")]
        public static void SetNavigationSourceLinkBuilder(this IEdmModel model, IEdmNavigationSource navigationSource,
            NavigationSourceLinkBuilderAnnotation navigationSourceLinkBuilder)
        {
            if (model == null)
            {
                throw Error.ArgumentNull("model");
            }

            model.SetAnnotationValue(navigationSource, navigationSourceLinkBuilder);
        }

        internal static ClrTypeCache GetTypeMappingCache(this IEdmModel model)
        {
            Contract.Assert(model != null);

            ClrTypeCache typeMappingCache = model.GetAnnotationValue<ClrTypeCache>(model);
            if (typeMappingCache == null)
            {
                typeMappingCache = new ClrTypeCache();
                model.SetAnnotationValue(model, typeMappingCache);
            }

            return typeMappingCache;
        }

        internal static OperationTitleAnnotation GetOperationTitleAnnotation(this IEdmModel model, IEdmOperation action)
        {
            Contract.Assert(model != null);
            return model.GetAnnotationValue<OperationTitleAnnotation>(action);
        }

        internal static void SetOperationTitleAnnotation(this IEdmModel model, IEdmOperation action, OperationTitleAnnotation title)
        {
            Contract.Assert(model != null);
            model.SetAnnotationValue(action, title);
        }

        private static OperationLinkBuilder GetDefaultOperationLinkBuilder(IEdmOperation operation)
        {
            OperationLinkBuilder linkBuilder = null;
            if (operation.Parameters != null)
            {
                if (operation.Parameters.First().Type.IsEntity())
                {
                    if (operation is IEdmAction)
                    {
                        linkBuilder = new OperationLinkBuilder(
                            (ResourceContext resourceContext) =>
                                resourceContext.GenerateActionLink(operation), followsConventions: true);
                    }
                    else
                    {
                        linkBuilder = new OperationLinkBuilder(
                            (ResourceContext resourceContext) =>
                                resourceContext.GenerateFunctionLink(operation), followsConventions: true);
                    }
                }
                else if (operation.Parameters.First().Type.IsCollection())
                {
                    if (operation is IEdmAction)
                    {
                        linkBuilder =
                            new OperationLinkBuilder(
                                (ResourceSetContext reseourceSetContext) =>
                                    reseourceSetContext.GenerateActionLink(operation), followsConventions: true);
                    }
                    else
                    {
                        linkBuilder =
                            new OperationLinkBuilder(
                                (ResourceSetContext reseourceSetContext) =>
                                    reseourceSetContext.GenerateFunctionLink(operation), followsConventions: true);
                    }
                }
            }
            return linkBuilder;
        }
    }
}