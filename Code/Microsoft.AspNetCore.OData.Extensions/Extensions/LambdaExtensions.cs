using System;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNetCore.OData.Extensions.Extensions
{
    public static class LambdaExtensions
    {
        public static PropertyInfo GetAccessedProperty<T>(this Expression<Func<T, object>> expression)
        {
            Contract.Assert(expression != null);

            var memberNode = expression.Body as MemberExpression;
            if (memberNode == null)
            {
                throw new ArgumentException("Expression does not refer to a property.");
            }
            PropertyInfo propertyInfo = memberNode.Member as PropertyInfo;
            if (propertyInfo == null)
            {
                throw new ArgumentException("Member expression must be a property.");
                    //Error.InvalidOperation(SRResources.MemberExpressionsMustBeProperties,
                    //memberNode.Member.DeclaringType.FullName, memberNode.Member.Name);
            }

            // Ensure we get the top-most version of this PropertyInfo
            propertyInfo = memberNode.Expression.Type.GetProperty(propertyInfo.Name);
            if (memberNode.Expression.NodeType != ExpressionType.Parameter)
            {
                throw new ArgumentException("Member expression must be bound to a lambda parameter.");
                //throw Error.InvalidOperation(SRResources.MemberExpressionsMustBeBoundToLambdaParameter);
            }

            return propertyInfo;
        }
    }
}