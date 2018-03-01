using System;
using System.Linq.Expressions;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Validation
{
    public class EntityValidation<TEntity> : IEntityValidation
    {
        private Func<TEntity, bool> _validationFunction;
        public string Message { get; }
        public string Key { get; }
        public Expression<Func<TEntity, bool>> ValidationExpression { get; }

        public Func<TEntity, bool> Validate 
        {
            get
            {
                if (_validationFunction != null)
                {
                    return _validationFunction;
                }

                var fn = ValidationExpression.Compile();
                _validationFunction = _ => !fn(_);
                return _validationFunction;
            }
        }

        Expression IEntityValidation.ValidationExpression => ValidationExpression;
        Func<object, bool> IEntityValidation.Validate => obj => Validate((TEntity)obj);

        public EntityValidation(Expression<Func<TEntity, bool>> validationExpression, string key, string message)
        {
            ValidationExpression = validationExpression;
            Key = key;
            Message = message;
        }
    }
}