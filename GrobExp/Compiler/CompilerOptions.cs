using System;

namespace GrobExp
{
    [Flags]
    public enum CompilerOptions
    {
        None = 0,
        CheckNullReferences = 1,
        CheckArrayIndexes = 2,
        ExtendOnAssign = 4,
        UseTernaryLogic = 8,
        All = CheckNullReferences | CheckArrayIndexes | ExtendOnAssign | UseTernaryLogic
    }
}