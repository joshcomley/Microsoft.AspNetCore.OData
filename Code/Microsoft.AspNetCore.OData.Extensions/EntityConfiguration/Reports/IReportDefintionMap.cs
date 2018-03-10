using System.Collections.Generic;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Reports
{
    public interface IReportDefintionMap
    {
        IReadOnlyCollection<IReportDefinition> Reports { get; }
    }
}