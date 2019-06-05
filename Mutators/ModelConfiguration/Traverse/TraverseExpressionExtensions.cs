using System;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

namespace GrobExp.Mutators.ModelConfiguration.Traverse
{
    internal static class TraverseExpressionExtensions
    {
        [NotNull]
        public static Expression GetPathToChild([NotNull] this Expression path, [NotNull] ModelConfigurationEdge edge)
        {
            if (edge.IsArrayIndex)
                return path.MakeArrayIndex((int)edge.Value);

            if (edge.IsMemberAccess)
                return path.MakeMemberAccess((MemberInfo)edge.Value);

            if (edge.IsEachMethod)
                return path.MakeEachCall();

            if (edge.IsConvertation)
                return path.MakeTypeConversion((Type)edge.Value);

            if (edge.IsIndexerParams)
                return path.MakeIndexerCall((object[])edge.Value, path.Type);

            throw new InvalidOperationException($"Unknown edge value: {edge.Value}");
        }

    }
}