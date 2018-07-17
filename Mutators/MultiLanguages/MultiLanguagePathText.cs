using System;

namespace GrobExp.Mutators.MultiLanguages
{
    public abstract class MultiLanguagePathText : MultiLanguageTextBase, IComparable
    {
        public abstract int CompareTo(object obj);
    }
}