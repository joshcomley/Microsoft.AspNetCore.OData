using System;
using System.Linq.Expressions;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Reports
{
    public class DynamicReportDefinition<T> : ReportDefinition<T>
    {
        public DynamicReportDefinition(ModelConfiguration modelConfiguration)
        {
            var entity = modelConfiguration.ForEntityType<T>();
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                if (property.PropertyType.IsValueType || property.PropertyType == typeof(string))
                {
                    var param = Expression.Parameter(typeof(T), "_");
                    Expression propertyAccessor = Expression.PropertyOrField(param, property.Name);
                    var lambda = Expression.Lambda<Func<T, object>>(Expression.Convert(propertyAccessor, typeof(object)), param);
                    AddCustomField(property.Name, lambda);
                }
                //var field = Fields.Last();
                //var propertyConfig = entity.PropertyMetadata(property.Name);
                //if (propertyConfig != null)
                //{

                //}
            }
        }
    }
}