using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GrobExp.Mutators
{
    public class ValidationResultTreeNode : IEnumerable<FormattedValidationResult>
    {
        public ValidationResultTreeNode()
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

        public void Add(string path, FormattedValidationResult validationResult)
        {
            AddValidationResult(validationResult, new[] {path.Split('.')});
        }

        public void AddValidationResult(FormattedValidationResult validationResult, string[][] paths)
        {
            paths = paths.Where(path => path.Length > 0).ToArray();
            if(paths.Length == 0) return;
            int lcp;
            for(lcp = 0;; ++lcp)
                if(!paths.All(path => lcp < path.Length && path[lcp] == paths[0][lcp])) break;
            Traverse(paths[0].Take(lcp)).ValidationResults.Add(validationResult);
        }

        public ValidationResultTreeNode Traverse<TRoot, TChild>(Expression<Func<TRoot, TChild>> path)
        {
            var edges = new List<string>();
            var exp = path.Body;
            while(exp.NodeType != ExpressionType.Parameter)
            {
                switch(exp.NodeType)
                {
                case ExpressionType.MemberAccess:
                    var memberExpression = (MemberExpression)exp;
                    edges.Add(memberExpression.Member.Name);
                    exp = memberExpression.Expression;
                    break;
                case ExpressionType.ArrayIndex:
                    var binaryExpression = (BinaryExpression)exp;
                    edges.Add(ExpressionCompiler.Compile(Expression.Lambda<Func<int>>(binaryExpression.Right))().ToString());
                    exp = binaryExpression.Left;
                    break;
                default:
                    throw new NotSupportedException("Node type '" + exp.NodeType + "' is not supported");
                }
            }
            edges.Reverse();
            var node = this;
            foreach(var edge in edges)
            {
                ValidationResultTreeNode child;
                if(!node.children.TryGetValue(edge, out child))
                    return null;
                node = child;
            }
            return node;
        }

        public List<FormattedValidationResult> ValidationResults { get; private set; }

        private IEnumerator<FormattedValidationResult> GetEnumerator(bool returnFirst)
        {
            return new ValidationResultTreeNodeEnumerator(this, returnFirst);
        }

        private void Dfs(string path, Dictionary<string, List<FormattedValidationResult>> result)
        {
            List<FormattedValidationResult> current;
            if(!result.TryGetValue(path, out current))
                result.Add(path, ValidationResults);
            else
                current.AddRange(ValidationResults);
            foreach(var entry in children)
            {
                var edge = entry.Key;
                entry.Value.Dfs(string.IsNullOrEmpty(path) ? edge : path + "." + edge, result);
            }
        }

        private static ValidationResultTreeNode Create(FormattedValidationResult[] validationResults, string[] keys, ValidationResultTreeNode[] values)
        {
            var result = new ValidationResultTreeNode();
            if(validationResults != null)
                result.ValidationResults.AddRange(validationResults);
            if(keys != null && values != null)
            {
                for(var i = 0; i < keys.Length && i < values.Length; ++i)
                    result.children.Add(keys[i], values[i]);
            }
            return result;
        }

        private ValidationResultTreeNode Traverse(IEnumerable<string> path)
        {
            var node = this;
            foreach(var edge in path)
            {
                ValidationResultTreeNode child;
                if(!node.children.TryGetValue(edge, out child))
                {
                    child = new ValidationResultTreeNode();
                    node.children.Add(edge, child);
                }
                node = child;
            }
            return node;
        }

        private readonly Dictionary<string, ValidationResultTreeNode> children = new Dictionary<string, ValidationResultTreeNode>();

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
                childrenEnumerator = node.children.OrderBy(pair => pair.Key).ToList().GetEnumerator();
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
                var child = (KeyValuePair<string, ValidationResultTreeNode>)childrenEnumerator.Current;
                currentEnumerator = child.Value.GetEnumerator(returnAll || child.Key == "-1");
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