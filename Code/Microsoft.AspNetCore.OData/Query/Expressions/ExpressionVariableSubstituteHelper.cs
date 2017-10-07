using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.OData.Query.Expressions
{
    public static class ExpressionVariableSubstituteHelper
    {
        public static LambdaExpression
            Substitute
            (
                this LambdaExpression mainExpression,
                string varToSubstName, 
                Expression substExpression
            )
        {
            ParameterExpression peToSubst = (from pe in mainExpression.Parameters
                                             where pe.Name == varToSubstName
                                             select pe).FirstOrDefault();

            if (peToSubst == null)
            {
                throw new Exception
                (
                    String.Format("Could not find input parameter \"{0}\" in expression \"{1}\"",
                                    varToSubstName,
                                    mainExpression.ToString())
                );
            }

            //if (substExpression.ReturnType != peToSubst.Type)
            //{
            //    throw new Exception
            //    (
            //        String.Format
            //        (
            //            "The substitute expression return type \"{0}\" does not match the type of the substituted variable \"{1}:{2}\"",
            //            substExpression.ReturnType,
            //            varToSubstName,
            //            peToSubst.Type
            //        )
            //    );
            //}

            //List<ParameterExpression> pars = new List<ParameterExpression>();

            //pars.AddRange(mainExpression.Parameters);

            //int idxToSubst = pars.IndexOf(peToSubst);

            //pars.RemoveAt(idxToSubst);

            //foreach (ParameterExpression substPe in substExpression.Parameters)
            //{
            //    int numParamsOfTheSameNameInMainExpression =
            //        (from pe in pars
            //         where pe.Name == substPe.Name
            //         select pe).Count();

            //    if (numParamsOfTheSameNameInMainExpression == 0)
            //    {
            //        pars.Insert(idxToSubst, substPe);
            //        idxToSubst++;
            //        continue;
            //    }

            //    throw new Exception
            //    (
            //        String.Format
            //        (
            //            "Input parameter of name \"{0}\" already exists in the main expression",
            //            substPe.Name
            //        )
            //    );
            //}

            LambdaExpression modifiedMain =
                Expression.Lambda
                (
                    mainExpression,
                    null
                );

            SubstExpressionVisitor visitor =
                new SubstExpressionVisitor
                {
                    SubstExpression = substExpression,
                    ParamExpressionToSubstitute = peToSubst
                };

            LambdaExpression substResult = (LambdaExpression)visitor.Visit(mainExpression);

            return substResult;
        }
    }
}
