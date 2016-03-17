using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit.Utils;

namespace GrobExp.Compiler
{
    public static class ExpressionHashCalculator
    {
        public static int CalcHashCode(Expression node, bool strictly)
        {
            var context = new Context
                {
                    strictly = strictly,
                    hard = false,
                    p = 1084996987, //for sake of primality
                    parameters = new Dictionary<Type, Dictionary<ParameterExpression, int>>(),
                    labels = new Dictionary<LabelTarget, int>(),
                };
            CalcHashCode(node, context);
            return context.hashCode;
        }

        public static long CalcStrongHashCode(Expression node)
        {
            var context = new Context
                {
                    strictly = false,
                    hard = true,
                    p = 1084996987, //for sake of primality
                    parameters = new Dictionary<Type, Dictionary<ParameterExpression, int>>(),
                    labels = new Dictionary<LabelTarget, int>(),
                };
            CalcHashCode(node, context);
            return context.longHashCode;
        }

        private static void CalcHashCode(IEnumerable<Expression> list, Context context)
        {
            foreach(var exp in list)
                CalcHashCode(exp, context);
        }

        private static void CalcHashCode(Expression node, Context context)
        {
            if(node == null)
            {
                context.Add(0);
                return;
            }
            CalcHashCode(node.NodeType, context);
            CalcHashCode(node.Type, context);
            switch(node.NodeType)
            {
            case ExpressionType.Add:
            case ExpressionType.AddAssign:
            case ExpressionType.AddAssignChecked:
            case ExpressionType.AddChecked:
            case ExpressionType.And:
            case ExpressionType.AndAlso:
            case ExpressionType.AndAssign:
            case ExpressionType.ArrayIndex:
            case ExpressionType.Assign:
            case ExpressionType.Coalesce:
            case ExpressionType.Divide:
            case ExpressionType.DivideAssign:
            case ExpressionType.Equal:
            case ExpressionType.ExclusiveOr:
            case ExpressionType.ExclusiveOrAssign:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LeftShift:
            case ExpressionType.LeftShiftAssign:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.Modulo:
            case ExpressionType.ModuloAssign:
            case ExpressionType.Multiply:
            case ExpressionType.MultiplyAssign:
            case ExpressionType.MultiplyAssignChecked:
            case ExpressionType.MultiplyChecked:
            case ExpressionType.NotEqual:
            case ExpressionType.Or:
            case ExpressionType.OrAssign:
            case ExpressionType.OrElse:
            case ExpressionType.Power:
            case ExpressionType.PowerAssign:
            case ExpressionType.RightShift:
            case ExpressionType.RightShiftAssign:
            case ExpressionType.Subtract:
            case ExpressionType.SubtractAssign:
            case ExpressionType.SubtractAssignChecked:
            case ExpressionType.SubtractChecked:
                CalcHashCodeBinary((BinaryExpression)node, context);
                break;
            case ExpressionType.ArrayLength:
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
            case ExpressionType.Decrement:
            case ExpressionType.Increment:
            case ExpressionType.IsFalse:
            case ExpressionType.IsTrue:
            case ExpressionType.Negate:
            case ExpressionType.NegateChecked:
            case ExpressionType.Not:
            case ExpressionType.OnesComplement:
            case ExpressionType.PostDecrementAssign:
            case ExpressionType.PostIncrementAssign:
            case ExpressionType.PreDecrementAssign:
            case ExpressionType.PreIncrementAssign:
            case ExpressionType.TypeAs:
            case ExpressionType.UnaryPlus:
            case ExpressionType.Unbox:
            case ExpressionType.Quote:
            case ExpressionType.Throw:
                CalcHashCodeUnary((UnaryExpression)node, context);
                break;
            case ExpressionType.Parameter:
                CalcHashCodeParameter((ParameterExpression)node, context);
                break;
            case ExpressionType.Block:
                CalcHashCodeBlock((BlockExpression)node, context);
                break;
            case ExpressionType.Call:
                CalcHashCodeCall((MethodCallExpression)node, context);
                break;
            case ExpressionType.Conditional:
                CalcHashCodeConditional((ConditionalExpression)node, context);
                break;
            case ExpressionType.Constant:
                CalcHashCodeConstant((ConstantExpression)node, context);
                break;
            case ExpressionType.DebugInfo:
                CalcHashCodeDebugInfo((DebugInfoExpression)node, context);
                break;
            case ExpressionType.Default:
                CalcHashCodeDefault((DefaultExpression)node, context);
                break;
            case ExpressionType.Dynamic:
                CalcHashCodeDynamic((DynamicExpression)node, context);
                break;
            case ExpressionType.Extension:
                CalcHashCodeExtension(node, context);
                break;
            case ExpressionType.Goto:
                CalcHashCodeGoto((GotoExpression)node, context);
                break;
            case ExpressionType.Index:
                CalcHashCodeIndex((IndexExpression)node, context);
                break;
            case ExpressionType.Invoke:
                CalcHashCodeInvoke((InvocationExpression)node, context);
                break;
            case ExpressionType.Label:
                CalcHashCodeLabel((LabelExpression)node, context);
                break;
            case ExpressionType.Lambda:
                CalcHashCodeLambda((LambdaExpression)node, context);
                break;
            case ExpressionType.ListInit:
                CalcHashCodeListInit((ListInitExpression)node, context);
                break;
            case ExpressionType.Loop:
                CalcHashCodeLoop((LoopExpression)node, context);
                break;
            case ExpressionType.MemberAccess:
                CalcHashCodeMemberAccess((MemberExpression)node, context);
                break;
            case ExpressionType.MemberInit:
                CalcHashCodeMemberInit((MemberInitExpression)node, context);
                break;
            case ExpressionType.New:
                CalcHashCodeNew((NewExpression)node, context);
                break;
            case ExpressionType.NewArrayBounds:
            case ExpressionType.NewArrayInit:
                CalcHashCodeNewArray((NewArrayExpression)node, context);
                break;
            case ExpressionType.RuntimeVariables:
                CalcHashCodeRuntimeVariables((RuntimeVariablesExpression)node, context);
                break;
            case ExpressionType.Switch:
                CalcHashCodeSwitch((SwitchExpression)node, context);
                break;
            case ExpressionType.Try:
                CalcHashCodeTry((TryExpression)node, context);
                break;
            case ExpressionType.TypeEqual:
            case ExpressionType.TypeIs:
                CalcHashCodeTypeBinary((TypeBinaryExpression)node, context);
                break;
            default:
                throw new NotSupportedException("Node type '" + node.NodeType + "' is not supported");
            }
        }

