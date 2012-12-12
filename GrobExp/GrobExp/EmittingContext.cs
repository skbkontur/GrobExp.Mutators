using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GrobExp
{
    internal class EmittingContext
    {
        public LocalHolder DeclareLocal(Type type)
        {
            Queue<GrobIL.Local> queue;
            if(!locals.TryGetValue(type, out queue))
            {
                queue = new Queue<GrobIL.Local>();
                locals.Add(type, queue);
            }
            if(queue.Count == 0)
                queue.Enqueue(Il.DeclareLocal(type));
            return new LocalHolder(this, type, queue.Dequeue());
        }

        public void FreeLocal(Type type, GrobIL.Local local)
        {
            locals[type].Enqueue(local);
        }

        public CompilerOptions Options { get; set; }
        public ParameterExpression[] Parameters { get; set; }
        public ParameterExpression ClosureParameter { get; set; }
        public List<CompiledLambda> CompiledLambdas { get; set; }
        public GrobIL Il { get; set; }
        public Dictionary<ParameterExpression, LocalHolder> VariablesToLocals { get { return variablesToLocals; } }
        public Dictionary<LabelTarget, GrobIL.Label> Labels { get { return labels; } }
        public Stack<ParameterExpression> Variables { get { return variables; } }

        public bool CanReturn { get { return Options.HasFlag(CompilerOptions.CheckNullReferences) || Options.HasFlag(CompilerOptions.CheckArrayIndexes); } }

        public class LocalHolder : IDisposable
        {
            public LocalHolder(EmittingContext owner, Type type, GrobIL.Local local)
            {
                this.owner = owner;
                this.type = type;
                this.local = local;
            }

            public void Dispose()
            {
                owner.FreeLocal(type, local);
            }

            public static implicit operator GrobIL.Local(LocalHolder holder)
            {
                return holder.local;
            }

            private readonly EmittingContext owner;
            private readonly Type type;
            private readonly GrobIL.Local local;
        }

        private readonly Dictionary<ParameterExpression, LocalHolder> variablesToLocals = new Dictionary<ParameterExpression, LocalHolder>();
        private readonly Stack<ParameterExpression> variables = new Stack<ParameterExpression>();
        private readonly Dictionary<LabelTarget, GrobIL.Label> labels = new Dictionary<LabelTarget, GrobIL.Label>();

        private readonly Dictionary<Type, Queue<GrobIL.Local>> locals = new Dictionary<Type, Queue<GrobIL.Local>>();
    }
}