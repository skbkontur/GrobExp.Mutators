using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.ExpressionEmitters
{
    internal class LambdaExpressionEmitter : ExpressionEmitter<LambdaExpression>
    {
        protected override bool Emit(LambdaExpression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType)
        {
            var compiledLambda = LambdaCompiler.Compile(Expression.Lambda(node.Body, new[] {context.ClosureParameter}.Concat(node.Parameters)), context.Options, context.CompiledLambdas);
            var parameterTypes = node.Parameters.Select(parameter => parameter.Type).ToArray();
            Type delegateType = Extensions.GetDelegateType(new[] {typeof(Closure)}.Concat(parameterTypes).ToArray(), node.ReturnType);
            Type closureType;
            ExpressionEmittersCollection.Emit(context.ClosureParameter, context, out closureType);
            context.CompiledLambdas.Add(new CompiledLambda {Delegate = WrapDelegate(compiledLambda.Delegate, delegateType, closureType), ILCode = compiledLambda.ILCode});
            context.Il.Ldfld(Closure.DelegatesField); // stack: [closure.delegates]
            context.Il.Ldc_I4(context.CompiledLambdas.Count - 1); // stack: [closure.delegates, index]
            context.Il.Ldelem(typeof(Delegate)); // stack: [closure.delegates[index]]
            ExpressionEmittersCollection.Emit(context.ClosureParameter, context, out closureType); // stack: [closure.delegates[index], closure]
            resultType = Extensions.GetDelegateType(parameterTypes, node.ReturnType);
            Type type = Extensions.GetDelegateType(new[] {closureType}, resultType);
            context.Il.Call(type.GetMethod("Invoke", new[] {closureType}), type); // stack: [closure.delegates[index](closure)]
            return false;
        }

        private static Delegate WrapDelegate(Delegate @delegate, Type delegateType, Type closureType)
        {
            if(!delegateType.IsGenericType)
                throw new NotSupportedException("Unable to wrap a delegate of type '" + delegateType + "'");
            var genericTypeDefinition = delegateType.GetGenericTypeDefinition();
            var genericTypeArguments = new[] {closureType}.Concat(delegateType.GetGenericArguments().Skip(1)).ToArray();
            MethodInfo method;
            if(genericTypeDefinition == typeof(Func<,>))
                method = DelegateWrapper.wrapFunc2Method.MakeGenericMethod(genericTypeArguments);
            else if(genericTypeDefinition == typeof(Func<,,>))
                method = DelegateWrapper.wrapFunc3Method.MakeGenericMethod(genericTypeArguments);
            else if(genericTypeDefinition == typeof(Func<,,,>))
                method = DelegateWrapper.wrapFunc4Method.MakeGenericMethod(genericTypeArguments);
            else if(genericTypeDefinition == typeof(Func<,,,,>))
                method = DelegateWrapper.wrapFunc5Method.MakeGenericMethod(genericTypeArguments);
            else if(genericTypeDefinition == typeof(Action<>))
                method = DelegateWrapper.wrapAction1Method;
            else if(genericTypeDefinition == typeof(Action<,>))
                method = DelegateWrapper.wrapAction2Method.MakeGenericMethod(genericTypeArguments);
            else if(genericTypeDefinition == typeof(Action<,,>))
                method = DelegateWrapper.wrapAction3Method.MakeGenericMethod(genericTypeArguments);
            else if(genericTypeDefinition == typeof(Action<,,,>))
                method = DelegateWrapper.wrapAction4Method.MakeGenericMethod(genericTypeArguments);
            else
                throw new NotSupportedException("Unable to wrap a delegate of type '" + delegateType + "'");
            return (Delegate)method.Invoke(null, new object[] {@delegate});
        }
    }
}