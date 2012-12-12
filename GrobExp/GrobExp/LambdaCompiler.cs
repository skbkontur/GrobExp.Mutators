using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace GrobExp
{
    public static class LambdaCompiler
    {
        public static Func<TResult> Compile<TResult>(Expression<Func<TResult>> lambda, CompilerOptions options = CompilerOptions.All)
        {
            Delegate[] delegates;
            var func = (Func<Delegate[], TResult>)Compile(lambda, options, out delegates);
            return () => func(delegates);
        }

        public static Action Compile(Expression<Action> lambda, CompilerOptions options = CompilerOptions.All)
        {
            Delegate[] delegates;
            var action = (Action<Delegate[]>)Compile(lambda, options, out delegates);
            return () => action(delegates);
        }

        public static Func<T, TResult> Compile<T, TResult>(Expression<Func<T, TResult>> lambda, CompilerOptions options = CompilerOptions.All)
        {
            Delegate[] delegates;
            var func = (Func<Delegate[], T, TResult>)Compile(lambda, options, out delegates);
            return arg => func(delegates, arg);
        }

        public static Action<T> Compile<T>(Expression<Action<T>> lambda, CompilerOptions options = CompilerOptions.All)
        {
            Delegate[] delegates;
            var action = (Action<Delegate[], T>)Compile(lambda, options, out delegates);
            return arg => action(delegates, arg);
        }

        public static Func<T1, T2, TResult> Compile<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> lambda, CompilerOptions options = CompilerOptions.All)
        {
            Delegate[] delegates;
            var func = (Func<Delegate[], T1, T2, TResult>)Compile(lambda, options, out delegates);
            return (arg1, arg2) => func(delegates, arg1, arg2);
        }

        public static Action<T1, T2> Compile<T1, T2>(Expression<Action<T1, T2>> lambda, CompilerOptions options = CompilerOptions.All)
        {
            Delegate[] delegates;
            var action = (Action<Delegate[], T1, T2>)Compile(lambda, options, out delegates);
            return (arg1, arg2) => action(delegates, arg1, arg2);
        }

        public static Func<T1, T2, T3, TResult> Compile<T1, T2, T3, TResult>(Expression<Func<T1, T2, T3, TResult>> lambda, CompilerOptions options = CompilerOptions.All)
        {
            Delegate[] delegates;
            var func = (Func<Delegate[], T1, T2, T3, TResult>)Compile(lambda, options, out delegates);
            return (arg1, arg2, arg3) => func(delegates, arg1, arg2, arg3);
        }

        public static Action<T1, T2, T3> Compile<T1, T2, T3>(Expression<Action<T1, T2, T3>> lambda, CompilerOptions options = CompilerOptions.All)
        {
            Delegate[] delegates;
            var action = (Action<Delegate[], T1, T2, T3>)Compile(lambda, options, out delegates);
            return (arg1, arg2, arg3) => action(delegates, arg1, arg2, arg3);
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

        private static Delegate Compile(LambdaExpression lambda, CompilerOptions options, out Delegate[] delegates)
        {
            var compiledLambdas = new List<CompiledLambda>();
            ParameterExpression closureParameter;
            lambda = new ExpressionClosureResolver(lambda).Resolve(out closureParameter);
            var result = Compile(lambda, closureParameter, options, compiledLambdas);
            delegates = compiledLambdas.Select(compiledLambda => compiledLambda.Delegate).ToArray();
            var ilCode = new StringBuilder(result.ILCode);
            for(int i = 0; i < compiledLambdas.Count; ++i)
            {
                ilCode.AppendLine();
                ilCode.AppendLine();
                ilCode.AppendLine();
                ilCode.AppendLine();
                ilCode.AppendLine("delegates[" + i + "]");
                ilCode.AppendLine(compiledLambdas[i].ILCode);
            }
            return result.Delegate;
        }

        private static CompiledLambda Compile(LambdaExpression lambda, CompilerOptions options, List<CompiledLambda> compiledLambdas)
        {
            return Compile(lambda, lambda.Parameters[0], options, compiledLambdas);
        }

        private static CompiledLambda Compile(LambdaExpression lambda, ParameterExpression closureParameter, CompilerOptions options, List<CompiledLambda> compiledLambdas)
        {
            var parameters = lambda.Parameters.ToArray();
            Type[] parameterTypes = parameters.Select(parameter => parameter.Type).ToArray();
            Type returnType = lambda.Body.Type;
            var method = new DynamicMethod(Guid.NewGuid().ToString(), returnType, parameterTypes, module, true);
            var il = new GrobIL(method.GetILGenerator(), true, returnType, parameterTypes);

            var context = new EmittingContext
                {
                    Options = options,
                    Parameters = parameters,
                    ClosureParameter = closureParameter,
                    CompiledLambdas = compiledLambdas,
                    Il = il
                };
            var returnDefaultValueLabel = il.DefineLabel("returnDefaultValue");
            Type resultType;
            bool labelUsed = Build(lambda.Body, context, returnDefaultValueLabel, false, false, out resultType);
            if(lambda.Body.Type == typeof(bool) && resultType == typeof(bool?))
                ConvertFromNullableBoolToBool(context);
            il.Ret();
            if(labelUsed && context.CanReturn)
            {
                il.MarkLabel(returnDefaultValueLabel);
                il.Pop();
                if(returnType != typeof(void))
                {
                    if(!returnType.IsValueType)
                        il.Ldnull();
                    else
                    {
                        using(var defaultValue = context.DeclareLocal(returnType))
                        {
                            il.Ldloca(defaultValue);
                            il.Initobj(returnType);
                            il.Ldloc(defaultValue);
                        }
                    }
                }
                il.Ret();
            }
            return new CompiledLambda
                {
                    Delegate = method.CreateDelegate(GetDelegateType(parameterTypes, returnType)),
                    ILCode = il.GetILCode()
                };
        }

        private static Type GetDelegateType(Type[] parameterTypes, Type returnType)
        {
            if(returnType == typeof(void))
            {
                switch(parameterTypes.Length)
                {
                case 0:
                    return typeof(Action);
                case 1:
                    return typeof(Action<>).MakeGenericType(parameterTypes);
                case 2:
                    return typeof(Action<,>).MakeGenericType(parameterTypes);
                case 3:
                    return typeof(Action<,,>).MakeGenericType(parameterTypes);
                case 4:
                    return typeof(Action<,,,>).MakeGenericType(parameterTypes);
                case 5:
                    return typeof(Action<,,,,>).MakeGenericType(parameterTypes);
                case 6:
                    return typeof(Action<,,,,,>).MakeGenericType(parameterTypes);
                default:
                    throw new NotSupportedException("Too many parameters for Action: " + parameterTypes.Length);
                }
            }
            parameterTypes = parameterTypes.Concat(new[] {returnType}).ToArray();
            switch(parameterTypes.Length)
            {
            case 1:
                return typeof(Func<>).MakeGenericType(parameterTypes);
            case 2:
                return typeof(Func<,>).MakeGenericType(parameterTypes);
            case 3:
                return typeof(Func<,,>).MakeGenericType(parameterTypes);
            case 4:
                return typeof(Func<,,,>).MakeGenericType(parameterTypes);
            case 5:
                return typeof(Func<,,,,>).MakeGenericType(parameterTypes);
            case 6:
                return typeof(Func<,,,,,>).MakeGenericType(parameterTypes);
            default:
                throw new NotSupportedException("Too many parameters for Func: " + parameterTypes.Length);
            }
        }

        private static bool IsNullable(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private static bool EmitNullChecking(Type type, EmittingContext context, GrobIL.Label objIsNullLabel)
        {
            var il = context.Il;
            if(!type.IsValueType)
            {
                il.Dup(); // stack: [obj, obj]
                il.Brfalse(objIsNullLabel); // if(obj == null) goto returnDefaultValue; stack: [obj]
                return true;
            }
            if(IsNullable(type))
            {
                il.Dup();
                Type memberType;
                EmitMemberAccess(type, type.GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance), context, false, out memberType);
                il.Brfalse(objIsNullLabel);
                return true;
            }
            return false;
        }

        private static bool Build(Expression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, bool returnByRef, bool extend)
        {
            Type resultType;
            return Build(node, context, returnDefaultValueLabel, returnByRef, extend, out resultType);
        }

        private static bool Build(Expression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType)
        {
            switch(node.NodeType)
            {
            case ExpressionType.Parameter:
                BuildParameter((ParameterExpression)node, context, returnByRef, out resultType);
                return false;
            case ExpressionType.MemberAccess:
                return BuildMemberAccess((MemberExpression)node, context, returnDefaultValueLabel, returnByRef, extend, out resultType);
            case ExpressionType.Convert:
                return BuildConvert((UnaryExpression)node, context, returnDefaultValueLabel, extend, out resultType);
            case ExpressionType.Constant:
                BuildConstant((ConstantExpression)node, context, out resultType);
                return false;
            case ExpressionType.ArrayIndex:
                return BuildArrayIndex((BinaryExpression)node, context, returnDefaultValueLabel, returnByRef, extend, out resultType);
            case ExpressionType.ArrayLength:
                return BuildArrayLength((UnaryExpression)node, context, returnDefaultValueLabel, out resultType);
            case ExpressionType.Call:
                return BuildMethodCall((MethodCallExpression)node, context, returnDefaultValueLabel, extend, out resultType);
            case ExpressionType.Block:
                BuildBlock((BlockExpression)node, context, out resultType);
                return false;
            case ExpressionType.Assign:
                BuildAssign((BinaryExpression)node, context, out resultType);
                return false;
            case ExpressionType.New:
                BuildNew((NewExpression)node, context, out resultType);
                return false;
            case ExpressionType.Conditional:
                return BuildConditional((ConditionalExpression)node, context, returnDefaultValueLabel, returnByRef, extend, out resultType);
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
                BuildEqualNotEqual((BinaryExpression)node, context, out resultType);
                return false;
            case ExpressionType.GreaterThan:
            case ExpressionType.LessThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LessThanOrEqual:
                BuildComparison((BinaryExpression)node, context, out resultType);
                return false;
            case ExpressionType.Add:
            case ExpressionType.Subtract:
            case ExpressionType.Multiply:
            case ExpressionType.Divide:
            case ExpressionType.Modulo:
                BuildArithmetic((BinaryExpression)node, context, out resultType);
                return false;
            case ExpressionType.OrElse:
            case ExpressionType.AndAlso:
                BuildLogical((BinaryExpression)node, context, out resultType);
                return false;
            case ExpressionType.Not:
                BuildLogical((UnaryExpression)node, context, out resultType);
                return false;
            case ExpressionType.MemberInit:
                BuildMemberInit((MemberInitExpression)node, context, out resultType);
                return false;
            case ExpressionType.NewArrayInit:
                BuildNewArrayInit((NewArrayExpression)node, context, out resultType);
                return false;
            case ExpressionType.NewArrayBounds:
                BuildNewArrayBounds((NewArrayExpression)node, context, out resultType);
                return false;
            case ExpressionType.Default:
                BuildDefault((DefaultExpression)node, context, out resultType);
                return false;
            case ExpressionType.Coalesce:
                return BuildCoalesce((BinaryExpression)node, context, returnDefaultValueLabel, out resultType);
            case ExpressionType.Lambda:
                BuildLambda((LambdaExpression)node, context, out resultType);
                return false;
            case ExpressionType.Label:
                return BuildLabel((LabelExpression)node, context, returnDefaultValueLabel, out resultType);
            case ExpressionType.Goto:
                return BuildGoto((GotoExpression)node, context, returnDefaultValueLabel, out resultType);
            default:
                throw new NotSupportedException("Node type '" + node.NodeType + "' is not supported");
            }
        }

        private static bool BuildGoto(GotoExpression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, out Type resultType)
        {
            bool result = false;
            resultType = typeof(void);
            if(node.Value != null)
                result = Build(node.Value, context, returnDefaultValueLabel, false, false, out resultType);
            GrobIL.Label label;
            if(!context.Labels.TryGetValue(node.Target, out label))
                context.Labels.Add(node.Target, label = context.Il.DefineLabel(node.Target.Name));
            context.Il.Br(label);
            return result;
        }

        private static bool BuildLabel(LabelExpression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, out Type resultType)
        {
            bool result = false;
            resultType = typeof(void);
            if(node.DefaultValue != null)
                result = Build(node.DefaultValue, context, returnDefaultValueLabel, false, false, out resultType);
            GrobIL.Label label;
            if(!context.Labels.TryGetValue(node.Target, out label))
                context.Labels.Add(node.Target, label = context.Il.DefineLabel(node.Target.Name));
            context.Il.MarkLabel(label);
            return result;
        }

        private static bool BuildArrayLength(UnaryExpression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, out Type resultType)
        {
            var il = context.Il;
            var result = Build(node.Operand, context, returnDefaultValueLabel, false, false);
            if(context.Options.HasFlag(CompilerOptions.CheckNullReferences))
            {
                result = true;
                il.Dup();
                il.Brfalse(returnDefaultValueLabel);
            }
            il.Ldlen();
            resultType = typeof(int);
            return result;
        }

        private static void BuildLambda(LambdaExpression node, EmittingContext context, out Type resultType)
        {
            var compiledLambda = Compile(Expression.Lambda(node.Body, new[] {context.ClosureParameter}.Concat(node.Parameters)), context.Options, context.CompiledLambdas);
            var parameterTypes = node.Parameters.Select(parameter => parameter.Type).ToArray();
            Type delegateType = GetDelegateType(new[] {typeof(Closure)}.Concat(parameterTypes).ToArray(), node.ReturnType);
            Type closureType;
            BuildParameter(context.ClosureParameter, context, false, out closureType);
            context.CompiledLambdas.Add(new CompiledLambda {Delegate = WrapDelegate(compiledLambda.Delegate, delegateType, closureType), ILCode = compiledLambda.ILCode});
            context.Il.Ldfld(Closure.DelegatesField); // stack: [closure.delegates]
            context.Il.Ldc_I4(context.CompiledLambdas.Count - 1); // stack: [closure.delegates, index]
            context.Il.Ldelem(typeof(Delegate)); // stack: [closure.delegates[index]]
            BuildParameter(context.ClosureParameter, context, false, out closureType); // stack: [closure.delegates[index], closure]
            resultType = GetDelegateType(parameterTypes, node.ReturnType);
            Type type = GetDelegateType(new[] {closureType}, resultType);
            context.Il.Call(type.GetMethod("Invoke", new[] {closureType}), type); // stack: [closure.delegates[index](closure)]
        }

        private static bool BuildCoalesce(BinaryExpression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, out Type resultType)
        {
            if(node.Conversion != null)
                throw new NotSupportedException("Coalesce with conversion is not supported");
            // note ich: баг решарпера
            // ReSharper disable HeuristicUnreachableCode
            var left = node.Left;
            var right = node.Right;
            GrobIL il = context.Il;
            GrobIL.Label valueIsNullLabel = context.CanReturn ? il.DefineLabel("valueIsNull") : null;
            bool labelUsed = Build(left, context, valueIsNullLabel, false, false);
            if(left.Type.IsValueType)
            {
                using(var temp = context.DeclareLocal(left.Type))
                {
                    il.Stloc(temp);
                    il.Ldloca(temp);
                }
            }
            labelUsed |= EmitNullChecking(left.Type, context, valueIsNullLabel);
            if(left.Type.IsValueType)
            {
                if(!IsNullable(left.Type))
                    throw new InvalidOperationException("Type '" + left.Type + "' cannot be null");
                il.Ldfld(left.Type.GetField("value", BindingFlags.NonPublic | BindingFlags.Instance));
            }
            var valueIsNotNullLabel = il.DefineLabel("valueIsNotNull");
            il.Br(valueIsNotNullLabel);
            if(labelUsed)
            {
                il.MarkLabel(valueIsNullLabel);
                il.Pop();
            }
            var result = Build(right, context, returnDefaultValueLabel, false, false);
            il.MarkLabel(valueIsNotNullLabel);
            resultType = node.Type;
            return result;
            // ReSharper restore HeuristicUnreachableCode
        }

        private static void BuildDefault(DefaultExpression node, EmittingContext context, out Type resultType)
        {
            resultType = node.Type;
            if(node.Type == typeof(void))
                return;
            EmitLoadDefaultValue(context, node.Type);
        }

        private static void BuildNewArrayBounds(NewArrayExpression node, EmittingContext context, out Type resultType)
        {
            var il = context.Il;

            GrobIL.Label lengthIsNullLabel = context.CanReturn ? il.DefineLabel("lengthIsNull") : null;
            var labelUsed = Build(node.Expressions.Single(), context, lengthIsNullLabel, false, false);
            if(labelUsed && context.CanReturn)
            {
                var lengthIsNotNullLabel = il.DefineLabel("lengthIsNotNull");
                il.Br(lengthIsNotNullLabel);
                il.MarkLabel(lengthIsNullLabel);
                il.Pop();
                il.Ldc_I4(0);
                il.MarkLabel(lengthIsNotNullLabel);
            }
            il.Newarr(node.Type.GetElementType());
            resultType = node.Type;
        }

        private static void BuildNewArrayInit(NewArrayExpression node, EmittingContext context, out Type resultType)
        {
            var il = context.Il;
            Type elementType = node.Type.GetElementType();
            il.Ldc_I4(node.Expressions.Count); // stack: [length]
            il.Newarr(elementType); // stack: [new type[length]]
            for(int index = 0; index < node.Expressions.Count; index++)
            {
                var expression = node.Expressions[index];
                il.Dup();
                il.Ldc_I4(index);
                if(elementType.IsValueType && !elementType.IsPrimitive)
                {
                    il.Ldelema(elementType);
                    BuildArguments(context, expression);
                    il.Stobj(elementType);
                }
                else
                {
                    BuildArguments(context, expression);
                    il.Stelem(elementType);
                }
            }
            resultType = node.Type;
        }

        private static void BuildMemberInit(MemberInitExpression node, EmittingContext context, out Type resultType)
        {
            BuildNew(node.NewExpression, context, out resultType); // stack: [new obj(args)]
            GrobIL il = context.Il;
            if(!node.Type.IsValueType)
            {
                foreach(MemberAssignment assignment in node.Bindings)
                {
                    il.Dup();
                    BuildArguments(context, assignment.Expression);
                    EmitMemberAssign(node.Type, assignment.Member, il);
                }
            }
            else
            {
                using(var temp = context.DeclareLocal(node.Type))
                {
                    il.Stloc(temp);
                    il.Ldloca(temp);
                    foreach(MemberAssignment assignment in node.Bindings)
                    {
                        il.Dup();
                        BuildArguments(context, assignment.Expression);
                        EmitMemberAssign(node.Type, assignment.Member, il);
                    }
                    // todo или il.Ldobj()?
                    il.Pop();
                    il.Ldloc(temp);
                }
            }
        }

        private static void BuildLogical(BinaryExpression node, EmittingContext context, out Type resultType)
        {
            GrobIL il = context.Il;
            if(node.Method != null)
                throw new NotSupportedException("Custom operator '" + node.NodeType + "' is not supported");
            Expression left = node.Left;
            Expression right = node.Right;
            Type leftType;
            BuildArgument(context, left, false, out leftType); // stack: [left]
            if(leftType == typeof(bool))
            {
                switch(node.NodeType)
                {
                case ExpressionType.AndAlso:
                    {
                        var returnFalseLabel = il.DefineLabel("returnFalse");
                        il.Brfalse(returnFalseLabel); // if(left == false) return false; stack: []
                        Type rightType;
                        BuildArgument(context, right, false, out rightType); // stack: [right]
                        var doneLabel = il.DefineLabel("done");
                        il.Br(doneLabel); // goto done; stack: [right]
                        il.MarkLabel(returnFalseLabel);
                        il.Ldc_I4(0); // stack: [false]
                        if(rightType == typeof(bool?))
                            il.Newobj(nullableBoolConstructor); // stack: [new bool?(false)]
                        il.MarkLabel(doneLabel);
                        resultType = rightType;
                        break;
                    }
                case ExpressionType.OrElse:
                    {
                        var returnTrueLabel = il.DefineLabel("returnTrue");
                        il.Brtrue(returnTrueLabel); // if(left == true) return true; stack: []
                        Type rightType;
                        BuildArgument(context, right, false, out rightType); // stack: [right]
                        var doneLabel = il.DefineLabel("done");
                        il.Br(doneLabel); // goto done; stack: [right]
                        il.MarkLabel(returnTrueLabel);
                        il.Ldc_I4(1); // stack: [true]
                        if(rightType == typeof(bool?))
                            il.Newobj(nullableBoolConstructor); // stack: [new bool?(true)]
                        il.MarkLabel(doneLabel);
                        resultType = rightType;
                        break;
                    }
                default:
                    throw new NotSupportedException("Node type '" + node.NodeType + "' is not supported");
                }
            }
            else // bool?
            {
                switch(node.NodeType)
                {
                case ExpressionType.AndAlso:
                    {
                        /*
                                     * +-------+-------+-------+-------+
                                     * |  &&   | null  | false | true  |
                                     * +-------+-------+-------+-------+
                                     * | null  | null  | false | null  |
                                     * +-------+-------+-------+-------+
                                     * | false | false | false | false |
                                     * +-------+-------+-------+-------+
                                     * | true  | null  | false | true  |
                                     * +-------+-------+-------+-------+
                                 */
                        using(var localLeft = context.DeclareLocal(typeof(bool?)))
                        {
                            il.Stloc(localLeft); // localLeft = left; stack: []
                            il.Ldloca(localLeft); // stack: [&localLeft]
                            il.Ldfld(nullableBoolHasValueField); // stack: [localLeft.HasValue]
                            il.Ldc_I4(1); // stack: [localLeft.HasValue, true]
                            il.Xor(); // stack: [!localLeft.HasValue]
                            il.Ldloca(localLeft); // stack: [!localLeft.HasValue, &localLeft]
                            il.Ldfld(nullableBoolValueField); // stack: [!localLeft.HasValue, localLeft.Value]
                            il.Or(); // stack: [!localLeft.HasValue || localLeft.Value]
                            var returnFalseLabel = il.DefineLabel("returnFalse");
                            il.Brfalse(returnFalseLabel); // if(localLeft == false) goto returnFalse; stack: []
                            Type rightType;
                            BuildArgument(context, right, false, out rightType); // stack: [right]
                            using(var localRight = context.DeclareLocal(rightType))
                            {
                                il.Stloc(localRight); // localRight = right; stack: []
                                if(rightType == typeof(bool))
                                    il.Ldloc(localRight); // stack: [localRight]
                                else
                                {
                                    il.Ldloca(localRight); // stack: [&localRight]
                                    il.Ldfld(nullableBoolHasValueField); // stack: [localRight.HasValue]
                                    il.Ldc_I4(1); // stack: [localRight.HasValue, true]
                                    il.Xor(); // stack: [!localRight.HasValue]
                                    il.Ldloca(localRight); // stack: [!localRight.HasValue, &localRight]
                                    il.Ldfld(nullableBoolValueField); // stack: [!localRight.HasValue, localRight.Value]
                                    il.Or(); // stack: [!localRight.HasValue || localRight.Value]
                                }
                                il.Brfalse(returnFalseLabel); // if(localRight == false) goto returnFalse;
                                if(rightType == typeof(bool))
                                    il.Ldloc(localRight); // stack: [localRight]
                                else
                                {
                                    il.Ldloca(localRight); // stack: [&localRight]
                                    il.Ldfld(nullableBoolHasValueField); // stack: [localRight.HasValue]
                                    il.Ldloca(localRight); // stack: [localRight.HasValue, &localRight]
                                    il.Ldfld(nullableBoolValueField); // stack: [localRight.HasValue, localRight.Value]
                                    il.And(); // stack: [localRight.HasValue && localRight.Value]
                                }
                                var returnLeftLabel = il.DefineLabel("returnLeft");
                                il.Brtrue(returnLeftLabel); // if(localRight == true) goto returnLeft;
                                il.Ldloca(localLeft); // stack: [&localLeft]
                                il.Initobj(typeof(bool?)); // localLeft = default(bool?); stack: []
                                il.MarkLabel(returnLeftLabel);
                                il.Ldloc(localLeft); // stack: [localLeft]
                                var doneLabel = il.DefineLabel("done");
                                il.Br(doneLabel);
                                il.MarkLabel(returnFalseLabel);
                                il.Ldc_I4(0); // stack: [false]
                                il.Newobj(nullableBoolConstructor); // new bool?(false)
                                il.MarkLabel(doneLabel);
                                resultType = typeof(bool?);
                            }
                        }
                        break;
                    }
                case ExpressionType.OrElse:
                    {
                        /*
                                     * +-------+-------+-------+-------+
                                     * |  ||   | null  | false | true  |
                                     * +-------+-------+-------+-------+
                                     * | null  | null  | null  | true  |
                                     * +-------+-------+-------+-------+
                                     * | false | null  | false | true  |
                                     * +-------+-------+-------+-------+
                                     * | true  | true  | true  | true  |
                                     * +-------+-------+-------+-------+
                                 */
                        using(var localLeft = context.DeclareLocal(typeof(bool?)))
                        {
                            il.Stloc(localLeft); // localLeft = left; stack: []
                            il.Ldloca(localLeft); // stack: [&localLeft]
                            il.Ldfld(nullableBoolHasValueField); // stack: [localLeft.HasValue]
                            il.Ldloca(localLeft); // stack: [localLeft.HasValue, &localLeft]
                            il.Ldfld(nullableBoolValueField); // stack: [localLeft.HasValue, localLeft.Value]
                            il.And(); // stack: [localLeft.HasValue && localLeft.Value]
                            var returnTrueLabel = il.DefineLabel("returnTrue");
                            il.Brtrue(returnTrueLabel); // if(localLeft == true) goto returnTrue; stack: []
                            Type rightType;
                            BuildArgument(context, right, false, out rightType); // stack: [right]
                            using(var localRight = context.DeclareLocal(rightType))
                            {
                                il.Stloc(localRight); // localRight = right; stack: []
                                if(rightType == typeof(bool))
                                    il.Ldloc(localRight); // stack: [localRight]
                                else
                                {
                                    il.Ldloca(localRight); // stack: [&localRight]
                                    il.Ldfld(nullableBoolHasValueField); // stack: [localRight.HasValue]
                                    il.Ldloca(localRight); // stack: [localRight.HasValue, &localRight]
                                    il.Ldfld(nullableBoolValueField); // stack: [localRight.HasValue, localRight.Value]
                                    il.And(); // stack: [localRight.HasValue && localRight.Value]
                                }
                                il.Brtrue(returnTrueLabel); // if(localRight == true) goto returnTrue; stack: []
                                if(rightType == typeof(bool))
                                    il.Ldloc(localRight); // stack: [localRight]
                                else
                                {
                                    il.Ldloca(localRight); // stack: [&localRight]
                                    il.Ldfld(nullableBoolHasValueField); // stack: [localRight.HasValue]
                                    il.Ldc_I4(1); // stack: [localRight.HasValue, true]
                                    il.Xor(); // stack: [!localRight.HasValue]
                                    il.Ldloca(localRight); // stack: [!localRight.HasValue, &localRight]
                                    il.Ldfld(nullableBoolValueField); // stack: [!localRight.HasValue, localRight.Value]
                                    il.Or(); // stack: [!localRight.HasValue || localRight.Value]
                                }
                                var returnLeftLabel = il.DefineLabel("returnLeft");
                                il.Brfalse(returnLeftLabel); // if(localRight == false) goto returnLeft;
                                il.Ldloca(localLeft); // stack: [&localLeft]
                                il.Initobj(typeof(bool?)); // localLeft = default(bool?); stack: []
                                il.MarkLabel(returnLeftLabel);
                                il.Ldloc(localLeft); // stack: [localLeft]
                                var doneLabel = il.DefineLabel("done");
                                il.Br(doneLabel);
                                il.MarkLabel(returnTrueLabel);
                                il.Ldc_I4(1); // stack: [true]
                                il.Newobj(nullableBoolConstructor); // new bool?(true)
                                il.MarkLabel(doneLabel);
                                resultType = typeof(bool?);
                            }
                        }
                        break;
                    }
                default:
                    throw new NotSupportedException("Node type '" + node.NodeType + "' is not supported");
                }
            }
        }

        private static void BuildLogical(UnaryExpression node, EmittingContext context, out Type resultType)
        {
            GrobIL il = context.Il;
            if(node.Method != null)
                throw new NotSupportedException("Custom operator '" + node.NodeType + "' is not supported");
            var operand = node.Operand;

            BuildArgument(context, operand, false, out resultType);
            if(resultType == typeof(bool))
            {
                il.Ldc_I4(1);
                il.Xor();
            }
            else if(resultType == typeof(bool?))
            {
                using(var value = context.DeclareLocal(typeof(bool?)))
                {
                    il.Stloc(value);
                    il.Ldloca(value);
                    il.Ldfld(nullableBoolHasValueField);
                    var returnLabel = il.DefineLabel("return");
                    il.Brfalse(returnLabel);
                    il.Ldloca(value);
                    il.Ldfld(nullableBoolValueField);
                    il.Ldc_I4(1);
                    il.Xor();
                    il.Newobj(nullableBoolConstructor);
                    il.Stloc(value);
                    il.MarkLabel(returnLabel);
                    il.Ldloc(value);
                }
            }
            else throw new InvalidOperationException("Cannot perform '" + node.NodeType + "' operator on type '" + resultType + "'");
        }

        private static void BuildArithmetic(BinaryExpression node, EmittingContext context, out Type resultType)
        {
            Expression left = node.Left;
            Expression right = node.Right;
            BuildArguments(context, left, right);
            GrobIL il = context.Il;
            if(node.Method != null)
                il.Call(node.Method);
            else
            {
                var type = left.Type;
                if(type != right.Type)
                    throw new InvalidOperationException("Cannot perform operation '" + node.NodeType + "' on objects of different types '" + left.Type + "' and '" + right.Type + "'");
                if(!IsNullable(type))
                {
                    switch(node.NodeType)
                    {
                    case ExpressionType.Add:
                        il.Add();
                        break;
                    case ExpressionType.Subtract:
                        il.Sub();
                        break;
                    case ExpressionType.Multiply:
                        il.Mul();
                        break;
                    case ExpressionType.Divide:
                        il.Div(type);
                        break;
                    case ExpressionType.Modulo:
                        il.Rem(type);
                        break;
                    default:
                        throw new InvalidOperationException();
                    }
                }
                else
                {
                    using(var localLeft = context.DeclareLocal(type))
                    using(var localRight = context.DeclareLocal(type))
                    {
                        il.Stloc(localRight);
                        il.Stloc(localLeft);
                        FieldInfo hasValueField = type.GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance);
                        il.Ldloca(localLeft);
                        il.Ldfld(hasValueField);
                        il.Ldloca(localRight);
                        il.Ldfld(hasValueField);
                        il.And();
                        var returnNullLabel = il.DefineLabel("returnNull");
                        il.Brfalse(returnNullLabel);
                        FieldInfo valueField = type.GetField("value", BindingFlags.NonPublic | BindingFlags.Instance);
                        il.Ldloca(localLeft);
                        il.Ldfld(valueField);
                        il.Ldloca(localRight);
                        il.Ldfld(valueField);

                        Type argument = type.GetGenericArguments()[0];
                        switch(node.NodeType)
                        {
                        case ExpressionType.Add:
                            il.Add();
                            break;
                        case ExpressionType.Subtract:
                            il.Sub();
                            break;
                        case ExpressionType.Multiply:
                            il.Mul();
                            break;
                        case ExpressionType.Divide:
                            il.Div(argument);
                            break;
                        case ExpressionType.Modulo:
                            il.Rem(argument);
                            break;
                        default:
                            throw new InvalidOperationException();
                        }
                        il.Newobj(type.GetConstructor(new[] {argument}));

                        var doneLabel = il.DefineLabel("done");
                        il.Br(doneLabel);
                        il.MarkLabel(returnNullLabel);
                        il.Ldloca(localLeft);
                        il.Initobj(type);
                        il.Ldloc(localLeft);
                        il.MarkLabel(doneLabel);
                    }
                }
            }
            resultType = node.Type;
        }

        private static void BuildComparison(BinaryExpression node, EmittingContext context, out Type resultType)
        {
            Expression left = node.Left;
            Expression right = node.Right;
            BuildArguments(context, left, right);
            GrobIL il = context.Il;
            if(node.Method != null)
            {
                il.Call(node.Method);
                resultType = node.Method.ReturnType;
            }
            else
            {
                var type = left.Type;
                if(type != right.Type)
                    throw new InvalidOperationException("Cannot compare objects of different types '" + left.Type + "' and '" + right.Type + "'");
                if(!IsNullable(type))
                {
                    switch(node.NodeType)
                    {
                    case ExpressionType.GreaterThan:
                        il.Cgt(type);
                        break;
                    case ExpressionType.LessThan:
                        il.Clt(type);
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        il.Clt(type);
                        il.Ldc_I4(1);
                        il.Xor();
                        break;
                    case ExpressionType.LessThanOrEqual:
                        il.Cgt(type);
                        il.Ldc_I4(1);
                        il.Xor();
                        break;
                    default:
                        throw new InvalidOperationException();
                    }
                    resultType = typeof(bool);
                }
                else
                {
                    if(!context.Options.HasFlag(CompilerOptions.UseTernaryLogic))
                    {
                        using(var localLeft = context.DeclareLocal(type))
                        using(var localRight = context.DeclareLocal(type))
                        {
                            il.Stloc(localRight);
                            il.Stloc(localLeft);
                            il.Ldloca(localLeft);
                            FieldInfo valueField = type.GetField("value", BindingFlags.NonPublic | BindingFlags.Instance);
                            il.Ldfld(valueField);
                            il.Ldloca(localRight);
                            il.Ldfld(valueField);
                            var returnFalseLabel = il.DefineLabel("returnFalse");

                            Type argument = type.GetGenericArguments()[0];
                            switch(node.NodeType)
                            {
                            case ExpressionType.GreaterThan:
                                il.Ble(argument, returnFalseLabel);
                                break;
                            case ExpressionType.LessThan:
                                il.Bge(argument, returnFalseLabel);
                                break;
                            case ExpressionType.GreaterThanOrEqual:
                                il.Blt(argument, returnFalseLabel);
                                break;
                            case ExpressionType.LessThanOrEqual:
                                il.Bgt(argument, returnFalseLabel);
                                break;
                            default:
                                throw new InvalidOperationException();
                            }
                            il.Ldloca(localLeft);
                            FieldInfo hasValueField = type.GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance);
                            il.Ldfld(hasValueField);
                            il.Ldloca(localRight);
                            il.Ldfld(hasValueField);
                            il.And();
                            var doneLabel = il.DefineLabel("done");
                            il.Br(doneLabel);
                            il.MarkLabel(returnFalseLabel);
                            il.Ldc_I4(0);
                            il.MarkLabel(doneLabel);
                            resultType = typeof(bool);
                        }
                    }
                    else
                    {
                        using(var localLeft = context.DeclareLocal(type))
                        using(var localRight = context.DeclareLocal(type))
                        {
                            il.Stloc(localRight);
                            il.Stloc(localLeft);
                            FieldInfo hasValueField = type.GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance);
                            il.Ldloca(localLeft);
                            il.Ldfld(hasValueField);
                            il.Ldloca(localRight);
                            il.Ldfld(hasValueField);
                            il.And();
                            var returnNullLabel = il.DefineLabel("returnNull");
                            il.Brfalse(returnNullLabel);
                            FieldInfo valueField = type.GetField("value", BindingFlags.NonPublic | BindingFlags.Instance);
                            il.Ldloca(localLeft);
                            il.Ldfld(valueField);
                            il.Ldloca(localRight);
                            il.Ldfld(valueField);
                            var argumentType = type.GetGenericArguments()[0];

                            switch(node.NodeType)
                            {
                            case ExpressionType.GreaterThan:
                                il.Cgt(argumentType);
                                break;
                            case ExpressionType.LessThan:
                                il.Clt(argumentType);
                                break;
                            case ExpressionType.GreaterThanOrEqual:
                                il.Clt(argumentType);
                                il.Ldc_I4(1);
                                il.Xor();
                                break;
                            case ExpressionType.LessThanOrEqual:
                                il.Cgt(argumentType);
                                il.Ldc_I4(1);
                                il.Xor();
                                break;
                            default:
                                throw new InvalidOperationException();
                            }
                            il.Newobj(nullableBoolConstructor);

                            var doneLabel = il.DefineLabel("done");
                            il.Br(doneLabel);
                            il.MarkLabel(returnNullLabel);
                            EmitLoadDefaultValue(context, typeof(bool?));
                            il.MarkLabel(doneLabel);
                            resultType = typeof(bool?);
                        }
                    }
                }
            }
        }

        private static bool IsStruct(Type type)
        {
            return type.IsValueType && !type.IsPrimitive && !type.IsEnum;
        }

        private static void BuildEqualNotEqual(BinaryExpression node, EmittingContext context, out Type resultType)
        {
            Expression left = node.Left;
            Expression right = node.Right;
            BuildArguments(context, left, right);
            GrobIL il = context.Il;
            if(!IsNullable(left.Type) && !IsNullable(right.Type))
            {
                if(node.Method != null)
                    il.Call(node.Method);
                else
                {
                    if(IsStruct(left.Type) || IsStruct(right.Type))
                        throw new InvalidOperationException("Cannot compare structs");
                    il.Ceq();
                    if(node.NodeType == ExpressionType.NotEqual)
                    {
                        il.Ldc_I4(1);
                        il.Xor();
                    }
                }
            }
            else
            {
                var type = left.Type;
                if(type != right.Type)
                    throw new InvalidOperationException("Cannot compare objects of different types '" + left.Type + "' and '" + right.Type + "'");
                using(var localLeft = context.DeclareLocal(type))
                using(var localRight = context.DeclareLocal(type))
                {
                    il.Stloc(localRight);
                    il.Stloc(localLeft);
                    if(node.Method != null)
                    {
                        FieldInfo hasValueField = type.GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance);
                        il.Ldloca(localLeft); // stack: [&left]
                        il.Ldfld(hasValueField); // stack: [left.HasValue]
                        il.Dup(); // stack: [left.HasValue, left.HasValue]
                        il.Ldloca(localRight); // stack: [left.HasValue, left.HasValue, &right]
                        il.Ldfld(hasValueField); // stack: [left.HasValue, left.HasValue, right.HasValue]
                        var notEqualLabel = il.DefineLabel("notEqual");
                        il.Bne(notEqualLabel); // stack: [left.HasValue]
                        var equalLabel = il.DefineLabel("equal");
                        il.Brfalse(equalLabel);
                        FieldInfo valueField = type.GetField("value", BindingFlags.NonPublic | BindingFlags.Instance);
                        il.Ldloca(localLeft);
                        il.Ldfld(valueField);
                        il.Ldloca(localRight);
                        il.Ldfld(valueField);
                        il.Call(node.Method);
                        var doneLabel = il.DefineLabel("done");
                        il.Br(doneLabel);
                        il.MarkLabel(notEqualLabel);
                        il.Pop();
                        il.Ldc_I4(node.NodeType == ExpressionType.Equal ? 0 : 1);
                        il.Br(doneLabel);
                        il.MarkLabel(equalLabel);
                        il.Ldc_I4(node.NodeType == ExpressionType.Equal ? 1 : 0);
                        il.MarkLabel(doneLabel);
                    }
                    else
                    {
                        il.Ldloca(localLeft);
                        FieldInfo valueField = type.GetField("value", BindingFlags.NonPublic | BindingFlags.Instance);
                        il.Ldfld(valueField);
                        il.Ldloca(localRight);
                        il.Ldfld(valueField);
                        var notEqualLabel = il.DefineLabel("notEqual");
                        il.Bne(notEqualLabel);
                        il.Ldloca(localLeft);
                        FieldInfo hasValueField = type.GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance);
                        il.Ldfld(hasValueField);
                        il.Ldloca(localRight);
                        il.Ldfld(hasValueField);
                        il.Ceq();
                        var doneLabel = il.DefineLabel("done");
                        il.Br(doneLabel);
                        il.MarkLabel(notEqualLabel);
                        il.Ldc_I4(0);
                        il.MarkLabel(doneLabel);
                        if(node.NodeType == ExpressionType.NotEqual)
                        {
                            il.Ldc_I4(1);
                            il.Xor();
                        }
                    }
                }
            }
            resultType = typeof(bool);
        }

        private static bool BuildConditional(ConditionalExpression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType)
        {
            var result = false;
            GrobIL il = context.Il;
            var testIsNullLabel = il.DefineLabel("testIsNull");
            Type testType;
            var testIsNullLabelUsed = Build(node.Test, context, testIsNullLabel, false, false, out testType);
            if(testType == typeof(bool?))
                ConvertFromNullableBoolToBool(context);
            var ifFalseLabel = il.DefineLabel("ifFalse");
            il.Brfalse(ifFalseLabel);
            result |= Build(node.IfTrue, context, returnDefaultValueLabel, returnByRef, extend, out resultType);
            var doneLabel = il.DefineLabel("done");
            il.Br(doneLabel);
            if(testIsNullLabelUsed)
            {
                il.MarkLabel(testIsNullLabel);
                il.Pop();
            }
            il.MarkLabel(ifFalseLabel);
            result |= Build(node.IfFalse, context, returnDefaultValueLabel, returnByRef, extend, out resultType);
            il.MarkLabel(doneLabel);
            return result;
        }

        private static void BuildNew(NewExpression node, EmittingContext context, out Type resultType)
        {
            BuildArguments(context, node.Arguments.ToArray());
            // note ich: баг решарпера
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            // ReSharper disable HeuristicUnreachableCode
            GrobIL il = context.Il;
            if(node.Constructor != null)
                il.Newobj(node.Constructor);
            else
            {
                if(node.Type.IsValueType)
                {
                    using(var temp = context.DeclareLocal(node.Type))
                    {
                        il.Ldloca(temp);
                        il.Initobj(node.Type);
                        il.Ldloc(temp);
                    }
                }
                else throw new InvalidOperationException("Missing constructor for type '" + node.Type + "'");
            }
            resultType = node.Type;
            // ReSharper restore ConditionIsAlwaysTrueOrFalse
            // ReSharper restore HeuristicUnreachableCode
        }

        private static void BuildAssign(BinaryExpression node, EmittingContext context, out Type resultType)
        {
            var il = context.Il;
            var left = node.Left;
            var right = node.Right;
            if(left.Type != right.Type)
                throw new InvalidOperationException("Unable to put object of type '" + right.Type + "' into object of type '" + left.Type + "'");
            switch(left.NodeType)
            {
            case ExpressionType.Parameter:
                {
                    var parameter = (ParameterExpression)left;
                    int index = Array.IndexOf(context.Parameters, parameter);
                    if(index >= 0)
                        il.Ldarga(index); // stack: [&parameter]
                    else
                    {
                        EmittingContext.LocalHolder variable;
                        if(context.VariablesToLocals.TryGetValue(parameter, out variable))
                            il.Ldloca(variable); // stack: [&variable]
                        else
                            throw new InvalidOperationException("Unknown parameter " + parameter);
                    }
                    var rightIsNullLabel = context.CanReturn ? il.DefineLabel("rightIsNull") : null;
                    Type valueType;
                    bool labelUsed = Build(right, context, rightIsNullLabel, false, false, out valueType); // stack: [address, value]
                    if(right.Type == typeof(bool) && valueType == typeof(bool?))
                        ConvertFromNullableBoolToBool(context);
                    if(labelUsed && context.CanReturn)
                        EmitReturnDefaultValue(context, right.Type, rightIsNullLabel, il.DefineLabel("rightIsNotNull"));
                    il.Stind(left.Type); // *address = value
                    BuildParameter(parameter, context, false, out resultType);
                    break;
                }
            case ExpressionType.MemberAccess:
                {
                    var memberExpression = (MemberExpression)left;
                    bool closureAssign = memberExpression.Expression == context.ClosureParameter;
                    var leftIsNullLabel = !closureAssign && context.CanReturn ? il.DefineLabel("leftIsNull") : null;
                    bool leftIsNullLabelUsed = false;
                    if(memberExpression.Expression == null) // static member
                    {
                        if(memberExpression.Member is FieldInfo)
                            il.Ldnull();
                    }
                    else
                    {
                        Type type;
                        leftIsNullLabelUsed = Build(memberExpression.Expression, context, leftIsNullLabel, true, context.Options.HasFlag(CompilerOptions.ExtendOnAssign), out type);
                        if(type.IsValueType)
                        {
                            using(var temp = context.DeclareLocal(type))
                            {
                                il.Stloc(temp);
                                il.Ldloca(temp);
                            }
                        }
                        if(!closureAssign && context.Options.HasFlag(CompilerOptions.CheckNullReferences))
                            leftIsNullLabelUsed |= EmitNullChecking(memberExpression.Expression.Type, context, leftIsNullLabel);
                    }
                    using(var temp = context.DeclareLocal(right.Type))
                    {
                        var rightIsNullLabel = context.CanReturn ? il.DefineLabel("rightIsNull") : null;
                        Type rightType;
                        var rightIsNullLabelUsed = Build(right, context, rightIsNullLabel, false, false, out rightType);
                        if(right.Type == typeof(bool) && rightType == typeof(bool?))
                            ConvertFromNullableBoolToBool(context);
                        if(rightIsNullLabelUsed && context.CanReturn)
                            EmitReturnDefaultValue(context, right.Type, rightIsNullLabel, il.DefineLabel("rightIsNotNull"));
                        il.Stloc(temp);
                        il.Ldloc(temp);
                        EmitMemberAssign(memberExpression.Expression == null ? null : memberExpression.Expression.Type, memberExpression.Member, il);
                        il.Ldloc(temp);
                    }
                    if(leftIsNullLabelUsed)
                    {
                        var doneLabel = il.DefineLabel("done");
                        il.Br(doneLabel);
                        il.MarkLabel(leftIsNullLabel);
                        il.Pop();
                        var rightIsNullLabel = context.CanReturn ? il.DefineLabel("rightIsNull") : null;
                        Type rightType;
                        var rightIsNullLabelUsed = Build(right, context, rightIsNullLabel, false, false, out rightType);
                        if(right.Type == typeof(bool) && rightType == typeof(bool?))
                            ConvertFromNullableBoolToBool(context);
                        if(rightIsNullLabelUsed && context.CanReturn)
                            EmitReturnDefaultValue(context, right.Type, rightIsNullLabel, il.DefineLabel("rightIsNotNull"));
                        il.MarkLabel(doneLabel);
                    }
                    resultType = right.Type;
                    break;
                }
                /*case ExpressionType.ArrayIndex:
                {
                    var binaryExpression = (BinaryExpression)node.Left;
                    var arrayIsNullLabel = context.CanReturn ? il.DefineLabel("arrayIsNull") : null;
                    Type type;
                    bool arrayIsNullLabelUsed = Build(binaryExpression.Left, context, arrayIsNullLabel, true, context.Options.HasFlag(GroboCompilerOptions.ExtendAssigns), out type);
                    if (context.Options.HasFlag(GroboCompilerOptions.CheckNullReferences))
                    {
                        arrayIsNullLabelUsed = true;
                        EmitNullChecking(type, context, arrayIsNullLabel);
                    }

                    GrobIL.Label indexIsNullLabel = context.CanReturn ? il.DefineLabel("indexIsNull") : null;
                    bool labelUsed = Build(node.Right, context, indexIsNullLabel, false, false); // stack: [array, index]
                    if (labelUsed && context.CanReturn)
                    {
                        var indexIsNotNullLabel = il.DefineLabel("indexIsNotNull");
                        il.Br(indexIsNotNullLabel);
                        il.MarkLabel(indexIsNullLabel);
                        il.Pop();
                        il.Ldc_I4(0);
                        il.MarkLabel(indexIsNotNullLabel);
                    }
                    if(context.Options.HasFlag(GroboCompilerOptions.ExtendAssigns))
                    {
                        // stack: [array, index]
                        using(var index = context.DeclareLocal(typeof(int)))
                        {
                            il.Dup(); // stack: [array, index, index]
                            il.Stloc(index); // index = index; stack: [array, index]
                            il.Ldc_I4(0); // stack: [array, index, 0]
                            il.Blt(typeof(int), arrayIsNullLabel); // stack: [array]
                            il.Dup(); // stack: [array, array]
                            il.Ldlen(); // stack: [array, array.Length]
                            il.Ldloc(index); // stack: [array, array.Length, index]
                            var assignLabel = il.DefineLabel("assign");
                            il.Bgt(typeof(int), assignLabel);
                            il.Pop();
                        }
                    }
                    else if (context.Options.HasFlag(GroboCompilerOptions.CheckArrayIndexes))
                    {
                        using (var index = context.DeclareLocal(typeof(int)))
                        {
                            il.Stloc(index); // index = index; stack: [array]
                            il.Dup(); // stack: [array, array]
                            il.Ldlen(); // stack: [array, array.Length]
                            il.Ldloc(index); // stack: [array, array.Length, index]
                            il.Ble(typeof(int), returnDefaultValueLabel); // if(array.Length <= index) goto returnDefaultValue; stack: [array]
                            il.Ldloc(index); // stack: [array, index]
                            il.Ldc_I4(0); // stack: [array, index, 0]
                            il.Blt(typeof(int), returnDefaultValueLabel); // if(index < 0) goto returnDefaultValue; stack: [array]     .
                            il.Ldloc(index); // stack: [array, index]
                        }
                    }
                    break;
                    }*/
            default:
                throw new InvalidOperationException("Unable to assign to an expression with node type '" + left.NodeType + "'");
            }
        }

        private static void BuildBlock(BlockExpression node, EmittingContext context, out Type resultType)
        {
            foreach(var variable in node.Variables)
            {
                context.VariablesToLocals.Add(variable, context.DeclareLocal(variable.Type));
                context.Variables.Push(variable);
            }
            resultType = typeof(void);
            for(int index = 0; index < node.Expressions.Count; index++)
            {
                var expression = node.Expressions[index];
                GrobIL il = context.Il;
                var valueIsNullLabel = il.DefineLabel("valueIsNull");
                bool labelUsed = Build(expression, context, valueIsNullLabel, false, false, out resultType);
                if(resultType != typeof(void) && index < node.Expressions.Count - 1)
                {
                    // eat results of all expressions except the last one
                    if(IsStruct(resultType))
                    {
                        using(var temp = context.DeclareLocal(resultType))
                            context.Il.Stloc(temp);
                    }
                    else context.Il.Pop();
                }
                if(labelUsed)
                {
                    var doneLabel = il.DefineLabel("done");
                    il.Br(doneLabel);
                    il.MarkLabel(valueIsNullLabel);
                    il.Pop();
                    if(resultType != typeof(void) && index == node.Expressions.Count - 1)
                    {
                        // return default value for the last expression in the block
                        EmitLoadDefaultValue(context, resultType);
                    }
                    il.MarkLabel(doneLabel);
                }
            }
            if(node.Type == typeof(bool) && resultType == typeof(bool?))
                ConvertFromNullableBoolToBool(context);
            else if(node.Type == typeof(void) && resultType != typeof(void))
            {
                // eat results of all expressions except the last one
                if(IsStruct(resultType))
                {
                    using(var temp = context.DeclareLocal(resultType))
                        context.Il.Stloc(temp);
                }
                else context.Il.Pop();
            }
            foreach(var variable in node.Variables)
            {
                context.VariablesToLocals[variable].Dispose();
                context.VariablesToLocals.Remove(variable);
                context.Variables.Pop();
            }
            resultType = node.Type;
        }

        private static bool BuildMethodCall(MethodCallExpression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, bool extend, out Type resultType)
        {
            var result = false;
            GrobIL il = context.Il;
            var method = node.Method;
            Expression obj;
            IEnumerable<Expression> arguments;
            if(!method.IsStatic)
            {
                obj = node.Object;
                arguments = node.Arguments;
            }
            else if(method.GetCustomAttributes(typeof(ExtensionAttribute), false).Any())
            {
                obj = node.Arguments[0];
                arguments = node.Arguments.Skip(1);
            }
            else
            {
                obj = null;
                arguments = node.Arguments;
            }
            Type type = obj == null ? null : obj.Type;
            if(obj != null)
            {
                Type actualType;
                result |= Build(obj, context, returnDefaultValueLabel, true, extend, out actualType); // stack: [obj]
                if(actualType.IsValueType)
                {
                    using(var temp = context.DeclareLocal(actualType))
                    {
                        il.Stloc(temp);
                        il.Ldloca(temp);
                    }
                }
                if(context.Options.HasFlag(CompilerOptions.CheckNullReferences))
                    result |= EmitNullChecking(type, context, returnDefaultValueLabel);
            }
            BuildArguments(context, arguments.ToArray());
            il.Call(method, type);
            resultType = node.Type;
            return result;
        }

        private static bool BuildArrayIndex(BinaryExpression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType)
        {
            var result = false;
            GrobIL il = context.Il;
            result |= Build(node.Left, context, returnDefaultValueLabel, true, extend); // stack: [array]
            if(context.Options.HasFlag(CompilerOptions.CheckNullReferences))
            {
                result = true;
                il.Dup(); // stack: [array, array]
                il.Brfalse(returnDefaultValueLabel); // if(array == null) goto returnDefaultValue; stack: [array]
            }
            GrobIL.Label indexIsNullLabel = context.CanReturn ? il.DefineLabel("indexIsNull") : null;
            bool labelUsed = Build(node.Right, context, indexIsNullLabel, false, false); // stack: [array, index]
            if(labelUsed && context.CanReturn)
            {
                var indexIsNotNullLabel = il.DefineLabel("indexIsNotNull");
                il.Br(indexIsNotNullLabel);
                il.MarkLabel(indexIsNullLabel);
                il.Pop();
                il.Ldc_I4(0);
                il.MarkLabel(indexIsNotNullLabel);
            }
            if(context.Options.HasFlag(CompilerOptions.CheckArrayIndexes))
            {
                result = true;
                using(var arrayIndex = context.DeclareLocal(typeof(int)))
                {
                    il.Stloc(arrayIndex); // arrayIndex = index; stack: [array]
                    il.Dup(); // stack: [array, array]
                    il.Ldlen(); // stack: [array, array.Length]
                    il.Ldloc(arrayIndex); // stack: [array, array.Length, arrayIndex]
                    il.Ble(typeof(int), returnDefaultValueLabel); // if(array.Length <= arrayIndex) goto returnDefaultValue; stack: [array]
                    il.Ldloc(arrayIndex); // stack: [array, arrayIndex]
                    il.Ldc_I4(0); // stack: [array, arrayIndex, 0]
                    il.Blt(typeof(int), returnDefaultValueLabel); // if(arrayIndex < 0) goto returnDefaultValue; stack: [array]
                    il.Ldloc(arrayIndex); // stack: [array, arrayIndex]
                }
            }
            if(returnByRef && node.Type.IsValueType)
            {
                il.Ldelema(node.Type);
                resultType = node.Type.MakeByRefType();
            }
            else
            {
                il.Ldelem(node.Type); // stack: [array[arrayIndex]]
                resultType = node.Type;
            }
            return result;
        }

        private static void BuildConstant(ConstantExpression node, EmittingContext context, out Type resultType)
        {
            if(node.Value == null)
                EmitLoadDefaultValue(context, node.Type);
            else
            {
                switch(Type.GetTypeCode(node.Type))
                {
                case TypeCode.Boolean:
                    context.Il.Ldc_I4(((bool)node.Value) ? 1 : 0);
                    break;
                case TypeCode.Byte:
                    context.Il.Ldc_I4((byte)node.Value);
                    break;
                case TypeCode.Char:
                    context.Il.Ldc_I4((char)node.Value);
                    break;
                case TypeCode.Int16:
                    context.Il.Ldc_I4((short)node.Value);
                    break;
                case TypeCode.Int32:
                    context.Il.Ldc_I4((int)node.Value);
                    break;
                case TypeCode.SByte:
                    context.Il.Ldc_I4((sbyte)node.Value);
                    break;
                case TypeCode.UInt16:
                    context.Il.Ldc_I4((ushort)node.Value);
                    break;
                case TypeCode.UInt32:
                    unchecked
                    {
                        context.Il.Ldc_I4((int)(uint)node.Value);
                    }
                    break;
                case TypeCode.Int64:
                    context.Il.Ldc_I8((long)node.Value);
                    break;
                case TypeCode.UInt64:
                    unchecked
                    {
                        context.Il.Ldc_I8((long)(ulong)node.Value);
                    }
                    break;
                case TypeCode.Single:
                    context.Il.Ldc_R4((float)node.Value);
                    break;
                case TypeCode.Double:
                    context.Il.Ldc_R8((double)node.Value);
                    break;
                case TypeCode.String:
                    context.Il.Ldstr((string)node.Value);
                    break;
                default:
                    throw new NotSupportedException("Constant of type '" + node.Type + "' is not supported");
                }
            }
            resultType = node.Type;
        }

        private static bool BuildConvert(UnaryExpression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, bool extend, out Type resultType)
        {
            var result = Build(node.Operand, context, returnDefaultValueLabel, extend, false, out resultType); // stack: [obj]
            if(resultType != node.Type && !(context.Options.HasFlag(CompilerOptions.UseTernaryLogic) && resultType == typeof(bool?) && node.Type == typeof(bool)))
            {
                if(node.Method != null)
                    context.Il.Call(node.Method);
                else
                    EmitConvert(context, node.Operand.Type, node.Type); // stack: [(type)obj]
                resultType = node.Type;
            }
            return result;
        }

        private static void BuildParameter(ParameterExpression node, EmittingContext context, bool returnByRef, out Type resultType)
        {
            int index = Array.IndexOf(context.Parameters, node);
            if(index >= 0)
            {
                if(returnByRef && node.Type.IsValueType)
                {
                    context.Il.Ldarga(index); // stack: [&parameter]
                    resultType = node.Type.MakeByRefType();
                }
                else
                {
                    context.Il.Ldarg(index); // stack: [parameter]
                    resultType = node.Type;
                }
                return;
            }
            EmittingContext.LocalHolder variable;
            if(context.VariablesToLocals.TryGetValue(node, out variable))
            {
                if(returnByRef && node.Type.IsValueType)
                {
                    context.Il.Ldloca(variable); // stack: [&variable]
                    resultType = node.Type.MakeByRefType();
                }
                else
                {
                    context.Il.Ldloc(variable); // stack: [variable]
                    resultType = node.Type;
                }
            }
            else
                throw new InvalidOperationException("Unknown parameter " + node);
        }

        private static bool BuildMemberAccess(MemberExpression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType)
        {
            bool result = false;
            Type type = node.Expression == null ? null : node.Expression.Type;
            GrobIL il = context.Il;
            if(node.Expression == null)
            {
                if(node.Member is FieldInfo)
                    il.Ldnull();
            }
            else
            {
                result |= Build(node.Expression, context, returnDefaultValueLabel, true, extend, out type); // stack: [obj]
                if(type.IsValueType)
                {
                    using(var temp = context.DeclareLocal(type))
                    {
                        il.Stloc(temp);
                        il.Ldloca(temp);
                    }
                }
                if(node.Expression != context.ClosureParameter && context.Options.HasFlag(CompilerOptions.CheckNullReferences))
                    result |= EmitNullChecking(node.Expression.Type, context, returnDefaultValueLabel);
            }
            extend &= CanAssign(node.Member);
            Type memberType = GetMemberType(node.Member);
            ConstructorInfo constructor = memberType.GetConstructor(Type.EmptyTypes);
            extend &= memberType.IsClass && constructor != null;
            if(!extend)
                EmitMemberAccess(type, node.Member, context, returnByRef, out resultType); // stack: [obj.member]
            else
            {
                if(node.Expression == null)
                {
                    EmitMemberAccess(type, node.Member, context, returnByRef, out resultType); // stack: [obj.member]
                    var memberIsNotNullLabel = il.DefineLabel("memberIsNotNull");
                    il.Dup();
                    il.Brtrue(memberIsNotNullLabel);
                    il.Pop();
                    if(node.Member is FieldInfo)
                        il.Ldnull();
                    il.Newobj(constructor);
                    using(var newobj = context.DeclareLocal(memberType))
                    {
                        il.Stloc(newobj);
                        il.Ldloc(newobj);
                        EmitMemberAssign(type, node.Member, il);
                        il.Ldloc(newobj);
                    }
                    il.MarkLabel(memberIsNotNullLabel);
                }
                else
                {
                    using(var temp = context.DeclareLocal(type))
                    {
                        il.Stloc(temp);
                        il.Ldloc(temp);
                        EmitMemberAccess(type, node.Member, context, returnByRef, out resultType); // stack: [obj.member]
                        var memberIsNotNullLabel = il.DefineLabel("memberIsNotNull");
                        il.Dup();
                        il.Brtrue(memberIsNotNullLabel);
                        il.Pop();
                        il.Ldloc(temp);
                        il.Newobj(constructor);
                        using(var newobj = context.DeclareLocal(memberType))
                        {
                            il.Stloc(newobj);
                            il.Ldloc(newobj);
                            EmitMemberAssign(type, node.Member, il);
                            il.Ldloc(newobj);
                        }
                        il.MarkLabel(memberIsNotNullLabel);
                    }
                }
            }
            return result;
        }

        private static Type GetMemberType(MemberInfo member)
        {
            switch(member.MemberType)
            {
            case MemberTypes.Field:
                return ((FieldInfo)member).FieldType;
            case MemberTypes.Property:
                return ((PropertyInfo)member).PropertyType;
            default:
                throw new NotSupportedException("Member " + member + " is not supported");
            }
        }

        private static bool CanAssign(MemberInfo member)
        {
            return member.MemberType == MemberTypes.Field || (member.MemberType == MemberTypes.Property && ((PropertyInfo)member).CanWrite);
        }

        private static void ConvertFromNullableBoolToBool(EmittingContext context)
        {
            var il = context.Il;
            using(var temp = context.DeclareLocal(typeof(bool?)))
            {
                il.Stloc(temp);
                il.Ldloca(temp);
                il.Ldfld(nullableBoolValueField);
            }
        }

        private static void BuildArgument(EmittingContext context, Expression argument, bool convertToBool, out Type argumentType)
        {
            var il = context.Il;
            var argumentIsNullLabel = context.CanReturn ? il.DefineLabel("argumentIsNull") : null;
            bool labelUsed = Build(argument, context, argumentIsNullLabel, false, false, out argumentType);
            if(convertToBool && argument.Type == typeof(bool) && argumentType == typeof(bool?))
            {
                ConvertFromNullableBoolToBool(context);
                argumentType = typeof(bool);
            }
            if(labelUsed)
                EmitReturnDefaultValue(context, argument.Type, argumentIsNullLabel, il.DefineLabel("argumentIsNotNull"));
        }

        private static void BuildArguments(EmittingContext context, params Expression[] arguments)
        {
            foreach(var argument in arguments)
            {
                Type argumentType;
                BuildArgument(context, argument, true, out argumentType);
            }
        }

        private static void EmitReturnDefaultValue(EmittingContext context, Type type, GrobIL.Label valueIsNullLabel, GrobIL.Label valueIsNotNullLabel)
        {
            var il = context.Il;
            il.Br(valueIsNotNullLabel);
            il.MarkLabel(valueIsNullLabel);
            il.Pop();
            EmitLoadDefaultValue(context, type);
            il.MarkLabel(valueIsNotNullLabel);
        }

        private static void EmitLoadDefaultValue(EmittingContext context, Type type)
        {
            var il = context.Il;
            if(!type.IsValueType)
                il.Ldnull();
            else
            {
                using(var temp = context.DeclareLocal(type))
                {
                    il.Ldloca(temp);
                    il.Initobj(type);
                    il.Ldloc(temp);
                }
            }
        }

        private static void EmitMemberAccess(Type type, MemberInfo member, EmittingContext context, bool returnByRef, out Type memberType)
        {
            switch(member.MemberType)
            {
            case MemberTypes.Property:
                var property = (PropertyInfo)member;
                var getter = property.GetGetMethod();
                if(getter == null)
                    throw new MissingMemberException(member.DeclaringType.Name, member.Name + "_get");
                context.Il.Call(getter, type);
                Type propertyType = property.PropertyType;
                if(returnByRef && propertyType.IsValueType)
                {
                    using(var temp = context.DeclareLocal(propertyType))
                    {
                        context.Il.Stloc(temp);
                        context.Il.Ldloca(temp);
                        memberType = propertyType.MakeByRefType();
                    }
                }
                else
                    memberType = propertyType;
                break;
            case MemberTypes.Field:
                var field = (FieldInfo)member;
                if(returnByRef && field.FieldType.IsValueType)
                {
                    context.Il.Ldflda(field);
                    memberType = field.FieldType.MakeByRefType();
                }
                else
                {
                    context.Il.Ldfld(field);
                    memberType = field.FieldType;
                }
                break;
            default:
                throw new InvalidOperationException(); // todo exception
            }
        }

        private static void EmitMemberAssign(Type type, MemberInfo member, GrobIL il)
        {
            switch(member.MemberType)
            {
            case MemberTypes.Property:
                var setter = ((PropertyInfo)member).GetSetMethod();
                if(setter == null)
                    throw new MissingMemberException(member.DeclaringType.Name, member.Name + "_set");
                il.Call(setter, type);
                break;
            case MemberTypes.Field:
                il.Stfld((FieldInfo)member);
                break;
            }
        }

        private static void EmitConvert(EmittingContext context, Type from, Type to)
        {
            if(from == to) return;
            var il = context.Il;
            if(!from.IsValueType)
            {
                if(!to.IsValueType)
                    il.Castclass(to);
                else
                {
                    if(from != typeof(object))
                        throw new InvalidCastException("Cannot cast an object of type '" + from + "' to type '" + to + "'");
                    il.Unbox_Any(to);
                }
            }
            else
            {
                if(!to.IsValueType)
                {
                    if(to != typeof(object))
                        throw new InvalidCastException("Cannot cast an object of type '" + from + "' to type '" + to + "'");
                    il.Box(from);
                }
                else
                {
                    if(IsNullable(to))
                    {
                        var toArgument = to.GetGenericArguments()[0];
                        if(IsNullable(from))
                        {
                            var fromArgument = from.GetGenericArguments()[0];
                            using(var temp = context.DeclareLocal(from))
                            {
                                il.Stloc(temp);
                                il.Ldloca(temp);
                                FieldInfo hasValueField = from.GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance);
                                il.Ldfld(hasValueField);
                                var valueIsNullLabel = il.DefineLabel("valueIsNull");
                                il.Brfalse(valueIsNullLabel);
                                il.Ldloca(temp);
                                FieldInfo valueField = from.GetField("value", BindingFlags.NonPublic | BindingFlags.Instance);
                                il.Ldfld(valueField);
                                if(toArgument != fromArgument)
                                    EmitConvert(context, fromArgument, toArgument);
                                il.Newobj(to.GetConstructor(new[] {toArgument}));
                                var doneLabel = il.DefineLabel("done");
                                il.Br(doneLabel);
                                il.MarkLabel(valueIsNullLabel);
                                EmitLoadDefaultValue(context, to);
                                il.MarkLabel(doneLabel);
                            }
                        }
                        else
                        {
                            if(toArgument != from)
                                EmitConvert(context, from, toArgument);
                            il.Newobj(to.GetConstructor(new[] {toArgument}));
                        }
                    }
                    else if(to.IsEnum)
                        EmitConvert(context, from, typeof(int));
                    else if(from.IsEnum)
                        EmitConvert(context, typeof(int), to);
                    else
                        throw new NotSupportedException("Cast from type '" + from + "' to type '" + to + "' is not supported");
                }
            }
        }

        // ReSharper disable RedundantExplicitNullableCreation
        private static readonly ConstructorInfo nullableBoolConstructor = ((NewExpression)((Expression<Func<bool, bool?>>)(b => new bool?(b))).Body).Constructor;
        // ReSharper restore RedundantExplicitNullableCreation
        private static readonly FieldInfo nullableBoolValueField = typeof(bool?).GetField("value", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo nullableBoolHasValueField = typeof(bool?).GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
        private static readonly ModuleBuilder module = assembly.DefineDynamicModule(Guid.NewGuid().ToString());

        private enum ResultType
        {
            Value,
            ByRef,
            ValueTypeByRef,
        }
    }
}