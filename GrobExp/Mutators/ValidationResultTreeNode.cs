using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

using GrEmit;
using GrEmit.Utils;

using GrobExp.Compiler;

namespace GrobExp.Mutators
{
    public static class ValidationResultTreeNodeBuilder
    {
        public static Func<ValidationResultTreeNode, ValidationResultTreeNode> BuildFactory(Type type, bool buildType)
        {
            type = buildType ? BuildType(type) : type;
            var result = (Func<ValidationResultTreeNode, ValidationResultTreeNode>)factories[type];
            if(result == null)
            {
                lock(factoriesLock)
                {
                    result = (Func<ValidationResultTreeNode, ValidationResultTreeNode>)factories[type];
                    if(result == null)
                        factories[type] = result = BuildFactoryInternal(type);
                }
            }
            return result;
        }

        public static Type BuildType(Type type)
        {
            return BuildType(type, false);
        }

        private static Type BuildType(Type type, bool returnBeingBuilt)
        {
            var result = (Type)types[type];
            if(result == null)
            {
                lock(typesLock)
                {
                    result = (Type)types[type];
                    var typeBeingBuilt = (Type)typesBeingBuilt[type];
                    if(result == null && returnBeingBuilt && typeBeingBuilt != null)
                        return typeBeingBuilt;
                    types[type] = result = BuildTypeInternal(type);
                }
            }
            return result;
        }

        private static Func<ValidationResultTreeNode, ValidationResultTreeNode> BuildFactoryInternal(Type type)
        {
            var parameterTypes = new [] {typeof(ValidationResultTreeNode)};
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(ValidationResultTreeNode), parameterTypes, typeof(string), true);
            using(var il = new GroboIL(method))
            {
                var constructor = type.GetConstructor(parameterTypes);
                if(constructor == null)
                    throw new InvalidOperationException(string.Format("The type '{0}' has no constructor accepting one parameter of type '{1}'", type, typeof(ValidationResultTreeNode)));
                il.Ldarg(0);
                il.Newobj(constructor);
                il.Ret();
            }
            return (Func<ValidationResultTreeNode, ValidationResultTreeNode>)method.CreateDelegate(typeof(Func<ValidationResultTreeNode, ValidationResultTreeNode>));
        }

