using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.OData.Query.Expressions
{
    public class SubstExpressionVisitor : ExpressionVisitor
    {
        public ParameterExpression ParamExpressionToSubstitute
        {
            private get;
            set;
        }

        public Expression SubstExpression
        {
            private get;
            set;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.Operand == ParamExpressionToSubstitute)
            {
                return Expression.MakeUnary(node.NodeType, SubstExpression, node.Type);
            }

            return base.VisitUnary(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            int numArgsToSubstituteAmongMethodArgs = 
                (from expr in node.Arguments
                 where (expr == ParamExpressionToSubstitute)
                 select expr).Count();

            if (numArgsToSubstituteAmongMethodArgs == 0)
            {
                return base.VisitMethodCall(node);
            }

            List<Expression> arguments = new List<Expression>();

            foreach (Expression arg in node.Arguments)
            {
                if (arg == ParamExpressionToSubstitute)
                {
                    arguments.Add(SubstExpression);
                }
                else
                {
                    arguments.Add(Visit(arg));
                }
            }

            return Expression.Call(node.Object, node.Method, arguments);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            Expression left = null, right = null;
            bool substLeft = false;
            bool substRight = false;

            if (node.Left == ParamExpressionToSubstitute)
            {
                left = SubstExpression;
                substLeft = true;
            }
            else
                left = node.Left;

            if (node.Right == ParamExpressionToSubstitute)
            {
                right = SubstExpression;
                substRight = true;
            }
            else
                right = node.Right;

            if (substLeft || substRight)
            {
                if (!substLeft)
                {
                    left = Visit(left);
                }

                if (!substRight)
                {
                    right = Visit(right);
                }

                return Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
            }

            return base.VisitBinary(node);
        }
    }
}
