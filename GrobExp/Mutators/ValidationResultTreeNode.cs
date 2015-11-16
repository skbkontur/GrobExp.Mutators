using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

using GrEmit;

using GrobExp.Compiler;

namespace GrobExp.Mutators
{
    public static class ValidationResultTreeNodeBuilder
    {
        public static Type Build(Type type)
        {
            var result = (Type)cache[type];
            if(result == null)
            {
                lock(lockObject)
                {
                    result = (Type)cache[type];
                    if(result == null)
                        cache[type] = result = BuildInternal(type);
                }
            }
            return result;
        }

        private static Type BuildInternal(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var typeBuilder = module.DefineType(type.Name + "_" + id++, TypeAttributes.Class | TypeAttributes.Public, typeof(ValidationResultTreeNode));
            var fields = new List<FieldBuilder>();
            foreach(var property in properties)
            {
                var propertyType = property.PropertyType;
                Type fieldType;
                if(propertyType.IsArray)
                    fieldType = typeof(ValidationResultTreeArrayNode<>).MakeGenericType(Build(propertyType.GetElementType()));
                else if(propertyType.IsDictionary() || propertyType == typeof(Hashtable))
                    fieldType = typeof(ValidationResultTreeUniversalNode);
                else
                    fieldType = Build(propertyType);
                var field = typeBuilder.DefineField(property.Name, fieldType, FieldAttributes.Public);
                fields.Add(field);
            }
            var getChildrenMethod = typeof(ValidationResultTreeNode).GetMethod("GetChildren", BindingFlags.Instance | BindingFlags.NonPublic);
            var getChildrenMethodBuilder = typeBuilder.DefineMethod(getChildrenMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual, typeof(IEnumerable<KeyValuePair<object, ValidationResultTreeNode>>), Type.EmptyTypes);
            using(var il = new GroboIL(getChildrenMethodBuilder))
            {
                var listType = typeof(List<KeyValuePair<object, ValidationResultTreeNode>>);
                var addMethod = listType.GetMethod("Add", new[] {typeof(KeyValuePair<object, ValidationResultTreeNode>)});
                var itemConstructor = typeof(KeyValuePair<object, ValidationResultTreeNode>).GetConstructor(new[] {typeof(object), typeof(ValidationResultTreeNode)});
                var result = il.DeclareLocal(listType);
                il.Newobj(listType.GetConstructor(Type.EmptyTypes)); // stack: [new List<>()]
                il.Stloc(result); // result = new List<>(); stack: []
                foreach(var field in fields)
                {
                    il.Ldarg(0); // stack: [this]
                    il.Ldfld(field); // stack: [this.field]
                    var nextLabel = il.DefineLabel("next");
                    il.Brfalse(nextLabel); // if(this.field == null) goto next; stack: []
                    il.Ldloc(result); // stack: [result]
                    il.Ldstr(field.Name); // stack: [result, field.Name]
                    il.Ldarg(0); // stack: [result, field.Name, this]
                    il.Ldfld(field); // stack: [result, field.Name, this.field]
                    il.Newobj(itemConstructor); // stack: [result, new KeyValuePair<object, ValidationResultTreeNode>(field.Name, this.field)]
                    il.Call(addMethod); // result.Add(new KeyValuePair<object, ValidationResultTreeNode>(field.Name, this.field)); stack: []
                    il.MarkLabel(nextLabel);
                }
                il.Ldloc(result);
                il.Ret();
            }
            typeBuilder.DefineMethodOverride(getChildrenMethodBuilder, getChildrenMethod);
            return typeBuilder.CreateType();
        }

        private static volatile int id;
        private static readonly Hashtable cache = new Hashtable();
        private static readonly object lockObject = new object();

        private static readonly AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.RunAndSave);
        private static readonly ModuleBuilder module = assembly.DefineDynamicModule(Guid.NewGuid().ToString());
    }

    public class ValidationResultTreeNode<T> : IEnumerable<FormattedValidationResult>
    {
        public ValidationResultTreeNode()
        {
            root = (ValidationResultTreeNode)Activator.CreateInstance(ValidationResultTreeNodeBuilder.Build(typeof(T)));
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
            var pieces = path.Split(new [] {'.'}, StringSplitOptions.RemoveEmptyEntries);
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
                        field.SetValue(node, child = (ValidationResultTreeNode)Activator.CreateInstance(field.FieldType));
                    node = child;
                }
            }
            node.ValidationResults.Add(validationResult);
        }

        private readonly ValidationResultTreeNode root;
    }

    public class ValidationResultTreeUniversalNode : ValidationResultTreeNode
    {
        public ValidationResultTreeNode GotoChild(object edge)
        {
            ValidationResultTreeNode child;
            if(!children.TryGetValue(edge, out child))
                children.Add(edge, child = new ValidationResultTreeUniversalNode());
            return child;
        }

        protected override IEnumerable<KeyValuePair<object, ValidationResultTreeNode>> GetChildren()
        {
            return children;
        }

        private readonly Dictionary<object, ValidationResultTreeNode> children = new Dictionary<object, ValidationResultTreeNode>();
    }

    public class ValidationResultTreeArrayNode<TChild> : ValidationResultTreeArrayNode
        where TChild : ValidationResultTreeNode, new()
    {
        public ValidationResultTreeArrayNode()
            : base(() => new TChild())
        {
        }
    }

    public class ValidationResultTreeArrayNode : ValidationResultTreeNode
    {
        public ValidationResultTreeArrayNode(Func<ValidationResultTreeNode> childFactory)
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
                return negativeChild ?? (negativeChild = childFactory());
            if(index >= children.Count)
                forceCount(children, index + 1);
            return children[index] ?? (children[index] = childFactory());
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

        private readonly Func<ValidationResultTreeNode> childFactory;

        private static readonly Action<List<ValidationResultTreeNode>, int> forceCount = EmitForceCount();

        private ValidationResultTreeNode negativeChild;
        private readonly List<ValidationResultTreeNode> children = new List<ValidationResultTreeNode>();
    }

    public abstract class ValidationResultTreeNode : IEnumerable<FormattedValidationResult>
    {
        protected ValidationResultTreeNode()
        {
            ValidationResults = new List<FormattedValidationResult>();
        }

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