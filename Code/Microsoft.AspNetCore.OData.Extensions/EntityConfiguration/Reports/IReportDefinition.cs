using System.Collections.Generic;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Reports
{
    public interface IReportDefinition
    {
        string Title { get; set; }
        IReadOnlyCollection<IReportField> Fields { get; }
    }
}