using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    public class ArraysExtractor
    {
        public ArraysExtractor(List<Dictionary<Type, List<Expression>>> arrays)
        {
            this.arrays = arrays;
        }

        public void GetArrays(Expression value)
        {
            Type type;
            new ArraysExtractorVisitor(arrays, false).GetArrays(value, out type);
        }

        private readonly List<Dictionary<Type, List<Expression>>> arrays;
    }

    public class ArraysExtractorVisitor : ExpressionVisitor
    {
        public ArraysExtractorVisitor(List<Dictionary<Type, List<Expression>>> list, bool unique)
        {
            this.list = list;
            this.unique = unique;
        }

        public int GetArrays(Expression value, out Type type)
        {
            Visit(value);
            type = this.type;
            return maxLevel;
        }

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            foreach(var arg in node.Arguments)
                Visit(arg);
            return node;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if(unique && type != null && type != node.Type)
                throw new InvalidOperationException("Type '" + node.Type + "' is not valid at this point");
            type = node.Type;
            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if(IsArrayReduction(node))
            {
                var chain = node;
                var chainShards = chain.SmashToSmithereens();
                int i, l;
                if (chainShards[0].NodeType == ExpressionType.Call || chainShards[0].NodeType == ExpressionType.Invoke || chainShards[0].NodeType == ExpressionType.Constant)
                {
                    i = chainShards[0].NodeType == ExpressionType.Call || chainShards[0].NodeType == ExpressionType.Invoke ? 0 : 1;
                    Type subType;
                    var subMaxLevel = new ArraysExtractorVisitor(list, true).GetArrays(chainShards[i], out subType);
                    if(unique && type != null && type != subType)
                        throw new InvalidOperationException("Type '" + subType + "' is not valid at this point");
                    type = subType;
                    l = subMaxLevel;
                }
                else
                {
                    if(unique && type != null && type != chainShards[0].Type)
                        throw new InvalidOperationException("Type '" + chainShards[0].Type + "' is not valid at this point");
                    type = chainShards[0].Type;
                    l = 0;
                    i = 0;
                }
                for(++i; i < chainShards.Length; ++i)
                {
                    if(!IsArrayReduction(chainShards[i])) continue;
                    ++l;
                    if(l > maxLevel) maxLevel = l;
                    while(list.Count <= l)
                        list.Add(new Dictionary<Type, List<Expression>>());
                    List<Expression> arrays;
                    if(!list[l].TryGetValue(type, out arrays))
                        list[l].Add(type, arrays = new List<Expression>());
                    arrays.Add(chainShards[i]);
                }
                return node;
            }
            return base.VisitMethodCall(node);
        }

        private static bool IsArrayReduction(Expression node)
        {
            var methodCallExpression = node as MethodCallExpression;
            if(methodCallExpression == null)
                return false;
            var method = methodCallExpression.Method;
            return method.IsEachMethod() || method.IsCurrentMethod();
        }

        private int maxLevel;
        private Type type;
        private readonly List<Dictionary<Type, List<Expression>>> list;
        private readonly bool unique;
    }
}