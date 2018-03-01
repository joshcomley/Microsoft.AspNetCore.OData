using System;
using System.Linq.Expressions;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Display
{
    public class EntityDisplayTextFormatter<TEntity> : IEntityDisplayTextFormatter
    {
        private Func<TEntity, string> _formatterFunction;
        public Expression<Func<TEntity, string>> FormatterExpression { get; }

        public Func<TEntity, string> Format => _formatterFunction ?? (_formatterFunction = FormatterExpression.Compile());

        Expression IEntityDisplayTextFormatter.FormatterExpression => FormatterExpression;
        Func<object, string> IEntityDisplayTextFormatter.Format => obj => Format((TEntity)obj);

        public EntityDisplayTextFormatter(Expression<Func<TEntity, string>> formatterExpression)
        {
            FormatterExpression = formatterExpression;
        }
    }
}