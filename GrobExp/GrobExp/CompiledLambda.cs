using System;
using System.Reflection;

namespace GrobExp
{
    internal class CompiledLambda
    {
        public Delegate Delegate { get; set; }
        public MethodInfo Method { get; set; }
        public string ILCode { get; set; }
    }
}