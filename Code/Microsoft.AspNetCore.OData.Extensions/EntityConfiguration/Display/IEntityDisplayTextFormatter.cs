using System;
using System.Linq.Expressions;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Display
{
    public interface IEntityDisplayTextFormatter
    {
        Expression FormatterExpression { get; }
        Func<object, string> Format { get; }
    }
}