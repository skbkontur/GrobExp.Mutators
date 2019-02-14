using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.AutoEvaluators;
using GrobExp.Mutators.ModelConfiguration;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public interface IModelConfigurationNode
    {
        Type ConfiguratorType { get; }

        ModelConfigurationNode Traverse(Expression path, bool create);

        void AddMutatorSmart(LambdaExpression path, MutatorConfiguration mutator);

        void AddMutator(MutatorConfiguration mutator);

        void AddMutator(Expression path, MutatorConfiguration mutator);
    }

    public class ModelConfigurationNode : IModelConfigurationNode
    {
        internal ModelConfigurationNode(Type configuratorType, Type rootType, Type nodeType, ModelConfigurationNode root, ModelConfigurationNode parent, ModelConfigurationEdge edge, Expression path)
        {
            ConfiguratorType = configuratorType;
            RootType = rootType;
            NodeType = nodeType;
            Root = root ?? this;
            Parent = parent;
            Edge = edge;
            Path = path;
            mutators = new List<KeyValuePair<Expression, MutatorConfiguration>>();
            children = new Dictionary<ModelConfigurationEdge, ModelConfigurationNode>();
        }

        public void AddMutatorSmart(LambdaExpression path, MutatorConfiguration mutator)
        {
            path = (LambdaExpression)path.Simplify();
            var simplifiedPath = PathSimplifier.SimplifyPath(path, out var filter);
            mutator = mutator.ResolveAliases(ExpressionAliaser.CreateAliasesResolver(simplifiedPath.Body, path.Body));
            Traverse(simplifiedPath.Body, true).AddMutator(path.Body, filter == null ? mutator : mutator.If(filter));
        }

        public void AddMutator(MutatorConfiguration mutator)
        {
            if (mutator.IsUncoditionalSetter())
            {
                for (var i = 0; i < Mutators.Count; ++i)
                {
                    if (mutators[i].Value.IsUncoditionalSetter() && ExpressionEquivalenceChecker.Equivalent(Path, mutators[i].Key, false, false))
                    {
                        mutators[i] = new KeyValuePair<Expression, MutatorConfiguration>(Path, mutator);
                        return;
                    }
                }
            }

            mutators.Add(new KeyValuePair<Expression, MutatorConfiguration>(Path, mutator));
        }

        public void AddMutator(Expression path, MutatorConfiguration mutator)
        {
            if (mutator.IsUncoditionalSetter())
            {
                for (var i = 0; i < mutators.Count; ++i)
                {
                    if (mutators[i].Value.IsUncoditionalSetter() && ExpressionEquivalenceChecker.Equivalent(path, mutators[i].Key, false, false))
                    {
                        mutators[i] = new KeyValuePair<Expression, MutatorConfiguration>(path, mutator);
                        return;
                    }
                }
            }

            mutators.Add(new KeyValuePair<Expression, MutatorConfiguration>(path, mutator));
        }

        public static ModelConfigurationNode CreateRoot(Type configuratorType, Type type)
        {
            return new ModelConfigurationNode(configuratorType, type, type, null, null, null, Expression.Parameter(type, type.Name));
        }

        public override string ToString()
        {
            return this.ToPrettyString();
        }

        public ModelConfigurationNode Traverse(Expression path, bool create)
        {
            return Traverse(path, create, out _);
        }

        internal ModelConfigurationNode Traverse(Expression path, bool create, out List<KeyValuePair<Expression, Expression>> arrayAliases)
        {
            arrayAliases = new List<KeyValuePair<Expression, Expression>>();
            Traverse(path, null, out var result, create, arrayAliases);
            return result;
        }

        internal bool Traverse(Expression path, ModelConfigurationNode subRoot, out ModelConfigurationNode child, bool create)
        {
            return Traverse(path, subRoot, out child, create, new List<KeyValuePair<Expression, Expression>>());
        }

        internal ModelConfigurationNode GotoEachArrayElement(bool create)
        {
            return GetChild(ModelConfigurationEdge.Each, NodeType.GetItemType(), create);
        }

        internal ModelConfigurationNode GotoMember(MemberInfo member, bool create)
        {
            var edge = new ModelConfigurationEdge(member);
            switch (member.MemberType)
            {
            case MemberTypes.Field:
                return GetChild(edge, ((FieldInfo)member).FieldType, create);
            case MemberTypes.Property:
                return GetChild(edge, ((PropertyInfo)member).PropertyType, create);
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
        private bool Traverse(Expression path, ModelConfigurationNode subRoot, out ModelConfigurationNode child, bool create, List<KeyValuePair<Expression, Expression>> arrayAliases)
        {
            switch (path.NodeType)
            {
            case ExpressionType.Parameter:
                {
                    //Check if we have reached root of the tree
                    if (path.Type != NodeType)
                    {
                        // Shit happened
                        child = null;
                        return false;
                    }

                    // Ok, it's root
                    child = this;
                    return subRoot == this;
                }
            case ExpressionType.Convert:
                {
                    // Traverse to the operand's node and go by 'convertation' edge.
                    var unaryExpression = (UnaryExpression)path;
                    var result = Traverse(unaryExpression.Operand, subRoot, out child, create, arrayAliases);
                    child = child == null ? null : child.GotoConvertation(unaryExpression.Type, create);
                    return result || child == subRoot;
                }
            case ExpressionType.MemberAccess:
                {
                    // Traverse to the node of containing object and go by 'member-access' edge
                    var memberExpression = (MemberExpression)path;
                    var result = Traverse(memberExpression.Expression, subRoot, out child, create, arrayAliases);
                    child = child == null ? null : child.GotoMember(memberExpression.Member, create);
                    return result || child == subRoot;
                }
            case ExpressionType.ArrayIndex:
                {
                    var binaryExpression = (BinaryExpression)path;
                    // Traverse to the array's node
                    var result = Traverse(binaryExpression.Left, subRoot, out child, create, arrayAliases);
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
                        var result = Traverse(methodCallExpression.Arguments[0], subRoot, out child, create, arrayAliases);
                        child = child == null ? null : child.GotoEachArrayElement(create);
                        return result || child == subRoot;
                    }

                    if (method.IsArrayIndexer())
                    {
                        // Traverse to the array's node which is Object of method call
                        var result = Traverse(methodCallExpression.Object, subRoot, out child, create, arrayAliases);
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
                        var result = Traverse(methodCallExpression.Object, subRoot, out child, create, arrayAliases);
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
                    var result = Traverse(unaryExpression.Operand, subRoot, out child, create, arrayAliases);
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

        private ModelConfigurationNode GotoConvertation(Type type, bool create)
        {
            var edge = new ModelConfigurationEdge(type);
            return GetChild(edge, type, create);
        }

        private ModelConfigurationNode GotoIndexer(object[] parameters, bool create)
        {
            var edge = new ModelConfigurationEdge(parameters);
            var property = NodeType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                throw new InvalidOperationException("Type '" + NodeType + "' doesn't contain indexer");
            if (!property.CanRead || !property.CanWrite)
                throw new InvalidOperationException("Type '" + NodeType + "' has indexer that doesn't contain either getter or setter");
            return GetChild(edge, property.GetGetMethod().ReturnType, create);
        }

        private ModelConfigurationNode GotoArrayElement(int index, bool create)
        {
            return GetChild(new ModelConfigurationEdge(index), NodeType.GetItemType(), create);
        }

        private ModelConfigurationNode GetChild(ModelConfigurationEdge edge, Type childType, bool create)
        {
            if (!children.TryGetValue(edge, out var child) && create)
            {
                Expression path;
                if (edge.IsArrayIndex)
                    path = Path.MakeArrayIndex((int)edge.Value);
                else if (edge.IsMemberAccess)
                    path = Path.MakeMemberAccess((MemberInfo)edge.Value);
                else if (edge.IsEachMethod)
                    path = Path.MakeEachCall(childType);
                else if (edge.IsConvertation)
                    path = Path.MakeConvertation((Type)edge.Value);
                else if (edge.IsIndexerParams)
                    path = Path.MakeIndexerCall((object[])edge.Value, NodeType);
                else throw new InvalidOperationException();

                child = new ModelConfigurationNode(ConfiguratorType, RootType, childType, Root, this, edge, path);
                children.Add(edge, child);
            }

            return child;
        }

        public Expression Path { get; }

        public Type NodeType { get; }

        internal IEnumerable<ModelConfigurationNode> Children => children.Values;
        internal ICollection<KeyValuePair<Expression, MutatorConfiguration>> Mutators => mutators;
        public Type ConfiguratorType { get; }
        internal Type RootType { get; }
        internal ModelConfigurationNode Root { get; }
        internal ModelConfigurationNode Parent { get; }
        internal ModelConfigurationEdge Edge { get; }

        internal readonly List<KeyValuePair<Expression, MutatorConfiguration>> mutators;
        internal readonly Dictionary<ModelConfigurationEdge, ModelConfigurationNode> children;
    }
}