using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators.Visitors
{
    public class SelectManyCollectionSelectorExtender : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var method = node.Method;
            if (IsEnumerableSelectManyMethod(method))
            {
                var enumerable = Visit(node.Arguments[0]);
                var selector = (LambdaExpression)Visit(node.Arguments[1]);
                var collectionType = selector.Body.Type;
                Expression emptyCollection = null;
                if (collectionType.IsArray)
                    emptyCollection = Expression.NewArrayBounds(collectionType.GetElementType(), Expression.Constant(0));
                else
                {
                    var constructor = collectionType.GetConstructor(Type.EmptyTypes);
                    if (constructor != null)
                        emptyCollection = Expression.New(constructor);
                    else if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                        emptyCollection = Expression.Convert(Expression.NewArrayBounds(collectionType.GetItemType(), Expression.Constant(0)), collectionType);
                    else throw new InvalidOperationException("Cannot create an empty collection of type " + collectionType);
                }

                selector = Expression.Lambda(Expression.Coalesce(selector.Body, emptyCollection), selector.Parameters);
                return Expression.Call(Visit(node.Object), method, new[] {enumerable, selector}.Concat(node.Arguments.Skip(2)));
            }

            return base.VisitMethodCall(node);
        }

        private static bool IsEnumerableSelectManyMethod(MethodInfo method)
        {
            return method.DeclaringType == typeof(Enumerable) && method.IsGenericMethod && method.GetGenericMethodDefinition().Name == "SelectMany";
        }
    }
}