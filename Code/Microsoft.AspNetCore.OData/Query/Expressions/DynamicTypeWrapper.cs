﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.OData.Query.Expressions
{
    /// <summary>
    /// Represents a container class that contains properties that are grouped by using $apply.
    /// </summary>
    public abstract class DynamicTypeWrapper
    {
        /// <summary>
        /// Gets values stored in the wrapper
        /// </summary>
        public abstract Dictionary<string, object> Values { get; }

        /// <summary>
        /// Attempts to get the value of the Property called <paramref name="propertyName"/> from the underlying Entity.
        /// </summary>
        /// <param name="propertyName">The name of the Property</param>
        /// <param name="value">The new value of the Property</param>
        /// <returns>True if successful</returns>
        [SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate", Justification = "Generics not appropriate here")]
        public bool TryGetPropertyValue(string propertyName, out object value)
        {
            return this.Values.TryGetValue(propertyName, out value);
        }

        /// <summary>
        /// Get property value.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public object GetPropertyValue(string propertyName)
        {
            return this.Values[propertyName];
        }

        /// <summary>
        /// Set property value
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        public void SetPropertyValue(string propertyName, object value)
        {
            this.Values[propertyName] = value;
        }
    }

    [JsonConverter(typeof(DynamicTypeWrapperConverter))]
    public class GroupByWrapper : DynamicTypeWrapper
    {
        private Dictionary<string, object> _values
        {
            get => _values1;
            set => _values1 = value;
        }

        protected static readonly IPropertyMapper DefaultPropertyMapper = new IdentityPropertyMapper();
        private Dictionary<string, object> _values1;

        /// <summary>
        /// Gets or sets the property container that contains the properties being expanded. 
        /// </summary>
        public virtual AggregationPropertyContainer GroupByContainer { get; set; }

        /// <summary>
        /// Gets or sets the property container that contains the properties being expanded. 
        /// </summary>
        public virtual AggregationPropertyContainer Container { get; set; }

        public override Dictionary<string, object> Values
        {
            get
            {
                EnsureValues();
                return this._values;
            }
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var compareWith = obj as GroupByWrapper;
            if (compareWith == null)
            {
                return false;
            }
            var dictionary1 = this.Values;
            var dictionary2 = compareWith.Values;
            return dictionary1.Count() == dictionary2.Count() && !dictionary1.Except(dictionary2).Any();
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            EnsureValues();
            long hash = 1870403278L; //Arbitrary number from Anonymous Type GetHashCode implementation
            foreach (var v in this.Values.Values)
            {
                hash = (hash * -1521134295L) + (v == null ? 0 : v.GetHashCode());
            }

            return (int)hash;
        }

        private void EnsureValues()
        {
            if (_values == null)
            {
                if (this.GroupByContainer != null)
                {
                    this._values = this.GroupByContainer.ToDictionary(DefaultPropertyMapper);
                }
                else
                {
                    this._values = new Dictionary<string, object>();
                }

                if (this.Container != null)
                {
                    _values = _values.Concat(this.Container.ToDictionary(DefaultPropertyMapper)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }
            }
        }
    }

    public class NoGroupByWrapper : GroupByWrapper
    {
    }

    public class AggregationWrapper : GroupByWrapper
    {
    }

    public class NoGroupByAggregationWrapper : GroupByWrapper
    {
    }
}
