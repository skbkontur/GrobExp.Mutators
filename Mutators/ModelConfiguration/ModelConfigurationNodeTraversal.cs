using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.ModelConfiguration
{
    public static class ModelConfigurationNodeTraversal
    {
        public static ModelConfigurationNode Traverse(this ModelConfigurationNode node, Expression path, bool create)
        {
            return node.Traverse(path, create, out _);
        }

        internal static ModelConfigurationNode Traverse(this ModelConfigurationNode node, Expression path, bool create, out List<KeyValuePair<Expression, Expression>> arrayAliases)
        {
            arrayAliases = new List<KeyValuePair<Expression, Expression>>();
            node.Traverse(path, null, out var result, create, arrayAliases);
            return result;
        }

        internal static bool Traverse(this ModelConfigurationNode node, Expression path, ModelConfigurationNode subRoot, out ModelConfigurationNode child, bool create)
        {
            return node.Traverse(path, subRoot, out child, create, new List<KeyValuePair<Expression, Expression>>());
        }

        internal static ModelConfigurationNode GotoEachArrayElement(this ModelConfigurationNode node, bool create)
        {
            return node.GetChild(ModelConfigurationEdge.Each, node.NodeType.GetItemType(), create);
        }

        internal static ModelConfigurationNode GotoMember(this ModelConfigurationNode node, MemberInfo member, bool create)
        {
            var edge = new ModelConfigurationEdge(member);
            switch (member.MemberType)
            {
            case MemberTypes.Field:
                return node.GetChild(edge, ((FieldInfo)member).FieldType, create);
            case MemberTypes.Property:
                return node.GetChild(edge, ((PropertyInfo)member).PropertyType, create);
            default:
                throw new NotSupportedException("Member rootType " + member.MemberType + " is not supported");
            }
        }

        /// <summary>
        ///     Traverses the <paramref name="path" /> from this node, creating missing nodes if <paramref name="create" /> is true. <br />
        ///     At the end, <paramref name="child" /> points to the last traversed node and <paramref name="arrayAliases" /> contains some shit.
        /// </summary>
        /// <returns>
        ///     True, if <paramref name="subRoot" /> node was visited while traversing from root node by <paramref name="path" />
        /// </returns>
        private static bool Traverse(this ModelConfigurationNode theNode, Expression path, ModelConfigurationNode subRoot, out ModelConfigurationNode child, bool create, List<KeyValuePair<Expression, Expression>> arrayAliases)
        {
            switch (path.NodeType)
            {
            case ExpressionType.Parameter:
                {
                    //Check if we have reached root of the tree
                    if (path.Type != theNode.NodeType)
                    {
                        // Shit happened
                        child = null;
                        return false;
                    }

                    // Ok, it's root
                    child = theNode;
                    return subRoot == theNode;
                }
            case ExpressionType.Convert:
                {
                    // Traverse to the operand's node and go by 'convertation' edge.
                    var unaryExpression = (UnaryExpression)path;
                    var result = theNode.Traverse(unaryExpression.Operand, subRoot, out child, create, arrayAliases);
                    child = child == null ? null : child.GotoConvertation(unaryExpression.Type, create);
                    return result || child == subRoot;
                }
            case ExpressionType.MemberAccess:
                {
                    // Traverse to the node of containing object and go by 'member-access' edge
                    var memberExpression = (MemberExpression)path;
                    var result = theNode.Traverse(memberExpression.Expression, subRoot, out child, create, arrayAliases);
                    child = child == null ? null : child.GotoMember(memberExpression.Member, create);
                    return result || child == subRoot;
                }
            case ExpressionType.ArrayIndex:
                {
                    var binaryExpression = (BinaryExpression)path;
                    // Traverse to the array's node
                    var result = theNode.Traverse(binaryExpression.Left, subRoot, out child, create, arrayAliases);
                    if (child != null)
                    {
                        // Try go by 'array-index' edge
                        var newChild = child.GotoArrayElement(GetIndex(binaryExpression.Right), create);
                        // If no child exists and create:false go by 'each' edge and create array alias for migration
                        if (newChild == null)
                        {
                            newChild = child.GotoEachArrayElement(create : false);
                            var array = GetArrayMigrationAlias(child);
                            if (array != null)
                            {
                                arrayAliases.Add(new KeyValuePair<Expression, Expression>(
                                                     Expression.ArrayIndex(array, binaryExpression.Right),
                                                     array.MakeEachCall())
                                    );
                            }
                        }

                        child = newChild;
                    }

                    return result || child == subRoot;
                }
            case ExpressionType.Call:
                {
                    var methodCallExpression = (MethodCallExpression)path;
                    var method = methodCallExpression.Method;
                    if (method.IsEachMethod() || method.IsCurrentMethod())
                    {
                        // Traverse to the array's node which is in Arguments[0], because Each() and Current() are extension methods, and go by 'each' edge
                        var result = theNode.Traverse(methodCallExpression.Arguments[0], subRoot, out child, create, arrayAliases);
                        child = child == null ? null : child.GotoEachArrayElement(create);
                        return result || child == subRoot;
                    }

                    if (method.IsArrayIndexer())
                    {
                        // Traverse to the array's node which is Object of method call
                        var result = theNode.Traverse(methodCallExpression.Object, subRoot, out child, create, arrayAliases);
                        if (child != null)
                        {
                            // Try go by 'array-index' edge
                            var newChild = child.GotoArrayElement(GetIndex(methodCallExpression.Arguments[0]), create);
                            // If no child exists and create:false go by 'each' edge and create array alias for migration
                            if (newChild == null)
                            {
                                newChild = child.GotoEachArrayElement(false);
                                var array = GetArrayMigrationAlias(child);
                                if (array != null)
                                {
                                    arrayAliases.Add(new KeyValuePair<Expression, Expression>(
                                                         Expression.ArrayIndex(array, methodCallExpression.Arguments[0]),
                                                         array.MakeEachCall())
                                        );
                                }
                            }

                            child = newChild;
                        }

                        return result || child == subRoot;
                    }

                    if (method.IsIndexerGetter())
                    {
                        // Traverse to the object's node which is Object of method call
                        var result = theNode.Traverse(methodCallExpression.Object, subRoot, out child, create, arrayAliases);
                        if (child != null)
                        {
                            var parameters = methodCallExpression.Arguments.Select(exp => ((ConstantExpression)exp).Value).ToArray();
                            // Try go by 'indexer' edge
                            var newChild = child.GotoIndexer(parameters, create);
                            // If no child exists and create:false go by 'each' edge and create array alias for migration
                            if (newChild == null)
                            {
                                newChild = child.GotoEachArrayElement(false);
                                if (newChild != null)
                                {
                                    newChild = newChild.GotoMember(newChild.NodeType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance), false);
                                    var array = GetArrayMigrationAlias(child);
                                    if (array != null)
                                    {
                                        arrayAliases.Add(new KeyValuePair<Expression, Expression>(
                                                             Expression.Call(array, array.Type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), methodCallExpression.Arguments),
                                                             Expression.Property(array.MakeEachCall(), "Value"))
                                            );
                                    }
                                }
                            }

                            child = newChild;
                        }

                        return result || child == subRoot;
                    }

                    throw new NotSupportedException("Method " + method + " is not supported");
                }
            case ExpressionType.ArrayLength:
                {
                    // Traverse to the array's node and go by 'length' edge

                    //                    if(create)
                    //                        throw new NotSupportedException("Node type " + path.NodeType + " is not supported");
                    var unaryExpression = (UnaryExpression)path;
                    var result = theNode.Traverse(unaryExpression.Operand, subRoot, out child, create, arrayAliases);
                    child = child == null ? null : child.GotoMember(ModelConfigurationEdge.ArrayLengthProperty, create);
                    return result || child == subRoot;
                }
            default:
                throw new NotSupportedException("Node type " + path.NodeType + " is not supported");
            }
        }

        private static Expression GetArrayMigrationAlias(ModelConfigurationNode node)
        {
            var arrays = node.GetArrays();
            var childPath = new ExpressionWrapper(node.Path, strictly : false);
            return arrays.Values.FirstOrDefault(arrayPath => !childPath.Equals(new ExpressionWrapper(arrayPath, strictly : false)));
        }

        private static int GetIndex(Expression exp)
        {
            // todo использовать ExpressionCompiler
            if (exp.NodeType == ExpressionType.Constant)
                return (int)((ConstantExpression)exp).Value;
            return Expression.Lambda<Func<int>>(Expression.Convert(exp, typeof(int))).Compile()();
        }

        private static ModelConfigurationNode GotoConvertation(this ModelConfigurationNode node, Type type, bool create)
        {
            var edge = new ModelConfigurationEdge(type);
            return node.GetChild(edge, type, create);
        }

        private static ModelConfigurationNode GotoIndexer(this ModelConfigurationNode node, object[] parameters, bool create)
        {
            var edge = new ModelConfigurationEdge(parameters);
            var property = node.NodeType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                throw new InvalidOperationException("Type '" + node.NodeType + "' doesn't contain indexer");
            if (!property.CanRead || !property.CanWrite)
                throw new InvalidOperationException("Type '" + node.NodeType + "' has indexer that doesn't contain either getter or setter");
            return node.GetChild(edge, property.GetGetMethod().ReturnType, create);
        }

        private static ModelConfigurationNode GotoArrayElement(this ModelConfigurationNode node, int index, bool create)
        {
            return node.GetChild(new ModelConfigurationEdge(index), node.NodeType.GetItemType(), create);
        }

        private static ModelConfigurationNode GetChild(this ModelConfigurationNode node, ModelConfigurationEdge edge, Type childType, bool create)
        {
            if (!node.children.TryGetValue(edge, out var child) && create)
            {
                Expression path;
                if (edge.IsArrayIndex)
                    path = node.Path.MakeArrayIndex((int)edge.Value);
                else if (edge.IsMemberAccess)
                    path = node.Path.MakeMemberAccess((MemberInfo)edge.Value);
                else if (edge.IsEachMethod)
                    path = node.Path.MakeEachCall(childType);
                else if (edge.IsConvertation)
                    path = node.Path.MakeConvertation((Type)edge.Value);
                else if (edge.IsIndexerParams)
                    path = node.Path.MakeIndexerCall((object[])edge.Value, node.NodeType);
                else throw new InvalidOperationException();

                child = new ModelConfigurationNode(node.ConfiguratorType, node.RootType, childType, node.Root, node, edge, path);
                node.children.Add(edge, child);
            }

            return child;
        }
    }
}