        private static void CalcHashCode(ExpressionType expressionType, Context context)
        {
            context.Add((int)expressionType);
        }

        private static void CalcHashCode(MemberBindingType bindingType, Context context)
        {
            context.Add((int)bindingType);
        }

        private static void CalcHashCode(MemberInfo member, Context context)
        {
            if(member == null)
            {
                context.Add(0);
                return;
            }
            CalcHashCode(member.Module.Assembly.FullName, context);
            context.Add(member.Module.MetadataToken);
            context.Add(member.MetadataToken);
        }

        private static unsafe void CalcHashCode(string str, Context context)
        {
            if(str == null)
            {
                context.Add(0);
                return;
            }
            if(!context.hard)
                context.Add(str.GetHashCode());
            else
            {
                var length = str.Length;
                context.Add(length);
                fixed(char* c = str)
                {
                    var p = (int*)c;
                    for(int i = 0; i < length >> 1; ++i)
                        context.Add(*p++);
                    if((length & 1) == 1)
                        context.Add(*(short*)p);
                }
            }
        }

        private static void CalcHashCode(int x, Context context)
        {
            context.Add(x);
        }

        private static void CalcHashCode(Type type, Context context)
        {
            CalcHashCode(type.Module.Assembly.FullName, context);
            context.Add(type.Module.MetadataToken);
            context.Add(type.MetadataToken);
            // todo calc hash code of type
            CalcHashCode(Formatter.Format(type), context);
        }

        private static void CalcHashCode(GotoExpressionKind kind, Context context)
        {
            if(context.hard)
                CalcHashCode((int)kind, context);
            else
                context.Add((int)kind);
        }

        private static void CalcHashCodeObject(object obj, Context context)
        {
            if(obj == null)
            {
                context.Add(0);
                return;
            }
            if(!context.hard)
                context.Add(obj.GetHashCode());
            else
            {
                var typecode = Type.GetTypeCode(obj.GetType());
                switch(typecode)
                {
                case TypeCode.Boolean:
                    CalcHashCode((bool)obj ? 1 : 0, context);
                    break;
                case TypeCode.Char:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                    CalcHashCode((int)obj, context);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    CalcHashCode(unchecked((int)((ulong)obj >> 32)), context);
                    CalcHashCode(unchecked((int)((ulong)obj & ((1L << 32) - 1))), context);
                    break;
                case TypeCode.String:
                    CalcHashCode((string)obj, context);
                    break;
                default:
                    throw new NotSupportedException("Type is not supported by hard hashing");
                }
            }
        }

