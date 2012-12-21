using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class IndexExpressionEmitter : ExpressionEmitter<IndexExpression>
    {
        protected override bool Emit(IndexExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            if(node.Object != null && node.Object.Type.IsArray && node.Object.Type.GetArrayRank() == 1)
                return ExpressionEmittersCollection.Emit(Expression.ArrayIndex(node.Object, node.Arguments.Single()), context, returnDefaultValueLabel, whatReturn, extend, out resultType);
            bool result = false;
            if(node.Object != null)
            {
                Type objectType;
                result = ExpressionEmittersCollection.Emit(node.Object, context, returnDefaultValueLabel, ResultType.ByRefValueTypesOnly, extend, out objectType);
                if(objectType.IsValueType)
                {
                    using(var temp = context.DeclareLocal(objectType))
                    {
                        context.Il.Stloc(temp);
                        context.Il.Ldloca(temp);
                    }
                }
                if(context.Options.HasFlag(CompilerOptions.CheckNullReferences))
                    result |= context.EmitNullChecking(objectType, returnDefaultValueLabel);
            }
            if(node.Indexer != null)
            {
                context.EmitLoadArguments(node.Arguments.ToArray());
                MethodInfo getter = node.Indexer.GetGetMethod(true);
                if (getter == null)
                    throw new MissingMethodException(node.Indexer.ReflectedType.ToString(), "get_" + node.Indexer.Name);
                context.Il.Call(getter);
            }
            else
            {
                if(node.Object == null)
                    throw new InvalidOperationException("Static array indexing is a weird thing");
                Type arrayType = node.Object.Type;
                if(!arrayType.IsArray)
                    throw new InvalidOperationException("An array expected");
                int rank = arrayType.GetArrayRank();
                if(rank != node.Arguments.Count)
                    throw new InvalidOperationException("Incorrect number of indeces '" + node.Arguments.Count + "' provided to access an array with rank '" + rank + "'");
                Type indexType = node.Arguments.First().Type;
                if(indexType != typeof(int) && indexType != typeof(long))
                    throw new InvalidOperationException("Indexing array with an index of type '" + indexType + "' is not allowed");
                context.Il.Ldc_I4(node.Arguments.Count);
                context.Il.Newarr(indexType);
                for(int i = 0; i < node.Arguments.Count; ++i)
                {
                    context.Il.Dup();
                    context.Il.Ldc_I4(i);
                    Type argumentType;
                    context.EmitLoadArgument(node.Arguments[i], false, out argumentType);
                    if(argumentType != indexType)
                        throw new InvalidOperationException("Expected '" + indexType + "' but was '" + argumentType + "'");
                    context.Il.Stelem(indexType);
                }
                MethodInfo getValueMethod = arrayType.GetMethod("GetValue", new[] {indexType.MakeArrayType()});
                if(getValueMethod == null)
                    throw new MissingMethodException(arrayType.ToString(), "GetValues");
                context.Il.Call(getValueMethod);
            }
            resultType = node.Type;
            return result;
        }
    }
}