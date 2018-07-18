using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace GrobExp.Mutators
{
    public class SimplePathFormatter : IPathFormatter
    {
        public Expression GetFormattedPath(Expression[] paths)
        {
            var formattedPaths = paths.Select(GetText).ToArray();
            if (formattedPaths.Any(path => path == null))
                return null;
            return Expression.MemberInit(
                Expression.New(typeof(SimplePathFormatterText)),
                Expression.Bind(pathsProperty, Expression.NewArrayInit(typeof(string), formattedPaths)));
        }

        private Expression GetText(Expression path)
        {
            var current = new StringBuilder();
            Expression result = null;
            if (!GetText(path, current, ref result))
                return null;
            if (current.Length > 0)
            {
                Expression cur = Expression.Constant(current.ToString());
                result = result == null ? cur : Expression.Add(result, cur, stringConcatMethod);
            }

            return result ?? Expression.Constant(null, typeof(string));
        }

        private bool GetText(Expression path, StringBuilder current, ref Expression result)
        {
            switch (path.NodeType)
            {
            case ExpressionType.Parameter:
                break;
            case ExpressionType.MemberAccess:
                {
                    var memberExpression = (MemberExpression)path;
                    if (!GetText(memberExpression.Expression, current, ref result))
                        return false;
                    if (result != null || current.Length > 0)
                        current.Append(".");
                    current.Append(memberExpression.Member.Name);
                    break;
                }
            case ExpressionType.ArrayIndex:
                {
                    var binaryExpression = (BinaryExpression)path;
                    if (!GetText(binaryExpression.Left, current, ref result))
                        return false;
                    var index = binaryExpression.Right;
                    if (index.NodeType == ExpressionType.Constant)
                    {
                        current.Append("[");
                        current.Append(((ConstantExpression)index).Value);
                        current.Append("]");
                    }
                    else
                    {
                        current.Append("[");
                        Expression cur = Expression.Constant(current.ToString());
                        result = result == null ? cur : Expression.Add(result, cur, stringConcatMethod);
                        result = Expression.Add(result, Expression.Call(index, "ToString", Type.EmptyTypes), stringConcatMethod);
                        current.Clear();
                        current.Append("]");
                    }

                    break;
                }
            case ExpressionType.Call:
                {
                    var methodCallExpression = (MethodCallExpression)path;
                    if (methodCallExpression.Method.IsCurrentMethod() || methodCallExpression.Method.IsEachMethod())
                    {
                        Expression array = methodCallExpression.Arguments.Single();
                        if (!GetText(array, current, ref result))
                            return false;
                        current.Append("[");
                        Expression cur = Expression.Constant(current.ToString());
                        result = result == null ? cur : Expression.Add(result, cur, stringConcatMethod);
                        Type itemType = array.Type.GetItemType();
                        Expression index = Expression.Call(MutatorsHelperFunctions.CurrentIndexMethod.MakeGenericMethod(itemType), Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(itemType), array));
                        result = Expression.Add(result, Expression.Call(index, "ToString", Type.EmptyTypes), stringConcatMethod);
                        current.Clear();
                        current.Append("]");
                        break;
                    }

                    if (methodCallExpression.Method.IsWhereMethod() || methodCallExpression.Method.IsToArrayMethod() || methodCallExpression.Method.IsSelectMethod())
                    {
                        if (!GetText(methodCallExpression.Arguments[0], current, ref result))
                            return false;
                        break;
                    }

                    if (methodCallExpression.Method.IsIndexerGetter())
                    {
                        if (!GetText(methodCallExpression.Object, current, ref result))
                            return false;
                        if (methodCallExpression.Arguments.Count != 1)
                            throw new NotSupportedException();
                        current.Append(".");
                        current.Append(((ConstantExpression)methodCallExpression.Arguments[0]).Value);
                        Expression cur = Expression.Constant(current.ToString());
                        result = result == null ? cur : Expression.Add(result, cur, stringConcatMethod);
                        current.Clear();
                        break;
                    }

                    return false;
                }
            case ExpressionType.Convert:
                if (!GetText(((UnaryExpression)path).Operand, current, ref result))
                    return false;
                break;
            default:
                return false;
            }

            return true;
        }

        private readonly MethodInfo stringConcatMethod = ((BinaryExpression)((Expression<Func<string, string, string>>)((s1, s2) => s1 + s2)).Body).Method;

        private readonly MemberInfo pathsProperty = ((MemberExpression)((Expression<Func<SimplePathFormatterText, string[]>>)(text => text.Paths)).Body).Member;
    }
}