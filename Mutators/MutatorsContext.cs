using System.Collections.Generic;
using System.Linq;

namespace GrobExp.Mutators
{
    public abstract class MutatorsContext
    {
        public abstract string GetKey();

        public Dictionary<string, object> GetProperties()
        {
            var properties = GetType().GetProperties();
            var propertiesDict = properties.ToDictionary(prop => prop.Name, prop => prop.GetValue(this));
            return propertiesDict;
        }

        public static readonly MutatorsContext Empty = new EmptyMutatorsContext();
    }

    public sealed class EmptyMutatorsContext : MutatorsContext
    {
        public override string GetKey()
        {
            return "";
        }
    }
}