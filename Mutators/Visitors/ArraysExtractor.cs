using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    internal class ArraysExtractor
    {
        public ArraysExtractor(List<Dictionary<Type, List<Expression>>> arrays)
        {
            this.arrays = arrays;
        }

        public void GetArrays(Expression value)
        {
            Type type;
            new ArraysExtractorVisitor(arrays, paramTypeMustBeUnique : false).GetArrays(value, out type);
        }

        private readonly List<Dictionary<Type, List<Expression>>> arrays;
    }

    /// <summary>
    ///     Находит в выражении все массивы, отмеченные вызовами Each() или Current(),
    ///     и сохраняет их, группируя по уровню вложенности (количество Each() и Current() в соответствующем выражении)
    ///     и типу корня выражения.
    /// </summary>
    /// <returns>Максимальный уровень вложенности вызовов Each() и Current()</returns>
    internal class ArraysExtractorVisitor : ExpressionVisitor
    {
        public ArraysExtractorVisitor(List<Dictionary<Type, List<Expression>>> list, bool paramTypeMustBeUnique)
        {
            this.list = list;
            this.paramTypeMustBeUnique = paramTypeMustBeUnique;
        }

        public int GetArrays(Expression value, out Type type)
        {
            Visit(value);
            type = rootParamType;
            return maxLevel;
        }

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            foreach (var arg in node.Arguments)
                Visit(arg);
            return node;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (paramTypeMustBeUnique && rootParamType != null && rootParamType != node.Type)
                throw new InvalidOperationException("Type '" + node.Type + "' is not valid at this point");
            rootParamType = node.Type;
            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (!IsEachOrCurrentCall(node)) 
                return base.VisitMethodCall(node);
            var chain = node;
            var chainShards = chain.SmashToSmithereens();
            int i, l;
            if (chainShards[0].NodeType == ExpressionType.Call || chainShards[0].NodeType == ExpressionType.Invoke || chainShards[0].NodeType == ExpressionType.Constant)
            {
                i = chainShards[0].NodeType == ExpressionType.Call || chainShards[0].NodeType == ExpressionType.Invoke ? 0 : 1;
                var subMaxLevel = new ArraysExtractorVisitor(list, paramTypeMustBeUnique : true).GetArrays(chainShards[i], out var subType);
                if (paramTypeMustBeUnique && rootParamType != null && rootParamType != subType)
                    throw new InvalidOperationException("Type '" + subType + "' is not valid at this point");
                rootParamType = subType;
                l = subMaxLevel;
            }
            else
            {
                // ExpressionType.Parameter ???
                if (paramTypeMustBeUnique && rootParamType != null && rootParamType != chainShards[0].Type)
                    throw new InvalidOperationException("Type '" + chainShards[0].Type + "' is not valid at this point");
                rootParamType = chainShards[0].Type;
                l = 0;
                i = 0;
            }

            for (++i; i < chainShards.Length; ++i)
            {
                if (!IsEachOrCurrentCall(chainShards[i])) continue;
                ++l;
                if (l > maxLevel)
                    maxLevel = l;
                while (list.Count <= l)
                    list.Add(new Dictionary<Type, List<Expression>>());
                if (!list[l].TryGetValue(rootParamType, out var arrays))
                    list[l].Add(rootParamType, arrays = new List<Expression>());
                arrays.Add(chainShards[i]);
            }

            return node;
        }

        private static bool IsEachOrCurrentCall(Expression node)
        {
            if (!(node is MethodCallExpression methodCallExpression))
                return false;
            var method = methodCallExpression.Method;
            return method.IsEachMethod() || method.IsCurrentMethod();
        }

        private int maxLevel;
        private Type rootParamType;
        private readonly List<Dictionary<Type, List<Expression>>> list;
        private readonly bool paramTypeMustBeUnique;
    }
}