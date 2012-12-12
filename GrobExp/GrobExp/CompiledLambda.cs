using System;

namespace GrobExp
{
    internal class CompiledLambda
    {
        public Delegate Delegate { get; set; }
        public string ILCode { get; set; }
    }
}