        private static Type BuildTypeInternal(Type type)
        {
            var parentType = typeof(ValidationResultTreeNode);
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var typeBuilder = module.DefineType(type.Name + "_" + id++, TypeAttributes.Class | TypeAttributes.Public, parentType);

            typesBeingBuilt[type] = typeBuilder;

            var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] {parentType});
            using(var il = new GroboIL(constructor))
            {
                il.Ldarg(0); // stack: [this]
                il.Ldarg(1); // stack: [this, parent]
                var baseConstructor = parentType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] {parentType}, null);
                il.Call(baseConstructor); // base(parent); stack: []
                il.Ret();
            }
            var fields = new Dictionary<string, FieldBuilder>();
            foreach(var property in properties)
            {
                var propertyType = property.PropertyType;
                Type fieldType;
                if(propertyType.IsArray)
                    fieldType = typeof(ValidationResultTreeArrayNode<>).MakeGenericType(BuildType(propertyType.GetElementType()));
                else if(propertyType.IsDictionary() || propertyType == typeof(Hashtable))
                    fieldType = typeof(ValidationResultTreeUniversalNode);
                else
                    fieldType = BuildType(propertyType, true);
                var field = typeBuilder.DefineField(property.Name, fieldType, FieldAttributes.Public);
                fields.Add(property.Name, field);
            }
            var getChildrenMethod = parentType.GetMethod("GetChildren", BindingFlags.Instance | BindingFlags.NonPublic);
            var getChildrenMethodBuilder = typeBuilder.DefineMethod(getChildrenMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual, typeof(IEnumerable<KeyValuePair<object, ValidationResultTreeNode>>), Type.EmptyTypes);
            using(var il = new GroboIL(getChildrenMethodBuilder))
            {
                var listType = typeof(List<KeyValuePair<object, ValidationResultTreeNode>>);
                var addMethod = listType.GetMethod("Add", new[] {typeof(KeyValuePair<object, ValidationResultTreeNode>)});
                var itemConstructor = typeof(KeyValuePair<object, ValidationResultTreeNode>).GetConstructor(new[] {typeof(object), parentType});
                var list = il.DeclareLocal(listType);
                il.Newobj(listType.GetConstructor(Type.EmptyTypes)); // stack: [new List<>()]
                il.Stloc(list); // list = new List<>(); stack: []
                foreach(var field in fields.Values)
                {
                    il.Ldarg(0); // stack: [this]
                    il.Ldfld(field); // stack: [this.field]
                    var nextLabel = il.DefineLabel("next");
                    il.Brfalse(nextLabel); // if(this.field == null) goto next; stack: []
                    il.Ldloc(list); // stack: [list]
                    il.Ldstr(field.Name); // stack: [list, field.Name]
                    il.Ldarg(0); // stack: [list, field.Name, this]
                    il.Ldfld(field); // stack: [list, field.Name, this.field]
                    il.Newobj(itemConstructor); // stack: [list, new KeyValuePair<object, ValidationResultTreeNode>(field.Name, this.field)]
                    il.Call(addMethod); // list.Add(new KeyValuePair<object, ValidationResultTreeNode>(field.Name, this.field)); stack: []
                    il.MarkLabel(nextLabel);
                }
                il.Ldloc(list);
                il.Ret();
            }
            typeBuilder.DefineMethodOverride(getChildrenMethodBuilder, getChildrenMethod);

            var traverseEdgeMethod = parentType.GetMethod("TraverseEdge", BindingFlags.Instance | BindingFlags.NonPublic);
            var traverseEdgeMethodBuilder = typeBuilder.DefineMethod(traverseEdgeMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual, parentType, new[] { typeof(Expression) });
            using(var il = new GroboIL(traverseEdgeMethodBuilder))
            {
                il.Ldarg(1); // stack: [edge]
                il.Castclass(typeof(MemberExpression)); // stack: [(MemberExpression)edge]
                il.Call(HackHelpers.GetProp<MemberExpression>(x => x.Member).GetGetMethod()); // stack: [((MemberExpresion)edge).Member]
                il.Call(HackHelpers.GetProp<MemberInfo>(x => x.Name).GetGetMethod()); // stack: [((MemberExpresion)edge).Member.Name]
                var member = il.DeclareLocal(typeof(string));
                il.Stloc(member);
                foreach(var property in properties)
                {
                    il.Ldstr(property.Name); // stack: [property.Name]
                    il.Ldloc(member); // stack: [property.Name, member]
                    il.Call(typeof(string).GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public));
                    var nextLabel = il.DefineLabel("next");
                    il.Brfalse(nextLabel); // if(property.Name != member) goto next; stack: []
                    il.Ldarg(0); // stack: [this]
                    il.Ldfld(fields[property.Name]); // stack: [this.field]
                    il.Ret(); // return this.field;
                    il.MarkLabel(nextLabel);
                }
                il.Ldnull();
                il.Ret();
            }
            typeBuilder.DefineMethodOverride(traverseEdgeMethodBuilder, traverseEdgeMethod);

            var result = typeBuilder.CreateType();
            typesBeingBuilt[type] = null;
            return result;
        }

        private static volatile int id;
        private static readonly Hashtable types = new Hashtable();
        private static readonly Hashtable typesBeingBuilt = new Hashtable();
        private static readonly object typesLock = new object();
        private static readonly Hashtable factories = new Hashtable();
        private static readonly object factoriesLock = new object();

        private static readonly AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.RunAndSave);
        private static readonly ModuleBuilder module = assembly.DefineDynamicModule(Guid.NewGuid().ToString());
    }

    public class ValidationResultTreeNode<T> : IEnumerable<FormattedValidationResult>
    {
        public ValidationResultTreeNode()
        {
            root = ValidationResultTreeNodeBuilder.BuildFactory(typeof(T), true)(null);
        }

        public IEnumerator<FormattedValidationResult> GetEnumerator()
        {
            return root.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(string path, FormattedValidationResult validationResult)
        {
            var pieces = path.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries);
            var node = root;
            foreach(var edge in pieces)
            {
                if(node is ValidationResultTreeUniversalNode)
                    node = ((ValidationResultTreeUniversalNode)node).GotoChild(edge);
                else if(node is ValidationResultTreeArrayNode)
                    node = ((ValidationResultTreeArrayNode)node).GotoChild(int.Parse(edge));
                else
                {
                    var nodeType = node.GetType();
                    var field = nodeType.GetField(edge, BindingFlags.Instance | BindingFlags.Public);
                    var child = (ValidationResultTreeNode)field.GetValue(node);
                    if(child == null)
                        field.SetValue(node, child = ValidationResultTreeNodeBuilder.BuildFactory(field.FieldType, false)(node));
                    node = child;
                }
            }
            node.ValidationResults.Add(validationResult);
        }

        private readonly ValidationResultTreeNode root;
    }

    public class ValidationResultTreeUniversalNode : ValidationResultTreeNode
    {
        public ValidationResultTreeUniversalNode(ValidationResultTreeNode parent)
            : base(parent)
        {
        }

        public ValidationResultTreeNode GotoChild(object edge)
        {
            ValidationResultTreeNode child;
            if(!children.TryGetValue(edge, out child))
                children.Add(edge, child = new ValidationResultTreeUniversalNode(this));
            return child;
        }

        protected override ValidationResultTreeNode TraverseEdge(Expression edge)
        {
            throw new NotImplementedException("");
        }

        protected override IEnumerable<KeyValuePair<object, ValidationResultTreeNode>> GetChildren()
        {
            return children;
        }

        private readonly Dictionary<object, ValidationResultTreeNode> children = new Dictionary<object, ValidationResultTreeNode>();
    }

    public class ValidationResultTreeArrayNode<TChild> : ValidationResultTreeArrayNode
        where TChild : ValidationResultTreeNode
    {
        public ValidationResultTreeArrayNode(ValidationResultTreeNode parent)
            : base(parent, childCreator)
        {
        }

        private static readonly Func<ValidationResultTreeNode, ValidationResultTreeNode> childCreator = ValidationResultTreeNodeBuilder.BuildFactory(typeof(TChild), false);
    }

    public class ValidationResultTreeArrayNode : ValidationResultTreeNode
    {
        public ValidationResultTreeArrayNode(ValidationResultTreeNode parent, Func<ValidationResultTreeNode, ValidationResultTreeNode> childFactory)
            : base(parent)
        {
            this.childFactory = childFactory;
        }

        public ValidationResultTreeNode GotoChild(int[] indexes)
        {
            if(indexes.Length == 0)
                return null;
            var index = indexes[0];
            for(var i = 1; i < indexes.Length; ++i)
            {
                if(indexes[i] != index)
                    return null;
            }
            return GotoChild(index);
        }

        public ValidationResultTreeNode GotoChild(int index)
        {
            if(index == -1)
                return negativeChild ?? (negativeChild = childFactory(this));
            if(index >= children.Count)
                forceCount(children, index + 1);
            return children[index] ?? (children[index] = childFactory(this));
        }

        protected override ValidationResultTreeNode TraverseEdge(Expression edge)
        {
            if(edge.NodeType != ExpressionType.ArrayIndex)
                throw new InvalidOperationException("Expected array indexing but was '" + edge.NodeType + "'");
            int index = GetIndex(((BinaryExpression)edge).Right);
            if(index == -1)
                return negativeChild;
            if(index < 0 || index>= children.Count)
                return null;
            return children[index];
        }

        private static int GetIndex(Expression node)
        {
            if(node.NodeType == ExpressionType.Constant)
                return (int)((ConstantExpression)node).Value;
            return ExpressionCompiler.Compile(Expression.Lambda<Func<int>>(node))();
        }

        protected override IEnumerable<KeyValuePair<object, ValidationResultTreeNode>> GetChildren()
        {
            if(negativeChild != null)
                yield return new KeyValuePair<object, ValidationResultTreeNode>(-1, negativeChild);
            for(var i = 0; i < children.Count; ++i)
            {
                var child = children[i];
                if(child != null)
                    yield return new KeyValuePair<object, ValidationResultTreeNode>(i, child);
            }
        }

        private static Action<List<ValidationResultTreeNode>, int> EmitForceCount()
        {
            var listType = typeof(List<ValidationResultTreeNode>);
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {listType, typeof(int)}, typeof(string), true);
            var ensureCapacityMethod = listType.GetMethod("EnsureCapacity", BindingFlags.Instance | BindingFlags.NonPublic);
            if(ensureCapacityMethod == null)
                throw new InvalidOperationException("Method 'List<>.EnsureCapacity' is missing");
            var _sizeField = listType.GetField("_size", BindingFlags.Instance | BindingFlags.NonPublic);
            if(_sizeField == null)
                throw new InvalidOperationException("The field 'List<>._size' is not found");
            using(var il = new GroboIL(method))
            {
                il.Ldarg(0); // stack: [list]
                il.Ldarg(1); // stack: [list, size]
                il.Call(ensureCapacityMethod); // list.EnsureCapacity(size); stack: []
                il.Ldarg(0); // stack: [list]
                il.Ldarg(1); // stack: [list, size]
                il.Stfld(_sizeField); // list._size = size; stack: []
                il.Ret();
            }
            return (Action<List<ValidationResultTreeNode>, int>)method.CreateDelegate(typeof(Action<List<ValidationResultTreeNode>, int>));
        }

        private readonly Func<ValidationResultTreeNode, ValidationResultTreeNode> childFactory;

        private static readonly Action<List<ValidationResultTreeNode>, int> forceCount = EmitForceCount();

        private ValidationResultTreeNode negativeChild;
        private readonly List<ValidationResultTreeNode> children = new List<ValidationResultTreeNode>();
    }

    public abstract class ValidationResultTreeNode : IEnumerable<FormattedValidationResult>
    {
        private readonly ValidationResultTreeNode parent;

        protected ValidationResultTreeNode(ValidationResultTreeNode parent)
        {
            this.parent = parent;
            ValidationResults = new List<FormattedValidationResult>();
        }

        public ValidationResultTreeNode Traverse<T, TV>(Expression<Func<T, TV>> path)
        {
            var shards = path.Body.SmashToSmithereens();
            if(shards[0].NodeType != ExpressionType.Parameter)
                throw new InvalidOperationException("Expected parameter but was '" + shards[0].NodeType + "'");
            var node = this;
            for(int i = 1; i < shards.Length && node != null; ++i)
                node = node.TraverseEdge(shards[i]);
            return node;
        }

        protected abstract ValidationResultTreeNode TraverseEdge(Expression edge);

        public IEnumerator<FormattedValidationResult> GetEnumerator()
        {
            return GetEnumerator(false);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string Print()
        {
            var result = new StringBuilder();
            Print("ROOT", 0, result);
            return result.ToString();
        }

//        public void AddValidationResult(FormattedValidationResult validationResult, object[][] paths)
//        {
//            var node = this;
//            paths = paths.Where(path => path.Length > 0).ToArray();
//            if(paths.Length > 0)
//            {
//                int lcp;
//                for(lcp = 0;; ++lcp)
//                    if(!paths.All(path => lcp < path.Length && Eq(path[lcp], paths[0][lcp]))) break;
//                if(!(lcp == 1 && ReferenceEquals(paths[0][0], "")))
//                {
//                    foreach(var edge in paths[0].Take(lcp))
//                    {
//                        ++node.Count;
//                        node = node.GotoChild(edge);
//                    }
//                }
//            }
//            node.ValidationResults.Add(validationResult);
//            ++node.Count;
//        }

        public void AddValidationResult(FormattedValidationResult validationResult)
        {
            ValidationResults.Add(validationResult);
            var node = this;
            while(node != null)
            {
                ++node.Count;
                node = node.parent;
            }
        }

        public bool Exhausted { get { return Count >= MaxValidationResults; } }

        public int Count { get; private set; }

        public List<FormattedValidationResult> ValidationResults { get; private set; }
        public const int MaxValidationResults = 1000;
        protected abstract IEnumerable<KeyValuePair<object, ValidationResultTreeNode>> GetChildren();

        private void AppendLine(int margin, string value, StringBuilder result)
        {
            for(var i = 0; i < margin; ++i)
                result.Append(' ');
            result.AppendLine(value);
        }

        private void Print(string name, int margin, StringBuilder result)
        {
            AppendLine(margin, name, result);
            margin += 4;
            foreach(var validationResult in ValidationResults)
                AppendLine(margin, string.Format("Result: '{0}', Priority: '{1}', Text: '{2}'", validationResult.Type, validationResult.Priority, validationResult.Message.GetText("RU")), result);
            foreach(var child in GetChildren())
                child.Value.Print("<" + child.Key + ">", margin, result);
        }

//        public void Add(string path, FormattedValidationResult validationResult)
//        {
//            var pieces = path.Split('.');
//            AddValidationResult(validationResult, new[] {pieces.Select(piece =>
//                {
//                    int idx;
//                    return int.TryParse(piece, out idx) ? (object)idx : piece;
//                }).ToArray()});
//        }

        private static bool Eq(object x, object y)
        {
            if(x is string && y is string)
                return (string)x == (string)y;
            if(x is int && y is int)
                return (int)x == (int)y;
            return false;
        }

        private IEnumerator<FormattedValidationResult> GetEnumerator(bool returnFirst)
        {
            return new ValidationResultTreeNodeEnumerator(this, returnFirst);
        }

        private class ZzzComparer : IComparer<object>
        {
            public int Compare(object x, object y)
            {
                if(x is string && y is string)
                    return string.Compare(((string)x), (string)y, StringComparison.Ordinal);
                if(x is int && y is int)
                    return ((int)x).CompareTo((int)y);
                throw new InvalidOperationException("Incompatible types");
            }
        }

        private class ValidationResultTreeNodeEnumerator : IEnumerator<FormattedValidationResult>
        {
            public ValidationResultTreeNodeEnumerator(ValidationResultTreeNode node, bool returnAll)
            {
                this.returnAll = returnAll;
                var orderedValidationResults = node.ValidationResults.OrderBy(result => result).ToList();
                var priority = orderedValidationResults.Count == 0 ? 0 : orderedValidationResults[0].Priority;
                validationResultsEnumerator = returnAll
                    ? orderedValidationResults.GetEnumerator()
                    : orderedValidationResults.Where(result => result.Priority == priority).ToList().GetEnumerator();
                childrenEnumerator = node.GetChildren().OrderBy(pair => pair.Key, new ZzzComparer()).ToList().GetEnumerator();
                Reset();
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if(currentEnumerator.MoveNext())
                    return true;
                if(!childrenEnumerator.MoveNext())
                    return false;
                var child = (KeyValuePair<object, ValidationResultTreeNode>)childrenEnumerator.Current;
                currentEnumerator = child.Value.GetEnumerator(returnAll || (child.Key is int && (int)child.Key == -1));
                return MoveNext();
            }

            public void Reset()
            {
                validationResultsEnumerator.Reset();
                childrenEnumerator.Reset();
                currentEnumerator = validationResultsEnumerator;
            }

            public FormattedValidationResult Current { get { return currentEnumerator.Current; } }

            object IEnumerator.Current { get { return Current; } }
            private readonly bool returnAll;
            private readonly IEnumerator<FormattedValidationResult> validationResultsEnumerator;
            private readonly IEnumerator childrenEnumerator;
            private IEnumerator<FormattedValidationResult> currentEnumerator;
        }
    }
}