        private static void CalcHashCodeParameter(ParameterExpression node, Context context)
        {
            if(node == null)
            {
                context.Add(0);
                return;
            }

            var parameterType = node.IsByRef ? node.Type.MakeByRefType() : node.Type;
            CalcHashCode(parameterType, context);
            if(context.strictly)
                CalcHashCode(node.Name, context);
            else
            {
                Dictionary<ParameterExpression, int> parameters;
                if(!context.parameters.TryGetValue(parameterType, out parameters))
                    context.parameters.Add(parameterType, parameters = new Dictionary<ParameterExpression, int>());
                int index;
                if(!parameters.TryGetValue(node, out index))
                    parameters.Add(node, index = parameters.Count);
                CalcHashCode(index, context);
            }
        }

        private static void CalcHashCodeUnary(UnaryExpression node, Context context)
        {
            CalcHashCode(node.Method, context);
            CalcHashCode(node.Operand, context);
        }

        private static void CalcHashCodeBinary(BinaryExpression node, Context context)
        {
            CalcHashCode(node.Method, context);
            CalcHashCode(node.Left, context);
            CalcHashCode(node.Right, context);
        }

        private static void CalcHashCodeBlock(BlockExpression node, Context context)
        {
            if(context.strictly)
            {
                foreach(var variable in node.Variables)
                    CalcHashCodeParameter(variable, context);
            }

            if(!context.strictly)
            {
                foreach(var variable in node.Variables)
                {
                    Dictionary<ParameterExpression, int> parameters;
                    if(!context.parameters.TryGetValue(variable.Type, out parameters))
                        context.parameters.Add(variable.Type, parameters = new Dictionary<ParameterExpression, int>());
                    parameters.Add(variable, parameters.Count);
                }
            }
            CalcHashCode(node.Expressions, context);
            if(!context.strictly)
            {
                foreach(var variable in node.Variables)
                    context.parameters[variable.Type].Remove(variable);
            }
        }

        private static void CalcHashCodeCall(MethodCallExpression node, Context context)
        {
            CalcHashCode(node.Method, context);
            CalcHashCode(node.Object, context);
            CalcHashCode(node.Arguments, context);
        }

        private static void CalcHashCodeConditional(ConditionalExpression node, Context context)
        {
            CalcHashCode(node.Test, context);
            CalcHashCode(node.IfTrue, context);
            CalcHashCode(node.IfFalse, context);
        }

        private static void CalcHashCodeConstant(ConstantExpression node, Context context)
        {
            CalcHashCodeObject(node.Value, context);
        }

        private static void CalcHashCodeDebugInfo(DebugInfoExpression node, Context context)
        {
            throw new NotSupportedException();
        }

        private static void CalcHashCodeDefault(DefaultExpression node, Context context)
        {
            //empty body
        }

        private static void CalcHashCodeDynamic(DynamicExpression node, Context context)
        {
            throw new NotSupportedException();
        }

        private static void CalcHashCodeExtension(Expression node, Context context)
        {
            throw new NotSupportedException();
        }

        private static void CalcHashCodeLabel(LabelTarget target, Context context)
        {
            if(target == null)
            {
                context.Add(0);
                return;
            }

            int labelId;
            if (!context.labels.TryGetValue(target, out labelId))
                context.labels.Add(target, labelId = context.labels.Count);
            CalcHashCode(labelId, context);
        }

        private static void CalcHashCodeGoto(GotoExpression node, Context context)
        {
            CalcHashCode(node.Kind, context);
            CalcHashCodeLabel(node.Target, context);
            CalcHashCode(node.Value, context);
        }

        private static void CalcHashCodeIndex(IndexExpression node, Context context)
        {
            CalcHashCode(node.Object, context);
            CalcHashCode(node.Indexer, context);
            CalcHashCode(node.Arguments, context);
        }

        private static void CalcHashCodeInvoke(InvocationExpression node, Context context)
        {
            CalcHashCode(new[] {node.Expression}.Concat(node.Arguments), context);
        }

        private static void CalcHashCodeLabel(LabelExpression node, Context context)
        {
            int labelId;
            if (!context.labels.TryGetValue(node.Target, out labelId))
            {
                labelId = context.labels.Count;
                context.Add(labelId);
            }
            context.Add(labelId);
            CalcHashCode(node.DefaultValue, context);
        }

        private static void CalcHashCodeLambda(LambdaExpression node, Context context)
        {
            if(!context.strictly)
            {
                foreach(var parameter in node.Parameters)
                {
                    Dictionary<ParameterExpression, int> parameters;
                    if(!context.parameters.TryGetValue(parameter.Type, out parameters))
                        context.parameters.Add(parameter.Type, parameters = new Dictionary<ParameterExpression, int>());
                    parameters.Add(parameter, parameters.Count);
                }
            }
            CalcHashCode(node.Body, context);
            if(!context.strictly)
            {
                foreach(var parameter in node.Parameters)
                    context.parameters[parameter.Type].Remove(parameter);
            }
        }

