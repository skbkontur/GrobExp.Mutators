using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class DebugInfoExpressionEmitter : ExpressionEmitter<DebugInfoExpression>
    {
        protected override bool Emit(DebugInfoExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            resultType = typeof(void);
            if(context.DebugInfoGenerator == null)
                return false;
            if(node.IsClear && context.SequencePointCleared)
                return false;
            markSequencePoint(context.DebugInfoGenerator, context.Lambda, context.Method, context.Il, node);
            context.Il.Nop();
            context.SequencePointCleared = node.IsClear;
            return false;
        }

        private static Action<DebugInfoGenerator, LambdaExpression, MethodBase, GroboIL, DebugInfoExpression> BuildSequencePointMarker()
        {
            var parameterTypes = new[] {typeof(DebugInfoGenerator), typeof(LambdaExpression), typeof(MethodBase), typeof(GroboIL), typeof(DebugInfoExpression)};
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), parameterTypes, typeof(DebugInfoExpressionEmitter), true);
            var il = new GroboIL(dynamicMethod);
            il.Ldarg(0);
            il.Ldarg(1);
            il.Ldarg(2);
            il.Ldarg(3);
            il.Ldfld(typeof(GroboIL).GetField("il", BindingFlags.NonPublic | BindingFlags.Instance));
            il.Ldarg(4);
            var markSequencePointMethod = typeof(DebugInfoGenerator).GetMethod("MarkSequencePoint", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(LambdaExpression), typeof(MethodBase), typeof(ILGenerator), typeof(DebugInfoExpression) }, null);
            il.Call(markSequencePointMethod, typeof(DebugInfoGenerator));
            il.Ret();
            return (Action<DebugInfoGenerator, LambdaExpression, MethodBase, GroboIL, DebugInfoExpression>)dynamicMethod.CreateDelegate(typeof(Action<DebugInfoGenerator, LambdaExpression, MethodBase, GroboIL, DebugInfoExpression>));
        }

        private static readonly Action<DebugInfoGenerator, LambdaExpression, MethodInfo, GroboIL, DebugInfoExpression> markSequencePoint = BuildSequencePointMarker();
    }
}