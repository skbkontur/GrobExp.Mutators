using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

using GrEmit;

namespace GrobExp
{
    public class ExpressionClosureBuilder : ExpressionVisitor
    {
        public ExpressionClosureBuilder(LambdaExpression lambda)
        {
            this.lambda = lambda;
            string name = "Closure_" + (uint)Interlocked.Increment(ref closureId);
            typeBuilder = LambdaCompiler.Module.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, typeof(Closure));
        }

        public Type Build(out Dictionary<ConstantExpression, FieldInfo> constants, out Dictionary<ParameterExpression, FieldInfo> parameters, out Func<Closure> closureCreator)
        {
            Visit(lambda);
            if(hasSubLambdas)
                typeBuilder.DefineField("delegates", typeof(Delegate[]), FieldAttributes.Public | FieldAttributes.InitOnly);
            Type result = typeBuilder.CreateType();
            closureCreator = BuildClosureCreator(result);
            constants = this.constants.ToDictionary(item => item.Key, item => result.GetField(item.Value.Name));
            parameters = this.parameters.ToDictionary(item => item.Key, item => result.GetField(item.Value.Name));
            return result;
        }

        private bool hasSubLambdas;

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if(node != lambda)
                hasSubLambdas = true;
            localParameters.Push(new HashSet<ParameterExpression>(node.Parameters));
            var res = base.VisitLambda(node);
            localParameters.Pop();
            return res;
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            var peek = localParameters.Peek();
            foreach(var variable in node.Variables)
                peek.Add(variable);
            var res = base.VisitBlock(node);
            foreach(var variable in node.Variables)
                peek.Remove(variable);
            return res;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if(node.Value == null || node.Type.IsPrimitive || node.Type == typeof(string))
                return node;
            var key = new KeyValuePair<Type, object>(node.Type, node.Value);
            var field = (FieldInfo)hashtable[key];
            if(field == null)
            {
                field = typeBuilder.DefineField(GetFieldName(node.Type), GetFieldType(node.Type), FieldAttributes.Public | FieldAttributes.InitOnly);
                hashtable[key] = field;
            }
            if(!constants.ContainsKey(node))
                constants.Add(node, field);
            return node;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            var peek = localParameters.Peek();
            if(!peek.Contains(node) && !parameters.ContainsKey(node))
            {
                FieldInfo field = typeBuilder.DefineField(GetFieldName(node.Type), GetFieldType(node.Type), FieldAttributes.Public);
                parameters.Add(node, field);
            }
            return base.VisitParameter(node);
        }

        private Func<Closure> BuildClosureCreator(Type type)
        {
            var method = new DynamicMethod("Create_" + type.Name, type, new[] {typeof(object[])}, LambdaCompiler.Module, true);
            var il = new GroboIL(method);
            var consts = new object[hashtable.Count];
            int index = 0;
            il.Newobj(type.GetConstructor(Type.EmptyTypes));
            foreach(DictionaryEntry entry in hashtable)
            {
                var pair = (KeyValuePair<Type, object>)entry.Key;
                var constType = pair.Key;
                consts[index] = pair.Value;
                il.Dup();
                il.Ldarg(0);
                il.Ldc_I4(index++);
                il.Ldelem(typeof(object));
                string name = ((FieldInfo)entry.Value).Name;
                var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
                if(field == null)
                    throw new MissingFieldException(type.Name, name);
                if(constType.IsValueType)
                {
                    il.Unbox_Any(constType);
                    if(field.FieldType != constType)
                    {
                        var constructor = field.FieldType.GetConstructor(new[] {constType});
                        if(constructor == null)
                            throw new InvalidOperationException("Missing constructor of type '" + Format(field.FieldType) + "' with parameter of type '" + Format(constType) + "'");
                        il.Newobj(constructor);
                    }
                }
                else if(field.FieldType != constType)
                    throw new InvalidOperationException("Attempt to assign a value of type '" + Format(constType) + "' to field of type '" + Format(field.FieldType) + "'");
                il.Stfld(field);
            }
            il.Ret();
            var func = (Func<object[], Closure>)method.CreateDelegate(typeof(Func<object[], Closure>));
            return () => func(consts);
        }

        private static Type GetFieldType(Type type)
        {
            return (type.IsNestedPrivate || type.IsNotPublic) && type.IsValueType
                       ? typeof(StrongBox<>).MakeGenericType(new[] {type})
                       : type;
        }

        private static string Format(Type type)
        {
            if(!type.IsGenericType)
                return type.Name;
            return type.Name + "<" + string.Join(", ", type.GetGenericArguments().Select(Format)) + ">";
        }

        private string GetFieldName(Type type)
        {
            return Format(type) + "_" + fieldId++;
        }

        private static int closureId;
        private int fieldId;

        private readonly LambdaExpression lambda;
        private readonly Stack<HashSet<ParameterExpression>> localParameters = new Stack<HashSet<ParameterExpression>>();

        private readonly Hashtable hashtable = new Hashtable();
        private readonly Dictionary<ConstantExpression, FieldInfo> constants = new Dictionary<ConstantExpression, FieldInfo>();
        private readonly Dictionary<ParameterExpression, FieldInfo> parameters = new Dictionary<ParameterExpression, FieldInfo>();

        private readonly TypeBuilder typeBuilder;
    }
}