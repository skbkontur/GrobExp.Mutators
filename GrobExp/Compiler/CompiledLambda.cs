using System;
using System.Reflection;

using GrEmit;

namespace GrobExp.Compiler
{
    internal class CompiledLambda
    {
        public Delegate Delegate { get; set; }
        public MethodInfo Method { get; set; }
        public ILCode ILCode { get; set; }
    }
}