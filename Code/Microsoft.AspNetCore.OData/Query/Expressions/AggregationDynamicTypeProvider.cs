﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser.Aggregation;

namespace Microsoft.AspNetCore.OData.Query.Expressions
{
    /// <summary>
    /// Factory for dynamic types in aggregation result
    /// </summary>
    /// <remarks>
    /// Implemented as "skyhook" so far. Need to look for DI in WebAPI
    /// </remarks>
    internal class AggregationDynamicTypeProvider
    {
        private static readonly MethodInfo getPropertyValueMethod = typeof(AggregationWrapper).GetMethod("GetPropertyValue");
        private static readonly MethodInfo setPropertyValueMethod = typeof(AggregationWrapper).GetMethod("SetPropertyValue");

        private const string ModuleName = "MainModule";
        private const string DynamicTypeName = "DynamicTypeWrapper";

        /// <summary>
        /// Generates type by provided definition.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="assemblyProvider"></param>
        /// <param name="propertyNodes"></param>
        /// <param name="expressions"></param>
        /// <returns></returns>
        /// <remarks>
        /// We create new assembly each time, but they will be collected by GC.
        /// Current performance testing results is 0.5ms per type. We should consider caching types, however trade off is between CPU perfomance and memory usage (might be it will we an option for library user)
        /// </remarks>
        public static Type GetResultType<T>(
            IEdmModel model,
            IAssemblyProvider assemblyProvider,
            IEnumerable<GroupByPropertyNode> propertyNodes = null,
            IEnumerable<AggregateExpression> expressions = null) where T : AggregationWrapper
        {
            Contract.Assert(model != null);

            // Do not have properties, just return base class
            if ((expressions == null || !expressions.Any()) && (propertyNodes == null || !propertyNodes.Any()))
            {
                return typeof(T);
            }

            TypeBuilder tb = GetTypeBuilder<T>(DynamicTypeName);
            if (expressions != null && expressions.Any())
            {
                foreach (var field in expressions)
                {
                    if (field.TypeReference.Definition.TypeKind == EdmTypeKind.Primitive)
                    {
                        var primitiveType = EdmLibHelpers.GetClrType(field.TypeReference, model, assemblyProvider);
                        CreateProperty(tb, field.Alias, primitiveType);
                    }
                }
            }

            if (propertyNodes != null && propertyNodes.Any())
            {
                foreach (var field in propertyNodes)
                {
                    if (field.Expression != null && field.TypeReference.Definition.TypeKind == EdmTypeKind.Primitive)
                    {
                        var primitiveType = EdmLibHelpers.GetClrType(field.TypeReference, model, assemblyProvider);
                        CreateProperty(tb, field.Name, primitiveType);
                    }
                    else
                    {
                        var complexProp = GetResultType<AggregationWrapper>(model, assemblyProvider, field.ChildTransformations);
                        CreateProperty(tb, field.Name, complexProp);
                    }
                }
            }

            var typeInfo = tb.CreateTypeInfo();
            return typeInfo.AsType();
        }

        private static TypeBuilder GetTypeBuilder<T>(string typeSignature) where T : DynamicTypeWrapper
        {
            var an = new AssemblyName(typeSignature);

            // Create GC collectable assembly. It will be collected after usage and we don't need to worry about memmory usage
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(ModuleName);
            TypeBuilder tb = moduleBuilder.DefineType(typeSignature,
                                TypeAttributes.Public |
                                TypeAttributes.Class |
                                TypeAttributes.AutoClass |
                                TypeAttributes.AnsiClass |
                                TypeAttributes.BeforeFieldInit |
                                TypeAttributes.AutoLayout,
                                typeof(T));
            return tb;
        }

        private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType)
        {
            PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);

            // Property get method
            // get
            // {
            //  return (propertyType)this.GetPropertyValue("propertyName");
            // }
            MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, null/*Type.EmptyTypes*/);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldstr, propertyName);
            getIl.Emit(OpCodes.Callvirt, getPropertyValueMethod);

            var propertyTypeInfo = propertyType.GetTypeInfo();
            if (propertyTypeInfo.IsValueType)
            {
                // for value type (type) means unboxing
                getIl.Emit(OpCodes.Unbox_Any, propertyType);
            }
            else
            {
                // for ref types (type) means cast
                getIl.Emit(OpCodes.Castclass, propertyType);
            }

            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr =
                tb.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldstr, propertyName);
            setIl.Emit(OpCodes.Ldarg_1);
            if (propertyTypeInfo.IsValueType)
            {
                // Boxing value types to store as an object
                setIl.Emit(OpCodes.Box, propertyType);
            }

            setIl.Emit(OpCodes.Callvirt, setPropertyValueMethod);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }
    }
}
