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
            return Expression.MemberInit(Expression.New(typeof(SimplePathFormatterText)), Expression.Bind(pathsProperty, Expression.NewArrayInit(typeof(string), paths.Select(GetText))));
        }

        private Expression GetText(Expression path)
        {
            var current = new StringBuilder();
            Expression result = null;
            GetText(path, current, ref result);
            if(current.Length > 0)
            {
                Expression cur = Expression.Constant(current.ToString());
                result = result == null ? cur : Expression.Add(result, cur, stringConcatMethod);
            }
            return result ?? Expression.Constant(null, typeof(string));
        }

        private void GetText(Expression path, StringBuilder current, ref Expression result)
        {
            switch(path.NodeType)
            {
            case ExpressionType.Parameter:
                break;
            case ExpressionType.MemberAccess:
                {
                    var memberExpression = (MemberExpression)path;
                    GetText(memberExpression.Expression, current, ref result);
                    if(result != null || current.Length > 0)
                        current.Append(".");
                    current.Append(memberExpression.Member.Name);
                    break;
                }
            case ExpressionType.ArrayIndex:
                {
                    var binaryExpression = (BinaryExpression)path;
                    GetText(binaryExpression.Left, current, ref result);
                    var index = binaryExpression.Right;
                    if(index.NodeType == ExpressionType.Constant)
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
                    if(methodCallExpression.Method.IsCurrentMethod() || methodCallExpression.Method.IsEachMethod())
                    {
                        Expression array = methodCallExpression.Arguments.Single();
                        GetText(array, current, ref result);
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
                    if(methodCallExpression.Method.IsWhereMethod() || methodCallExpression.Method.IsToArrayMethod() || methodCallExpression.Method.IsSelectMethod())
                    {
                        GetText(methodCallExpression.Arguments[0], current, ref result);
                        break;
                    }
                    throw new NotSupportedException("Method " + methodCallExpression.Method + " is not supported");
                }
            default:
                throw new NotSupportedException("Node type '" + path.NodeType + "' is not supported");
            }
        }

        private readonly MethodInfo stringConcatMethod = ((BinaryExpression)((Expression<Func<string, string, string>>)((s1, s2) => s1 + s2)).Body).Method;

        private readonly MemberInfo pathsProperty = ((MemberExpression)((Expression<Func<SimplePathFormatterText, string[]>>)(text => text.Paths)).Body).Member;
    }
}