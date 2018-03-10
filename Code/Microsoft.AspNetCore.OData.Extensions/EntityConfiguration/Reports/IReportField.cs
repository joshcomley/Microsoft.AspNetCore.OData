using System;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Reports
{
    public interface IReportField
    {
        string Title { get; set; }
        string Key { get; set; }
        Func<object, object> CommentFormatter { get; }
        Func<object, object> NoValueFormatter { get; }
        Func<object, object> Formatter { get; }
        Func<object, string> Link { get; }
        ReportFieldKind Kind { get; set; }
        ReportFieldStyle Style { get; set; }
    }
}