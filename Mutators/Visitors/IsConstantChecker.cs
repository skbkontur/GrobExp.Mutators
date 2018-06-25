using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

namespace GrobExp.Mutators.Visitors
{
    public class IsConstantChecker : ExpressionVisitor
    {
        /// <summary>
        ///     An expression is considered not Constant if:
        ///     <list type="bullet">
        ///         <item>It references external parameters</item>
        ///         <item>It contains a call to DateTime.<see cref="DateTime.UtcNow" /></item>
        ///         <item>It contains a call to Guid.<see cref="Guid.NewGuid" /></item>
        ///         <item>It contains a call to <see cref="MutatorsHelperFunctions.Dynamic{T}" /></item>
        ///     </list>
        /// </summary>
        public bool IsConstant([NotNull] Expression exp)
        {
            Visit(exp);
            return isConstant;
        }

        protected override Expression VisitLambda<T>(Expression<T> lambda)
        {
            parameters.AddRange(lambda.Parameters);
            var res = base.VisitLambda(lambda);
            parameters.RemoveRange(parameters.Count - lambda.Parameters.Count, lambda.Parameters.Count);
            return res;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member == utcNowMember)
                isConstant = false;
            return base.VisitMember(node);
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            if (!parameters.Contains(p))
                isConstant = false;
            return base.VisitParameter(p);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.IsDynamicMethod() || node.Method == guidNewGuidMethod)
                isConstant = false;
            return base.VisitMethodCall(node);
        }

        private bool isConstant = true;
        private readonly List<ParameterExpression> parameters = new List<ParameterExpression>();

        private static readonly MemberInfo utcNowMember = ((MemberExpression)((Expression<Func<DateTime>>)(() => DateTime.UtcNow)).Body).Member;
        private static readonly MethodInfo guidNewGuidMethod = ((MethodCallExpression)((Expression<Func<Guid>>)(() => Guid.NewGuid())).Body).Method;
    }
}