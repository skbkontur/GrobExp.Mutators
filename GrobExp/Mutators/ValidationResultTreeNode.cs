using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

using GrEmit;

using GrobExp.Mutators.ReadonlyCollections;

using GrobExp.Compiler;

namespace GrobExp.Mutators
{
    public static class ValidationResultTreeNodeFactory
    {
        public static ValidationResultTreeNode Create<T>()
        {
            return Create(typeof(T));
        }

        public static ValidationResultTreeNode Create(Type type)
        {
            if(type.IsArray)
                return new ValidationResultTreeArrayNode(type.GetElementType());
            if(type.IsDictionary() || type == typeof(Hashtable))
                return new ValidationResultTreeUniversalNode();
            var properties = cache.GetOrAdd(type, GetPropertyNames);
            return new ValidationResultTreePropertyNode(type, properties.children.Clone(x => x), properties.types);
        }

        private static void ExtractProperties(Type type, List<PropertyInfo> result)
        {
            if(type == null || type == typeof(object))
                return;
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            result.AddRange(properties);
            ExtractProperties(type.BaseType, result);
        }

        private static PropertyInfo[] GetProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
//            var result = new List<PropertyInfo>();
//            ExtractProperties(type, result);
//            return result;
        }

        private static PropertiesInfo GetPropertyNames(Type type)
        {
            var propertyInfos = GetProperties(type);
            var keys = propertyInfos.Select(property => property.Name).ToArray();
            return new PropertiesInfo
                {
                    keys = keys,
                    types = ReadonlyHashtable.Create(keys, propertyInfos.Select(property => property.PropertyType).ToArray()),
                    children = ReadonlyHashtable.Create(keys, new ValidationResultTreeNode[keys.Length]),
                };
        }

        private class PropertiesInfo
        {
            public string[] keys;
            public IReadonlyHashtable<Type> types;
            public IReadonlyHashtable<ValidationResultTreeNode> children;
        }

        private static readonly ConcurrentDictionary<Type, PropertiesInfo> cache = new ConcurrentDictionary<Type, PropertiesInfo>();
    }

    public class ValidationResultTreeNode<T> : IEnumerable<FormattedValidationResult>
    {
        public ValidationResultTreeNode()
        {
            root = ValidationResultTreeNodeFactory.Create<T>();
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
            root.Add(path, validationResult);
        }

        private readonly ValidationResultTreeNode root;
    }

    public class ValidationResultTreePropertyNode : ValidationResultTreeNode
    {
        public ValidationResultTreePropertyNode(Type type, IReadonlyHashtable<ValidationResultTreeNode> children, IReadonlyHashtable<Type> propertyTypes)
        {
            this.type = type;
            this.children = children;
            this.propertyTypes = propertyTypes;
        }

        protected override ValidationResultTreeNode GotoChild(object edge)
        {
            var str = edge as string;
            if(str == null)
                throw new InvalidOperationException("A string expected");
            ValidationResultTreeNode child;
            if(!children.TryGetValue(str, out child))
                throw new InvalidOperationException(string.Format("Type '{0}' has no property '{1}'", type, str));
            if(child != null)
                return child;
            Type propertyType;
            if(!propertyTypes.TryGetValue(str, out propertyType))
                throw new InvalidOperationException(string.Format("Type '{0}' has no property '{1}'", type, str));
            children.TryUpdateValue(str, child = ValidationResultTreeNodeFactory.Create(propertyType));
            return child;
        }

        protected override IEnumerable<KeyValuePair<object, ValidationResultTreeNode>> GetChildren()
        {
            foreach(var pair in children)
                if(pair.Value != null)
                    yield return new KeyValuePair<object, ValidationResultTreeNode>(pair.Key, pair.Value);
        }

        private readonly Type type;
        private readonly IReadonlyHashtable<ValidationResultTreeNode> children;
        private readonly IReadonlyHashtable<Type> propertyTypes;
    }

    public class ValidationResultTreeUniversalNode : ValidationResultTreeNode
    {
        protected override ValidationResultTreeNode GotoChild(object edge)
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

    public class ValidationResultTreeArrayNode : ValidationResultTreeNode
    {
        public ValidationResultTreeArrayNode(Type type)
        {
            this.type = type;
        }

        protected override ValidationResultTreeNode GotoChild(object edge)
        {
            if(!(edge is int))
                throw new InvalidOperationException("An int expected");
            var index = (int)edge;
            if(index == -1)
                return negativeChild ?? (negativeChild = ValidationResultTreeNodeFactory.Create(type));
            if(index >= children.Count)
                forceCount(children, index + 1);
            return children[index] ?? (children[index] = ValidationResultTreeNodeFactory.Create(type));
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

        private readonly Type type;
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

        private void AppendLine(int margin, string value, StringBuilder result)
        {
            for (int i = 0; i < margin; ++i)
                result.Append(' ');
            result.AppendLine(value);
        }

        private void Print(string name, int margin, StringBuilder result)
        {
            AppendLine(margin, name, result);
            margin += 4;
            foreach (var validationResult in ValidationResults)
                AppendLine(margin, string.Format("Result: '{0}', Priority: '{1}', Text: '{2}'", validationResult.Type, validationResult.Priority, validationResult.Message.GetText("RU")), result);
            foreach (var child in children)
            {
                child.Value.Print("<" + child.Key + ">", margin, result);
            }
        }

        public void Add(string path, FormattedValidationResult validationResult)
        {
            var pieces = path.Split('.');
            AddValidationResult(validationResult, new[] {pieces.Select(piece =>
                {
                    int idx;
                    return int.TryParse(piece, out idx) ? (object)idx : piece;
                }).ToArray()});
        }

        public void AddValidationResult(FormattedValidationResult validationResult, object[][] paths)
        {
            var node = this;
            paths = paths.Where(path => path.Length > 0).ToArray();
            if(paths.Length > 0)
            {
                int lcp;
                for(lcp = 0;; ++lcp)
                    if(!paths.All(path => lcp < path.Length && path[lcp] == paths[0][lcp])) break;
                if(!(lcp == 1 && ReferenceEquals(paths[0][0], "")))
                {
                    foreach(var edge in paths[0].Take(lcp))
                    {
                        ++node.Count;
                        node = node.GotoChild(edge);
                    }
                }
            }
            node.ValidationResults.Add(validationResult);
            ++node.Count;
        }

        public bool Exhausted { get { return Count >= MaxValidationResults; } }

        public int Count { get; private set; }

        public List<FormattedValidationResult> ValidationResults { get; private set; }
        public const int MaxValidationResults = 1000;
        protected abstract ValidationResultTreeNode GotoChild(object edge);
        protected abstract IEnumerable<KeyValuePair<object, ValidationResultTreeNode>> GetChildren();

        private IEnumerator<FormattedValidationResult> GetEnumerator(bool returnFirst)
        {
            return new ValidationResultTreeNodeEnumerator(this, returnFirst);
        }

        private class ZzzComparer: IComparer<object>
        {
            public int Compare(object x, object y)
            {
                if (x is string && y is string)
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