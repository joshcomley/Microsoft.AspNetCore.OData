using System;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;

namespace Brandless.AspNetCore.OData.Extensions.Extensions
{
    public static class LambdaExtensions
    {
        public static PropertyInfo GetAccessedProperty(this LambdaExpression expression)
        {
            MemberExpression exp;

            //this line is necessary, because sometimes the expression comes in as Convert(originalexpression)
            if (expression.Body is UnaryExpression)
            {
                var unExp = (UnaryExpression)expression.Body;
                if (unExp.Operand is MemberExpression)
                {
                    exp = (MemberExpression)unExp.Operand;
                }
                else
                    throw new ArgumentException();
            }
            else if (expression.Body is MemberExpression)
            {
                exp = (MemberExpression)expression.Body;
            }
            else
            {
                throw new ArgumentException();
            }

            return (PropertyInfo)exp.Member;
        }
    }
}