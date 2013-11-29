using System;
using System.Linq;

using GrobExp.Mutators.MultiLanguages;

namespace GrobExp.Mutators
{
    [MultiLanguageTextType("SimplePathFormatterText")]
    public class SimplePathFormatterText : MultiLanguagePathText, IComparable<SimplePathFormatterText>
    {
        public int CompareTo(SimplePathFormatterText other)
        {
            if(other == null) return 1;
            if(ReferenceEquals(this, other)) return 0;
            int length = Paths == null ? 0 : Paths.Length;
            int otherLength = other.Paths == null ? 0 : other.Paths.Length;
            if(length == 0)
                return otherLength == 0 ? 0 : -1;
            if(otherLength == 0)
                return 1;
            for(int i = 0; i < length && i < otherLength; ++i)
            {
                var current = Paths[i].CompareTo(other.Paths[i]);
                if(current != 0)
                    return current;
            }
            return length.CompareTo(otherLength);
        }

        public override int CompareTo(object obj)
        {
            if(obj == null) return 1;
            if(!(obj is SimplePathFormatterText))
                throw new ArgumentException();
            return CompareTo((SimplePathFormatterText)obj);
        }

        public string[] Paths { get; set; }

        protected override void Register()
        {
            Register("RU");
            Register("EN");
        }

        private void Register(string language)
        {
            Register(language, () => Paths == null || Paths.Length == 0 ? "" : (Paths.Length == 1 ? "'" + Paths[0] + "'" : "[" + Paths.Select(path => "'" + path + "'").ToArray().JoinIgnoreEmpty(", ") + "]"));
        }
    }
}