using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.OData.Query.Expressions
{
    internal class InterceptorContainer
    {
        private readonly List<object> _interceptors;
        private readonly MethodInfo _interceptMethod;

        public InterceptorContainer(Type clrType, IServiceProvider serviceProvider)
        {
            var interceptorType = typeof(IODataQueryInterceptor<>).MakeGenericType(clrType);
            _interceptors = serviceProvider
                .GetServices(interceptorType)
                .ToList();
            _interceptMethod =
                interceptorType
                    .GetMethod(nameof(IODataQueryInterceptor<string>.Intercept))
                ;
        }

        public bool Any => _interceptors?.Any() ?? false;

        public void ForEach(Expression source, Action<Expression, LambdaExpression> onIntercept)
        {
            foreach (var interceptor in _interceptors)
            {
                var predicate = (LambdaExpression)_interceptMethod.Invoke(interceptor, new object[]
                {
                    null,
                    null,
                    null
                });
                onIntercept(source, predicate);
            }
        }
    }
}