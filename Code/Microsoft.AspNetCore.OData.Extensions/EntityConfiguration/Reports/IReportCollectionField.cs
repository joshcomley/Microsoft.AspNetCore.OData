using System;
using System.Collections;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Reports
{
    public interface IReportCollectionField
    {
        Func<object, IEnumerable> PropertyAccessor { get; }
        IReportField CollectionField { get; }
    }
}