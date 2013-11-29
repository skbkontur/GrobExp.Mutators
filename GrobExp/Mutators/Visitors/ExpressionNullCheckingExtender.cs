using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators.Visitors
{
    public class ExpressionNullCheckingExtender : ExpressionVisitor
    {
        public Expression Extend(Expression node)
        {
            node = new SelectManyCollectionSelectorExtender().Visit(node);
            //return node;
            if(node.Type == typeof(bool))
            {
                node = Visit(node);
                return Expression.Equal(node, Expression.Constant(true, node.Type));
            }
            return Visit(node);
        }

        public override Expression Visit(Expression node)
        {
            return IsChain(node) ? VisitChain(node) : base.Visit(node);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if(node.Body.Type == typeof(bool))
            {
                var body = Visit(node.Body);
                return node.Update(Expression.Equal(body, Expression.Constant(true, body.Type)), node.Parameters);
            }
            return base.VisitLambda(node);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            switch(node.NodeType)
            {
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
                {
                    var operand = Visit(node.Operand);
                    return operand.Type == node.Type ? operand : node.Update(operand);
                }
            case ExpressionType.Not:
                {
                    var operand = Visit(node.Operand);
                    var variable = Expression.Variable(operand.Type);
                    Expression result = node.Update(variable);
                    if(node.Type == typeof(bool))
                        result = Expression.Convert(result, typeof(bool?));
                    if(CanBeNull(operand.Type))
                    {
                        var test = Expression.Equal(variable, Expression.Constant(null, operand.Type));
                        result = Expression.Condition(test, Expression.Constant(null, result.Type), result);
                    }
                    return Expression.Block(result.Type, new[] {variable}, new[] {Expression.Assign(variable, operand), result});
                }
            case ExpressionType.ArrayLength:
                {
                    var operand = Visit(node.Operand);
                    var array = Expression.Variable(operand.Type);
                    Expression result = Expression.Condition(Expression.Equal(array, Expression.Constant(null, array.Type)), Expression.Constant(0, typeof(int)), Expression.ArrayLength(array));
                    return Expression.Block(result.Type, new[] {array}, new[] {Expression.Assign(array, operand), result});
                }
            }
            return base.VisitUnary(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            switch(node.NodeType)
            {
            case ExpressionType.Assign:
                return Expression.Assign(node.Left, Visit(node.Right));
            case ExpressionType.Add:
            case ExpressionType.AddChecked:
            case ExpressionType.Subtract:
            case ExpressionType.SubtractChecked:
            case ExpressionType.Multiply:
            case ExpressionType.MultiplyChecked:
            case ExpressionType.Divide:
            case ExpressionType.Modulo:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.AndAlso:
            case ExpressionType.OrElse:
                {
                    var left = Visit(node.Left);
                    var right = Visit(node.Right);
                    var leftType = left.Type == typeof(bool) ? typeof(bool?) : left.Type;
                    var rightType = right.Type == typeof(bool) ? typeof(bool?) : right.Type;
                    var leftVariable = Expression.Variable(leftType);
                    var rightVariable = Expression.Variable(rightType);
                    var leftAssign = Expression.Assign(leftVariable, leftType == left.Type ? left : Expression.Convert(left, leftType));
                    var rightAssign = Expression.Assign(rightVariable, rightType == right.Type ? right : Expression.Convert(right, rightType));

                    Expression result = node.Update(leftVariable, (LambdaExpression)Visit(node.Conversion), rightVariable);
                    if(node.Type == typeof(bool))
                        result = Expression.Convert(result, typeof(bool?));

                    Expression test = null;
                    if(CanBeNull(left.Type))
                        test = Expression.Equal(leftVariable, Expression.Constant(null, left.Type));
                    if(CanBeNull(right.Type))
                    {
                        var current = Expression.Equal(rightVariable, Expression.Constant(null, right.Type));
                        test = test == null ? current : Expression.OrElse(test, current);
                    }
                    if(test != null)
                        result = Expression.Condition(test, Expression.Constant(null, result.Type), result);

                    if(node.NodeType == ExpressionType.AndAlso)
                    {
                        result = Expression.Condition(Expression.Equal(rightVariable, Expression.Constant(false, typeof(bool?))), Expression.Constant(false, typeof(bool?)), result);
                        var inner = Expression.Block(result.Type, new[] {rightVariable}, rightAssign, result);
                        var testIfFalse = Expression.Condition(Expression.Equal(leftVariable, Expression.Constant(false, typeof(bool?))), Expression.Constant(false, typeof(bool?)), inner);
                        return Expression.Block(result.Type, new[] {leftVariable}, leftAssign, testIfFalse);
                    }
                    if(node.NodeType == ExpressionType.OrElse)
                    {
                        result = Expression.Condition(Expression.Equal(rightVariable, Expression.Constant(true, typeof(bool?))), Expression.Constant(true, typeof(bool?)), result);
                        var inner = Expression.Block(result.Type, new[] {rightVariable}, rightAssign, result);
                        var testIfTrue = Expression.Condition(Expression.Equal(leftVariable, Expression.Constant(true, typeof(bool?))), Expression.Constant(true, typeof(bool?)), inner);
                        return Expression.Block(result.Type, new[] {leftVariable}, leftAssign, testIfTrue);
                    }
                    return Expression.Block(result.Type, new[] {leftVariable, rightVariable}, leftAssign, rightAssign, result);
                }
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
                return Expression.Convert(base.VisitBinary(node), typeof(bool?));
            default:
                return base.VisitBinary(node);
            }
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            return node.Update(Extend(node.Test), Visit(node.IfTrue), Visit(node.IfFalse));
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            return node.Update(Visit(node.Object), node.Arguments.Select((argument, i) => i == 0 && node.Method.IsExtension() ? Visit(argument) : Extend(argument)));
        }

        private delegate Expression ProcessMethodDelegate(MethodCallExpression node, out ParameterExpression[] variables, out Expression[] returnValues);

        private static bool CanBeNull(Type type)
        {
            return type.IsClass || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        private static bool IsChain(Expression node)
        {
            return (node.IsLinkOfChain(true, false) && node.NodeType != ExpressionType.Parameter)
                   || (node != null && node.NodeType == ExpressionType.Call && ((MethodCallExpression)node).Method.IsExtension());
        }

        private static object GetValueForCoalesce(Type type)
        {
            return type.GetDefaultValue();
        }

        private Expression VisitChain(Expression chain)
        {
            //return base.Visit(chain);

//            if(IsConstantAccess(chain) || !IsNullableOrReferenceTypeAccess(chain) || chain.Type == typeof(void))
//                return base.Visit(chain);
//            return Visit(Zzz.EvaluateChainSafely(chain));

            chain = base.Visit(chain);
            if(IsConstantAccess(chain) || !IsNullableOrReferenceTypeAccess(chain) || chain.Type == typeof(void))
                return base.Visit(chain);
            var variables = new List<ParameterExpression[]>();
            var returnValues = new List<Expression>();
            var conditions = new List<Expression>();
            Expression subChain = chain;
            do
            {
                ParameterExpression variable;
                switch(subChain.NodeType)
                {
                case ExpressionType.MemberAccess:
                    var memberExpression = (MemberExpression)subChain;
                    subChain = memberExpression.Expression;
                    variable = Expression.Variable(subChain.Type);
                    variables.Add(new[] {variable});
                    returnValues.Add(Expression.MakeMemberAccess(variable, memberExpression.Member));
                    conditions.Add(CheckNotNull(variable));
                    break;
                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression)subChain;
                    ParameterExpression[] curVariables;
                    Expression[] curReturnValues;
                    ProcessMethodDelegate methodProcessor = GetMethodProcessor(methodCallExpression.Method);
                    subChain = methodProcessor(methodCallExpression, out curVariables, out curReturnValues);
                    Expression condition = null;
                    foreach(var v in curVariables)
                    {
                        Expression current = CheckNotNull(v);
                        if(current != null)
                            condition = condition == null ? current : Expression.AndAlso(condition, current);
                    }
                    variables.Add(curVariables);
                    returnValues.AddRange(curReturnValues);
                    conditions.Add(condition);
                    break;
                case ExpressionType.ArrayIndex:
                    var binaryExpression = (BinaryExpression)subChain;
                    subChain = binaryExpression.Left;
                    variable = Expression.Variable(subChain.Type);
                    var index = Expression.Variable(binaryExpression.Right.Type);
                    variables.Add(new[] {index, variable});
                    var arrayNotNull = Expression.ReferenceNotEqual(variable, Expression.Constant(null, variable.Type));
                    var indexNonNegative = Expression.MakeBinary(ExpressionType.GreaterThanOrEqual, index, Expression.Constant(0));
                    var indexLessThanArrayLength = Expression.MakeBinary(ExpressionType.LessThan, index, Expression.ArrayLength(variable));
                    conditions.Add(Expression.AndAlso(arrayNotNull, Expression.AndAlso(indexNonNegative, indexLessThanArrayLength)));
                    returnValues.Add(Expression.MakeBinary(ExpressionType.ArrayIndex, variable, index, binaryExpression.IsLiftedToNull, binaryExpression.Method, binaryExpression.Conversion));
                    returnValues.Add(binaryExpression.Right);
                    break;
                default:
                    throw new InvalidOperationException();
                }
            } while(IsChain(subChain));
            var defaultValue = Expression.Constant(chain.Type.GetDefaultValue(), chain.Type);
            var result = returnValues[0];
            int idx = 1;
            for(int i = 0; i < variables.Count; ++i)
            {
                ParameterExpression[] curVariables = variables[i];
                var assigns = curVariables.Select(variable => Expression.Assign(variable, idx < returnValues.Count ? returnValues[idx++] : subChain)).Cast<Expression>().ToList();
                LabelTarget returnLabel = Expression.Label(chain.Type);
                Expression returnExpression = Expression.Return(returnLabel, result);
                Expression conditionalExp = conditions[i] == null ? returnExpression : Expression.IfThen(conditions[i], returnExpression);
                LabelExpression returnLabelExp = Expression.Label(returnLabel, defaultValue);
                result = Expression.Block(chain.Type, curVariables, assigns.Concat(new[] {conditionalExp, returnLabelExp}));
            }
            return result;
        }

        private static Expression DefaultMethodProcessor(MethodCallExpression node, out ParameterExpression[] variables, out Expression[] returnValues)
        {
            if(node.Object != null)
            {
                variables = new[] {Expression.Variable(node.Object.Type)};
                returnValues = new[] {Expression.Call(variables[0], node.Method, node.Arguments)};
                return node.Object;
            }
            variables = new[] {Expression.Variable(node.Arguments[0].Type)};
            returnValues = new[] {Expression.Call(node.Object, node.Method, new[] {variables[0]}.Concat(node.Arguments.Skip(1)))};
            return node.Arguments[0];
        }

        private static Expression LinqSelectManyMethodProcessor(MethodCallExpression node, out ParameterExpression[] variables, out Expression[] returnValues)
        {
            var enumerable = node.Arguments[0];
            var selector = (LambdaExpression)node.Arguments[1];
            var collectionType = selector.Body.Type;
            Expression emptyCollection = null;
            if(collectionType.IsArray)
                emptyCollection = Expression.NewArrayBounds(collectionType.GetElementType(), Expression.Constant(0));
            else
            {
                var constructor = collectionType.GetConstructor(Type.EmptyTypes);
                if(constructor != null)
                    emptyCollection = Expression.New(constructor);
                else if(collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    emptyCollection = Expression.Convert(Expression.NewArrayBounds(collectionType.GetItemType(), Expression.Constant(0)), collectionType);
                else throw new InvalidOperationException("Cannot create an empty collection of type " + collectionType);
            }
            selector = Expression.Lambda(Expression.Coalesce(selector.Body, emptyCollection), selector.Parameters);
            variables = new[] {Expression.Variable(enumerable.Type)};
            returnValues = new[] {Expression.Call(node.Object, node.Method, new[] {variables[0], (Expression)selector}.Concat(node.Arguments.Skip(2)))};
            return node.Arguments[0];
        }

        private static ProcessMethodDelegate GetMethodProcessor(MethodInfo method)
        {
//            if(method.IsGenericMethod)
//                method = method.GetGenericMethodDefinition();
//            if(method.DeclaringType == typeof(Enumerable))
//            {
//                if(method.Name == "SelectMany")
//                    return LinqSelectManyMethodProcessor;
//            }
            return DefaultMethodProcessor;
        }

        private static BinaryExpression CheckNotNull(Expression exp)
        {
            if(!exp.Type.IsValueType)
                return Expression.ReferenceNotEqual(exp, Expression.Constant(null, exp.Type));
            if(exp.Type.IsGenericType && exp.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return Expression.NotEqual(exp, Expression.Constant(null, exp.Type));
            return null;
        }

        private static bool IsConstantAccess(Expression chain)
        {
            return GetLeft(chain) is ConstantExpression;
        }

        private static Expression GetLeft(Expression chain)
        {
            switch(chain.NodeType)
            {
            case ExpressionType.Call:
                var methodCallExpression = (MethodCallExpression)chain;
                return methodCallExpression.Object ?? (methodCallExpression.Method.IsExtension() ? methodCallExpression.Arguments[0] : null);
            case ExpressionType.ArrayIndex:
                var binaryExpression = (BinaryExpression)chain;
                return binaryExpression.Left;
            case ExpressionType.MemberAccess:
                var memberExpression = (MemberExpression)chain;
                return memberExpression.Expression;
            default:
                throw new InvalidOperationException();
            }
        }

        private static bool IsNullableOrReferenceTypeAccess(Expression chain)
        {
            var left = GetLeft(chain);
            return !left.Type.IsValueType || (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        private static Expression Coalesce(Expression node)
        {
            if(node == null)
                return null;
            if(node.NodeType == ExpressionType.New)
                return node;
            if(node.Type.GetDefaultValue() != null)
                return node;
            var valueForCoalesce = GetValueForCoalesce(node.Type);
            if(valueForCoalesce == null)
                return node;
            return Expression.MakeBinary(ExpressionType.Coalesce, node, Expression.Constant(valueForCoalesce, node.Type));
        }

        private static bool IsNull(Expression exp)
        {
            return exp != null &&
                   (exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value == null
                    || exp.NodeType == ExpressionType.Convert && IsNull(((UnaryExpression)exp).Operand)
                    || exp.NodeType == ExpressionType.ConvertChecked && IsNull(((UnaryExpression)exp).Operand));
        }
    }
}