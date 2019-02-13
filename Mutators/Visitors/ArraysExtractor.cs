using System;
using System.Collections.Generic;
using System.Linq;
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
            new ArraysExtractorVisitor(arrays, paramTypeMustBeUnique : false).GetArrays(value);
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
        public ArraysExtractorVisitor(List<Dictionary<Type, List<Expression>>> arrayLevels, bool paramTypeMustBeUnique)
        {
            this.arrayLevels = arrayLevels;
            this.paramTypeMustBeUnique = paramTypeMustBeUnique;
        }

        public (int Level, Type SubType) GetArrays(Expression value)
        {
            Visit(value);
            return (maxLevel, rootParamType);
        }

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            foreach (var arg in node.Arguments)
                Visit(arg);
            return node;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            SaveParameterType(node.Type);
            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (!IsEachOrCurrentCall(node))
                return base.VisitMethodCall(node);

            var smithereens = node.SmashToSmithereens();
            if (smithereens[0].NodeType == ExpressionType.Constant)
                smithereens = smithereens.Skip(1).ToArray();

            var level = ProcessFirstShard(smithereens[0]);

            foreach (var shard in smithereens.Skip(1).Where(IsEachOrCurrentCall))
            {
                maxLevel = Math.Max(maxLevel, ++level);
                AddShardToLevel(shard, level);
            }

            return node;
        }

        private int ProcessFirstShard(Expression shard)
        {
            var (level, subType) = new ArraysExtractorVisitor(arrayLevels, paramTypeMustBeUnique : true).GetArrays(shard);
            SaveParameterType(subType);
            return level;
        }

        private void AddShardToLevel(Expression shard, int level)
        {
            while (arrayLevels.Count <= level)
                arrayLevels.Add(new Dictionary<Type, List<Expression>>());
            if (!arrayLevels[level].TryGetValue(rootParamType, out var arrays))
                arrayLevels[level].Add(rootParamType, arrays = new List<Expression>());
            arrays.Add(shard);
        }

        private void SaveParameterType(Type subType)
        {
            if (paramTypeMustBeUnique && rootParamType != null && rootParamType != subType)
                throw new InvalidOperationException("Type '" + subType + "' is not valid at this point");
            rootParamType = subType;
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
        private readonly List<Dictionary<Type, List<Expression>>> arrayLevels;
        private readonly bool paramTypeMustBeUnique;
    }
}