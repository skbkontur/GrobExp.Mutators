using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators.Visitors
{
    public static class ExpressionEquivalenceChecker
    {
        public static bool Equivalent(Expression first, Expression second, bool strictly, bool distinguishEachAndCurrent)
        {
            return Equivalent(first, second, new Context
                {
                    Strictly = strictly,
                    DistinguishEachAndCurrent = distinguishEachAndCurrent,
                    FirstParameters = new Dictionary<ParameterExpression, ParameterExpression>(),
                    SecondParameters = new Dictionary<ParameterExpression, ParameterExpression>(),
                    FirstLabels = new Dictionary<LabelTarget, LabelTarget>(),
                    SecondLabels = new Dictionary<LabelTarget, LabelTarget>()
                });
        }

        private static bool Equivalent(IList<Expression> firstList, IList<Expression> secondList, Context context)
        {
            if(firstList.Count != secondList.Count)
                return false;
            for(int i = 0; i < firstList.Count; i++)
            {
                if(!Equivalent(firstList[i], secondList[i], context))
                    return false;
            }
            return true;
        }

        private static bool Equivalent(Expression first, Expression second, Context context)
        {
            if(first == null || second == null)
                return first == null && second == null;
            if(first.NodeType != second.NodeType || first.Type != second.Type)
                return false;
            switch(first.NodeType)
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
                return EquivalentBinary((BinaryExpression)first, (BinaryExpression)second, context);
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
                return EquivalentUnary((UnaryExpression)first, (UnaryExpression)second, context);
            case ExpressionType.Parameter:
                return EquivalentParameter((ParameterExpression)first, (ParameterExpression)second, context);
            case ExpressionType.Block:
                return EquivalentBlock((BlockExpression)first, (BlockExpression)second, context);
            case ExpressionType.Call:
                return EquivalentCall((MethodCallExpression)first, (MethodCallExpression)second, context);
            case ExpressionType.Conditional:
                return EquivalentConditional((ConditionalExpression)first, (ConditionalExpression)second, context);
            case ExpressionType.Constant:
                return EquivalentConstant((ConstantExpression)first, (ConstantExpression)second, context);
            case ExpressionType.DebugInfo:
                return EquivalentDebugInfo((DebugInfoExpression)first, (DebugInfoExpression)second, context);
            case ExpressionType.Default:
                return EquivalentDefault((DefaultExpression)first, (DefaultExpression)second, context);
            case ExpressionType.Dynamic:
                return EquivalentDynamic((DynamicExpression)first, (DynamicExpression)second, context);
            case ExpressionType.Extension:
                return EquivalentExtension(first, second, context);
            case ExpressionType.Goto:
                return EquivalentGoto((GotoExpression)first, (GotoExpression)second, context);
            case ExpressionType.Index:
                return EquivalentIndex((IndexExpression)first, (IndexExpression)second, context);
            case ExpressionType.Invoke:
                return EquivalentInvoke((InvocationExpression)first, (InvocationExpression)second, context);
            case ExpressionType.Label:
                return EquivalentLabel((LabelExpression)first, (LabelExpression)second, context);
            case ExpressionType.Lambda:
                return EquivalentLambda((LambdaExpression)first, (LambdaExpression)second, context);
            case ExpressionType.ListInit:
                return EquivalentListInit((ListInitExpression)first, (ListInitExpression)second, context);
            case ExpressionType.Loop:
                return EquivalentLoop((LoopExpression)first, (LoopExpression)second, context);
            case ExpressionType.MemberAccess:
                return EquivalentMemberAccess((MemberExpression)first, (MemberExpression)second, context);
            case ExpressionType.MemberInit:
                return EquivalentMemberInit((MemberInitExpression)first, (MemberInitExpression)second, context);
            case ExpressionType.New:
                return EquivalentNew((NewExpression)first, (NewExpression)second, context);
            case ExpressionType.NewArrayBounds:
            case ExpressionType.NewArrayInit:
                return EquivalentNewArray((NewArrayExpression)first, (NewArrayExpression)second, context);
            case ExpressionType.RuntimeVariables:
                return EquivalentRuntimeVariables((RuntimeVariablesExpression)first, (RuntimeVariablesExpression)second, context);
            case ExpressionType.Switch:
                return EquivalentSwitch((SwitchExpression)first, (SwitchExpression)second, context);
            case ExpressionType.Try:
                return EquivalentTry((TryExpression)first, (TryExpression)second, context);
            case ExpressionType.TypeEqual:
            case ExpressionType.TypeIs:
                return EquivalentTypeBinary((TypeBinaryExpression)first, (TypeBinaryExpression)second, context);
            default:
                throw new NotSupportedException("Node type '" + first.NodeType + "' is not supported");
            }
        }

        private static bool EquivalentParameter(ParameterExpression first, ParameterExpression second, Context context)
        {
            if(context.Strictly)
            {
                if(first == second)
                    return true;
                ParameterExpression firstMapsTo;
                ParameterExpression secondMapsTo;
                if(!context.FirstParameters.TryGetValue(first, out firstMapsTo) || firstMapsTo != second)
                    return false;
                if(!context.SecondParameters.TryGetValue(second, out secondMapsTo) || secondMapsTo != first)
                    return false;
                return true;
            }
            else
            {
                ParameterExpression firstMapsTo;
                ParameterExpression secondMapsTo;
                if(!context.FirstParameters.TryGetValue(first, out firstMapsTo))
                    context.FirstParameters.Add(first, second);
                else if(firstMapsTo != second)
                    return false;
                if(!context.SecondParameters.TryGetValue(second, out secondMapsTo))
                    context.SecondParameters.Add(second, first);
                else if(secondMapsTo != first)
                    return false;
                return true;
            }
        }

        private static bool EquivalentLabel(LabelTarget first, LabelTarget second, Context context)
        {
            if(first == null || second == null)
                return first == null && second == null;
            LabelTarget firstMapsTo;
            LabelTarget secondMapsTo;
            if (!context.FirstLabels.TryGetValue(first, out firstMapsTo))
                context.FirstLabels.Add(first, second);
            else if (firstMapsTo != second)
                return false;
            if (!context.SecondLabels.TryGetValue(second, out secondMapsTo))
                context.SecondLabels.Add(second, first);
            else if (secondMapsTo != first)
                return false;
            return true;

        }

        private static bool EquivalentUnary(UnaryExpression first, UnaryExpression second, Context context)
        {
            return first.Method == second.Method && Equivalent(first.Operand, second.Operand, context);
        }

        private static bool EquivalentBinary(BinaryExpression first, BinaryExpression second, Context context)
        {
            return first.Method == second.Method && Equivalent(first.Left, second.Left, context) && Equivalent(first.Conversion, second.Conversion, context) && Equivalent(first.Right, second.Right, context);
        }

        private static bool EquivalentBlock(BlockExpression first, BlockExpression second, Context context)
        {
            if(first.Variables.Count != second.Variables.Count)
                return false;
            for(int i = 0; i < first.Variables.Count; ++i)
            {
                context.FirstParameters.Add(first.Variables[i], second.Variables[i]);
                context.SecondParameters.Add(second.Variables[i], first.Variables[i]);
            }
            var result = Equivalent(first.Expressions, second.Expressions, context);
            for(int i = 0; i < first.Variables.Count; ++i)
            {
                context.FirstParameters.Remove(first.Variables[i]);
                context.SecondParameters.Remove(second.Variables[i]);
            }
            return result;
        }

        private static bool IsEachOrCurrentMethod(MethodInfo method)
        {
            return method.IsEachMethod() || method.IsCurrentMethod();
        }

        private static bool EquivalentMethods(MethodInfo first, MethodInfo second, Context context)
        {
            if(MembersEqual(first, second))
                return true;
            return !context.DistinguishEachAndCurrent && IsEachOrCurrentMethod(first) && IsEachOrCurrentMethod(second) && first.GetGenericArguments()[0] == second.GetGenericArguments()[0];
        }

        private static bool EquivalentCall(MethodCallExpression first, MethodCallExpression second, Context context)
        {
            return EquivalentMethods(first.Method, second.Method, context) && Equivalent(first.Object, second.Object, context.Strictly, context.DistinguishEachAndCurrent) && Equivalent(first.Arguments, second.Arguments, context);
        }

        private static bool EquivalentConditional(ConditionalExpression first, ConditionalExpression second, Context context)
        {
            return Equivalent(first.Test, second.Test, context) && Equivalent(first.IfTrue, second.IfTrue, context) && Equivalent(first.IfFalse, second.IfFalse, context);
        }

        private static bool EquivalentConstant(ConstantExpression first, ConstantExpression second, Context context)
        {
            return EquivalentObjects(first.Value, second.Value);
        }

        private static bool EquivalentObjects(object first, object second)
        {
            if (ReferenceEquals(first, null))
                return ReferenceEquals(second, null);
            return !ReferenceEquals(second, null) && (ReferenceEquals(first, second) || first == second || first.Equals(second) || EqualsArrays(first, second));
        }

        private static bool EqualsArrays(object first, object second)
        {
            var firstArr = first as Array;
            var secondArr = second as Array;
            if(firstArr != null && secondArr != null)
            {
                if(firstArr.Length != secondArr.Length)
                    return false;
                for(int i = 0; i < firstArr.Length; ++i)
                    if(!EquivalentObjects(firstArr.GetValue(i), secondArr.GetValue(i)))
                        return false;
                return true;
            }
            return false;
        }

        private static bool EquivalentDebugInfo(DebugInfoExpression first, DebugInfoExpression second, Context context)
        {
            throw new NotImplementedException();
        }

        private static bool EquivalentDefault(DefaultExpression first, DefaultExpression second, Context context)
        {
            return true;
        }

        private static bool EquivalentDynamic(DynamicExpression first, DynamicExpression second, Context context)
        {
            throw new NotImplementedException();
        }

        private static bool EquivalentExtension(Expression first, Expression second, Context context)
        {
            throw new NotImplementedException();
        }

        private static bool EquivalentGoto(GotoExpression first, GotoExpression second, Context context)
        {
            return first.Kind == second.Kind && EquivalentLabel(first.Target, second.Target, context) && Equivalent(first.Value, second.Value, context);
        }

        private static bool EquivalentIndex(IndexExpression first, IndexExpression second, Context context)
        {
            throw new NotImplementedException();
        }

        private static bool EquivalentInvoke(InvocationExpression first, InvocationExpression second, Context context)
        {
            return Equivalent(first.Expression, second.Expression, context) && Equivalent(first.Arguments, second.Arguments, context);
        }

        private static bool EquivalentLabel(LabelExpression first, LabelExpression second, Context context)
        {
            return EquivalentLabel(first.Target, second.Target, context) && Equivalent(first.DefaultValue, second.DefaultValue, context);
        }

        private static bool EquivalentLambda(LambdaExpression first, LambdaExpression second, Context context)
        {
            if(first.Parameters.Count != second.Parameters.Count)
                return false;
            for(int i = 0; i < first.Parameters.Count; ++i)
            {
                context.FirstParameters.Add(first.Parameters[i], second.Parameters[i]);
                context.SecondParameters.Add(second.Parameters[i], first.Parameters[i]);
            }
            var result = Equivalent(first.Body, second.Body, context);
            for(int i = 0; i < first.Parameters.Count; ++i)
            {
                context.FirstParameters.Remove(first.Parameters[i]);
                context.SecondParameters.Remove(second.Parameters[i]);
            }
            return result;
        }

        private static bool EquivalentListInit(ListInitExpression first, ListInitExpression second, Context context)
        {
            if(!Equivalent(first.NewExpression, second.NewExpression, context))
                return false;
            if(first.Initializers.Count != second.Initializers.Count)
                return false;
            for(int i = 0; i < first.Initializers.Count; ++i)
            {
                if(!EquivalentMethods(first.Initializers[i].AddMethod, second.Initializers[i].AddMethod, context))
                    return false;
                if(!Equivalent(first.Initializers[i].Arguments, second.Initializers[i].Arguments, context))
                    return false;
            }
            return true;
        }

        private static bool EquivalentLoop(LoopExpression first, LoopExpression second, Context context)
        {
            return EquivalentLabel(first.BreakLabel, second.BreakLabel, context) 
                && EquivalentLabel(first.ContinueLabel, second.ContinueLabel, context) 
                && Equivalent(first.Body, second.Body, context);
        }

        private static bool MembersEqual(MemberInfo first, MemberInfo second)
        {
            return first.Module == second.Module && first.MetadataToken == second.MetadataToken;
        }

        private static bool EquivalentMemberAccess(MemberExpression first, MemberExpression second, Context context)
        {
            return MembersEqual(first.Member, second.Member) && Equivalent(first.Expression, second.Expression, context);
        }

        private static bool EquivalentMemberInit(MemberInitExpression first, MemberInitExpression second, Context context)
        {
            if(!EquivalentNew(first.NewExpression, second.NewExpression, context))
                return false;
            if(first.Bindings.Count != second.Bindings.Count)
                return false;
            for(int i = 0; i < first.Bindings.Count; ++i)
            {
                var firstAssignment = (MemberAssignment)first.Bindings[i];
                var secondAssignment = (MemberAssignment)second.Bindings[i];
                if(firstAssignment.BindingType != secondAssignment.BindingType
                   || !MembersEqual(firstAssignment.Member, secondAssignment.Member)
                   || !Equivalent(firstAssignment.Expression, secondAssignment.Expression, context))
                    return false;
            }
            return true;
        }

        private static bool EquivalentNew(NewExpression first, NewExpression second, Context context)
        {
            return first.Constructor == second.Constructor && Equivalent(first.Arguments, second.Arguments, context);
        }

        private static bool EquivalentNewArray(NewArrayExpression first, NewArrayExpression second, Context context)
        {
            return Equivalent(first.Expressions, second.Expressions, context);
        }

        private static bool EquivalentRuntimeVariables(RuntimeVariablesExpression first, RuntimeVariablesExpression second, Context context)
        {
            throw new NotImplementedException();
        }

        private static bool EquivalentSwitch(SwitchExpression first, SwitchExpression second, Context context)
        {
            throw new NotImplementedException();
        }

        private static bool EquivalentTry(TryExpression first, TryExpression second, Context context)
        {
            throw new NotImplementedException();
        }

        private static bool EquivalentTypeBinary(TypeBinaryExpression first, TypeBinaryExpression second, Context context)
        {
            throw new NotImplementedException();
        }

        private class Context
        {
            public bool Strictly { get; set; }
            public bool DistinguishEachAndCurrent { get; set; }
            public Dictionary<ParameterExpression, ParameterExpression> FirstParameters { get; set; }
            public Dictionary<ParameterExpression, ParameterExpression> SecondParameters { get; set; }
            public Dictionary<LabelTarget, LabelTarget> FirstLabels { get; set; }
            public Dictionary<LabelTarget, LabelTarget> SecondLabels { get; set; }
        }
    }
}