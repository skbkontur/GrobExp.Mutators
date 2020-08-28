using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.Visitors;

using JetBrains.Annotations;

namespace GrobExp.Mutators.ModelConfiguration.Traverse
{
    internal class ModelConfigurationTreeTraveler
    {
        private ModelConfigurationTreeTraveler(bool createPath, [CanBeNull] ModelConfigurationNode subRoot)
        {
            this.createPath = createPath;
            this.subRoot = subRoot;
            SubRootIsVisited = false;
            ArrayAliases = new List<KeyValuePair<Expression, Expression>>();
        }

        /// <summary>
        ///     Traverses the <paramref name="path" /> from <paramref name="node" />, creating missing nodes if <paramref name="create" /> is true.<br />
        ///     <returns>
        ///         Child - resulting node <br />
        ///         ArrayAliases - list of array aliases <br />
        ///         SubRootIsVisited - true if <paramref name="subRoot" /> was visited during traverse
        ///     </returns>
        /// </summary>
        [NotNull]
        public static TraverseResult Traverse([NotNull] ModelConfigurationNode node, [NotNull] Expression path, [CanBeNull] ModelConfigurationNode subRoot, bool create)
        {
            var traveler = new ModelConfigurationTreeTraveler(create, subRoot);
            var child = traveler.Traverse(node, path);
            return new TraverseResult
                {
                    Child = child,
                    ArrayAliases = traveler.ArrayAliases,
                    SubRootIsVisited = traveler.SubRootIsVisited,
                };
        }

        [CanBeNull]
        private ModelConfigurationNode Traverse([NotNull] ModelConfigurationNode node, [NotNull] Expression path)
        {
            var child = TraverseInternal(node, path);
            SubRootIsVisited |= child == subRoot;

            return child;
        }

        [CanBeNull]
        private ModelConfigurationNode TraverseInternal([NotNull] ModelConfigurationNode node, [NotNull] Expression path)
        {
            switch (path)
            {
            case ParameterExpression _:
                {
                    if (path.Type != node.NodeType)
                    {
                        // Shit happened
                        return null;
                    }

                    return node;
                }
            case UnaryExpression unaryExpression:
                {
                    switch (unaryExpression.NodeType)
                    {
                    case ExpressionType.ArrayLength:
                        return Traverse(node, unaryExpression.Operand)?.GotoMember(ModelConfigurationEdge.ArrayLengthProperty, createPath);

                    case ExpressionType.Convert:
                        return Traverse(node, unaryExpression.Operand)?.GotoTypeConversion(unaryExpression.Type, createPath);
                    }

                    break;
                }
            case MemberExpression memberExpression:
                {
                    return Traverse(node, memberExpression.Expression)?.GotoMember(memberExpression.Member, createPath);
                }
            case BinaryExpression binaryExpression:
                {
                    if (binaryExpression.NodeType != ExpressionType.ArrayIndex)
                        break;
                    return GotoArrayIndexOrEachElement(Traverse(node, binaryExpression.Left), binaryExpression.Right);
                }
            case MethodCallExpression methodCallExpression:
                {
                    var method = methodCallExpression.Method;

                    if (method.IsEachMethod() || method.IsCurrentMethod())
                    {
                        return Traverse(node, methodCallExpression.Arguments[0])?.GotoEachArrayElement(createPath);
                    }

                    if (method.IsArrayIndexer())
                    {
                        if (methodCallExpression.Object == null)
                            throw new InvalidOperationException("Method call object is null for GetValue method");

                        return GotoArrayIndexOrEachElement(Traverse(node, methodCallExpression.Object), methodCallExpression.Arguments[0]);
                    }

                    if (method.IsIndexerGetter())
                    {
                        if (methodCallExpression.Object == null)
                            throw new InvalidOperationException("Method call object is null for indexer method");

                        return GotoIndexerOrEachElement(Traverse(node, methodCallExpression.Object), methodCallExpression.Arguments.ToArray());
                    }

                    throw new NotSupportedException("Method " + method + " is not supported");
                }
            }
            throw new NotSupportedException("Node type " + path.NodeType + " is not supported");
        }

        [CanBeNull]
        private ModelConfigurationNode GotoArrayIndexOrEachElement([CanBeNull] ModelConfigurationNode child, [NotNull] Expression index)
        {
            if (child == null)
                return null;
            var newChild = child.GotoArrayElement(GetIndex(index), createPath);
            if (newChild != null)
                return newChild;

            // If no child exists and create:false create array alias for migration and go by 'Each' edge
            var array = GetArrayMigrationAlias(child);
            if (array != null)
            {
                ArrayAliases.Add(new KeyValuePair<Expression, Expression>(
                                     Expression.ArrayIndex(array, index),
                                     array.MakeEachCall()));
            }

            return child.GotoEachArrayElement(create : false);
        }

        [CanBeNull]
        private ModelConfigurationNode GotoIndexerOrEachElement([CanBeNull] ModelConfigurationNode child, [NotNull] Expression[] indexerArguments)
        {
            if (child == null)
                return null;

            var indexerParameters = indexerArguments.Select(exp => ((ConstantExpression)exp).Value).ToArray();
            // Try go by 'indexer' edge
            var newChild = child.GotoIndexer(indexerParameters, createPath);
            if (newChild != null)
                return newChild;

            // If no child exists and create:false go by 'each' edge and create array alias for migration
            newChild = child.GotoEachArrayElement(create : false);
            if (newChild == null)
                return null;

            var array = GetArrayMigrationAlias(child);
            if (array != null)
            {
                ArrayAliases.Add(new KeyValuePair<Expression, Expression>(
                                     Expression.Call(array, array.Type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), indexerArguments),
                                     Expression.Property(array.MakeEachCall(), "Value")));
            }

            return newChild.GotoMember(newChild.NodeType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance), create : false);
        }

        [CanBeNull]
        private static Expression GetArrayMigrationAlias([NotNull] ModelConfigurationNode node)
        {
            var arrays = node.GetArrays();
            var childPath = new ExpressionWrapper(node.Path, strictly : false);

            return arrays.Values.FirstOrDefault(arrayPath => !childPath.Equals(new ExpressionWrapper(arrayPath, strictly : false)));
        }

        private static int GetIndex([NotNull] Expression exp)
        {
            // todo использовать ExpressionCompiler
            if (exp.NodeType == ExpressionType.Constant)
                return (int)((ConstantExpression)exp).Value;
            return Expression.Lambda<Func<int>>(Expression.Convert(exp, typeof(int))).Compile()();
        }

        public bool SubRootIsVisited { get; private set; }

        public List<KeyValuePair<Expression, Expression>> ArrayAliases { get; }

        private readonly ModelConfigurationNode subRoot;
        private readonly bool createPath;
    }
}