        private static void CalcHashCodeElemInits(IEnumerable<ElementInit> inits, Context context)
        {
            foreach(var init in inits)
            {
                CalcHashCode(init.AddMethod, context);
                CalcHashCode(init.Arguments, context);
            }
        }

        private static void CalcHashCodeListInit(ListInitExpression node, Context context)
        {
            CalcHashCode(node.NewExpression, context);
            CalcHashCodeElemInits(node.Initializers, context);
        }

        private static void CalcHashCodeLoop(LoopExpression node, Context context)
        {
            CalcHashCode(node.Body, context);
            CalcHashCodeLabel(node.ContinueLabel, context);
            CalcHashCodeLabel(node.BreakLabel, context);
        }

        private static void CalcHashCodeMemberAccess(MemberExpression node, Context context)
        {
            CalcHashCode(node.Member, context);
            CalcHashCode(node.Expression, context);
        }

        private static void CalcHashCodeMemberInit(MemberInitExpression node, Context context)
        {
            CalcHashCode(node.NewExpression, context);
            foreach(var memberBinding in node.Bindings)
            {
                var binding = (MemberAssignment)memberBinding;
                CalcHashCode(binding.BindingType, context);
                CalcHashCode(binding.Member, context);
                CalcHashCode(binding.Expression, context);
            }
        }

        private static void CalcHashCodeNew(NewExpression node, Context context)
        {
            CalcHashCode(node.Constructor, context);
            CalcHashCode(node.Arguments, context);

            if(node.Members != null)
            {
                var normalizedMembers = node.Members.ToList();
                if(!context.strictly)
                {
                    normalizedMembers.Sort((first, second) =>
                    {
                        if (first.Module.Assembly != second.Module.Assembly)
                            return string.Compare(first.Module.Assembly.FullName, second.Module.Assembly.FullName, StringComparison.InvariantCulture);
                        if (first.Module.MetadataToken != second.Module.MetadataToken)
                            return first.Module.MetadataToken - second.Module.MetadataToken;
                        return first.MetadataToken - second.MetadataToken;
                    });
                }
                foreach (var member in node.Members)
                    CalcHashCode(member, context);
            }
        }

        private static void CalcHashCodeNewArray(NewArrayExpression node, Context context)
        {
            CalcHashCode(node.Expressions, context);
        }

        private static void CalcHashCodeRuntimeVariables(RuntimeVariablesExpression node, Context context)
        {
            throw new NotSupportedException();
        }

        private static void CalcHashCodeCases(IEnumerable<SwitchCase> cases, Context context)
        {
            foreach(var oneCase in cases)
            {
                CalcHashCode(oneCase.Body, context);
                CalcHashCode(oneCase.TestValues, context);
            }
        }

        private static void CalcHashCodeSwitch(SwitchExpression node, Context context)
        {
            CalcHashCodeCases(node.Cases, context);
            CalcHashCode(node.Comparison, context);
            CalcHashCode(node.DefaultBody, context);
            CalcHashCode(node.SwitchValue, context);
        }

        private static void CalcHashCodeCatchBlocks(IEnumerable<CatchBlock> handlers, Context context)
        {
            foreach(var handler in handlers)
            {
                CalcHashCode(handler.Body, context);
                CalcHashCode(handler.Filter, context);
                CalcHashCode(handler.Test, context);
                CalcHashCodeParameter(handler.Variable, context);
            }
        }

        private static void CalcHashCodeTry(TryExpression node, Context context)
        {
            CalcHashCode(node.Body, context);
            CalcHashCode(node.Fault, context);
            CalcHashCode(node.Finally, context);
            CalcHashCodeCatchBlocks(node.Handlers, context);
        }

        private static void CalcHashCodeTypeBinary(TypeBinaryExpression node, Context context)
        {
            CalcHashCode(node.Expression, context);
            CalcHashCode(node.TypeOperand, context);
        }

        private class Context
        {
            public bool strictly;
            public bool hard;
            public Dictionary<Type, Dictionary<ParameterExpression, int>> parameters;
            public Dictionary<LabelTarget, int> labels;
            public int p;
            public int hashCode;
            public long longHashCode;

            public void Add(int x)
            {
                if(!hard)
                {
                    hashCode = unchecked(hashCode * p + x);
                }
                else
                {
                    longHashCode = unchecked(longHashCode * p + x);
//                    var zuid = Mul(new Zuid { guid = longHashCode }, unchecked((uint)p));
//                    longHashCode = ExpressionHashCalculator.Add(zuid, ToZuid(x)).guid;
                }
            }
        }
    }
}