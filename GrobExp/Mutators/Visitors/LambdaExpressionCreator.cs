using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;

using GrobExp.Compiler;

namespace GrobExp.Mutators.Visitors
{
    public static class LambdaExpressionCreator
    {
        public static LambdaExpression Create(Expression body, params ParameterExpression[] parameters)
        {
            if(parameters.Any(parameter => parameter.IsByRef))
                return Expression.Lambda(body, parameters);
            var parameterz = new ParameterExpression[parameters.Length];
            Array.Copy(parameters, parameterz, parameters.Length);
            return GetLambdaFactory(body.Type, parameters)(body, new ReadOnlyCollection<ParameterExpression>(parameterz));
        }

        public static Expression<TDelegate> Create<TDelegate>(Expression body, params ParameterExpression[] parameters)
        {
            if(parameters.Any(parameter => parameter.IsByRef))
                return Expression.Lambda<TDelegate>(body, parameters);
            var parameterz = new ParameterExpression[parameters.Length];
            Array.Copy(parameters, parameterz, parameters.Length);
            return (Expression<TDelegate>)GetLambdaFactory(typeof(TDelegate))(body, new ReadOnlyCollection<ParameterExpression>(parameterz));
        }

        private static Func<Expression, ReadOnlyCollection<ParameterExpression>, LambdaExpression> GetLambdaFactory(Type returnType, ParameterExpression[] parameters)
        {
            return GetLambdaFactory(Extensions.GetDelegateType(parameters.Select(parameter => parameter.Type).ToArray(), returnType));
        }

        private static Func<Expression, ReadOnlyCollection<ParameterExpression>, LambdaExpression> GetLambdaFactory(Type delegateType)
        {
            var factory = (Func<Expression, ReadOnlyCollection<ParameterExpression>, LambdaExpression>)factories[delegateType];
            if(factory == null)
            {
                lock(factoriesLock)
                {
                    factory = (Func<Expression, ReadOnlyCollection<ParameterExpression>, LambdaExpression>)factories[delegateType];
                    if(factory == null)
                    {
                        factory = BuildLambdaFactory(delegateType);
                        factories[delegateType] = factory;
                    }
                }
            }
            return factory;
        }

        private static Func<Expression, ReadOnlyCollection<ParameterExpression>, LambdaExpression> BuildLambdaFactory(Type delegateType)
        {
            var resultType = typeof(Expression<>).MakeGenericType(delegateType);
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(LambdaExpression), new[] {typeof(Expression), typeof(ReadOnlyCollection<ParameterExpression>)}, typeof(LambdaExpressionCreator), true);
            var il = new GroboIL(method);
            il.Ldarg(0);
            il.Ldnull(typeof(string));
            il.Ldc_I4(0);
            il.Ldarg(1);
            il.Newobj(resultType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single());
            il.Ret();
            return (Func<Expression, ReadOnlyCollection<ParameterExpression>, LambdaExpression>)method.CreateDelegate(typeof(Func<Expression, ReadOnlyCollection<ParameterExpression>, LambdaExpression>));
        }

        private static readonly Hashtable factories = new Hashtable();
        private static readonly object factoriesLock = new object();
    }
}