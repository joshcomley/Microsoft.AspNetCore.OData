using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Brandless.AspNetCore.OData.Extensions.Extensions;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Reports
{
    public class ReportDefinition<TEntity> : IReportDefinition
    {
        public string Title { get; set; }
        private readonly List<ReportField<TEntity>> _fields = new List<ReportField<TEntity>>();

        public IReadOnlyCollection<ReportField<TEntity>> Fields => _fields;

        IReadOnlyCollection<IReportField> IReportDefinition.Fields => Fields;

        public ReportDefinition<TEntity> AddCustomField(
            string title,
            Expression<Func<TEntity, object>> formatter,
            Expression<Func<TEntity, object>> noValueFormatter = null,
            Expression<Func<TEntity, object>> commentFormatter = null,
            ReportFieldKind kind = ReportFieldKind.String,
            ReportFieldStyle style = ReportFieldStyle.Normal,
            Func<TEntity, string> link = null,
            string key = null,
            Action<ReportField<TEntity>> configure = null)
        {
            var reportField = new ReportField<TEntity>(title, formatter, noValueFormatter, commentFormatter, kind, style, link, key);
            configure?.Invoke(reportField);
            _fields.Add(reportField);
            return this;
        }

        public ReportDefinition<TEntity> AddCollectionField<TCollectionElement>(
            Expression<Func<TEntity, IEnumerable<TCollectionElement>>> property,
            Expression<Func<TCollectionElement, object>> formatter,
            Expression<Func<TCollectionElement, object>> noValueFormatter = null,
            Expression<Func<TCollectionElement, object>> commentFormatter = null,
            ReportFieldKind kind = ReportFieldKind.String,
            ReportFieldStyle style = ReportFieldStyle.Normal,
            Func<TCollectionElement, string> link = null,
            string title = null,
            string key = null,
            Action<ReportCollectionField<TEntity, TCollectionElement>> configure = null)
        {
            var reportCollectionField = new ReportCollectionField<TEntity, TCollectionElement>(
                property,
                title, 
                null, 
                null, 
                null, 
                ReportFieldKind.Collection, 
                ReportFieldStyle.Normal, 
                null, 
                key);
            reportCollectionField.CollectionField = 
                new ReportField<TCollectionElement>(title, formatter, noValueFormatter, commentFormatter, kind, style, link, key);
            configure?.Invoke(reportCollectionField);
            _fields.Add(reportCollectionField);
            return this;
        }

        public ReportDefinition<TEntity> AddField<TProperty>(
            Expression<Func<TEntity, TProperty>> property,
            string title = null,
            Expression<Func<TEntity, object>> noValueFormatter = null,
            Expression<Func<TEntity, object>> commentFormatter = null,
            ReportFieldKind kind = ReportFieldKind.String,
            ReportFieldStyle style = ReportFieldStyle.Normal,
            Func<TEntity, string> link = null,
            string key = null,
            Action<ReportField<TEntity>> configure = null)
        {
            var propertyInfo = property.GetAccessedProperty();
            return AddCustomField(
                title ?? propertyInfo.Name.IntelliSpace(),
                BuildLambda(property),
                noValueFormatter,
                commentFormatter,
                kind,
                style,
                link,
                key ?? title ?? propertyInfo.Name,
                configure);
        }

        private static Expression<Func<TEntity, object>> BuildLambda<TProperty>(Expression<Func<TEntity, TProperty>> property)
        {
            var propertyInfo = property.GetAccessedProperty();
            var param = Expression.Parameter(typeof(TEntity), "_");
            Expression propertyAccessor = Expression.PropertyOrField(param, propertyInfo.Name);
            //var toString = propertyInfo.PropertyType.GetMethods().First(m => m.Name == nameof(ToString) && m.GetParameters().Length == 0);
            //if (propertyInfo.PropertyType.IsClass || typeof(string).IsAssignableFrom(propertyInfo.PropertyType) || Nullable.GetUnderlyingType(propertyInfo.PropertyType) != null)
            //{
            //    var prop = propertyAccessor;
            //    prop = Expression.Call(prop, toString);
            //    propertyAccessor =
            //        Expression.Condition(
            //            Expression.Equal(Expression.Constant(null), propertyAccessor),
            //            Expression.Constant(""),
            //            prop);
            //}
            //else
            //{
            //    propertyAccessor = Expression.Call(propertyAccessor, toString);
            //}
            var lambda = Expression.Lambda<Func<TEntity, object>>(Expression.Convert(propertyAccessor, typeof(object)), param);
            return lambda;
        }

        public ReportDefinition<TEntity> RemoveField(string key)
        {
            _fields.RemoveAll(f => f.Key == key);
            return this;
        }

        public ReportDefinition<TEntity> RemoveFields(Predicate<ReportField<TEntity>> filter)
        {
            _fields.RemoveAll(filter);
            return this;
        }

        public ReportDefinition<TEntity> RemoveField(Expression<Func<TEntity, object>> property)
        {
            var propertyInfo = property.GetAccessedProperty();
            _fields.RemoveAll(f => f.Key == propertyInfo.Name);
            return this;
        }
    }
}