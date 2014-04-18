using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GrobExp.Mutators
{
    public static class MethodInfoExtensions
    {
        public static bool IsExtension(this MethodInfo method)
        {
            return method.GetCustomAttributes(typeof(ExtensionAttribute), false).Any();
        }

        public static bool IsIndexerGetter(this MethodInfo method)
        {
            return method.Name == "get_Item";
        }
    }
}