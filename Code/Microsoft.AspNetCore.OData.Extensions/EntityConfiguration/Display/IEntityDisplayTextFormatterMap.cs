using System.Linq.Expressions;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Display
{
    public interface IEntityDisplayTextFormatterMap
    {
        bool Has(string key);
        void Remove(string key);
        IEntityDisplayTextFormatter Get(string key);
        IEntityDisplayTextFormatter Default { get; }
        void Set(Expression formatterExpression, string key);
    }
}