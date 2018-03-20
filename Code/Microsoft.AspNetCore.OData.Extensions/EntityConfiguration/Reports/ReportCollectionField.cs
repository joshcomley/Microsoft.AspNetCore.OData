using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Brandless.AspNetCore.OData.Extensions.Extensions;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Reports
{
    public class ReportCollectionField<TEntity, TCollectionElement> : ReportField<TEntity>, IReportCollectionField
    {
        private Expression<Func<TEntity, IEnumerable<TCollectionElement>>> _propertyExpression;
        private Func<object, IEnumerable> _propertyAccessor;

        public ReportCollectionField(
            Expression<Func<TEntity, IEnumerable<TCollectionElement>>> propertyExpression,
            string title, Expression<Func<TEntity, object>> formatter,
            Expression<Func<TEntity, object>> noValueFormatter = null,
            Expression<Func<TEntity, object>> commentFormatter = null, ReportFieldKind kind = ReportFieldKind.String,
            ReportFieldStyle style = ReportFieldStyle.Normal, Func<TEntity, string> link = null, string key = null) :
            base(title, formatter, noValueFormatter, commentFormatter, kind, style, link, key)
        {
            PropertyExpression = propertyExpression;
            Property = propertyExpression.GetAccessedProperty();
        }

        public Func<TEntity, IEnumerable<TCollectionElement>> PropertyAccessor { get; private set; }

        Func<object, IEnumerable> IReportCollectionField.PropertyAccessor => _propertyAccessor;

        public Expression<Func<TEntity, IEnumerable<TCollectionElement>>> PropertyExpression
        {
            get => _propertyExpression;
            set
            {
                _propertyExpression = value;
                if (value == null)
                {
                    PropertyAccessor = null;
                    _propertyAccessor = null;
                }
                else
                {
                    PropertyAccessor = _propertyExpression.Compile();
                    _propertyAccessor = _ => PropertyAccessor((TEntity)_);
                }
            }
        }

        public ReportField<TCollectionElement> CollectionField { get; set; }

        IReportField IReportCollectionField.CollectionField => CollectionField;

        public PropertyInfo Property { get; set; }
    }
}