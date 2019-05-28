using System;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators;
using GrobExp.Mutators.ModelConfiguration;

namespace Mutators.Tests.Helpers
{
    public static class TestExpressionExtensions
    {
        public static Expression Goto(this Expression path, ModelConfigurationEdge edge)
        {
            if (edge.IsArrayIndex)
                return path.MakeArrayIndex((int)edge.Value);
            if (edge.IsMemberAccess)
                return path.MakeMemberAccess((MemberInfo)edge.Value);
            if (edge.IsEachMethod)
                return path.MakeEachCall(path.Type.GetElementType());
            if (edge.IsConvertation)
                return path.MakeConvertation((Type)edge.Value);
            if (edge.IsIndexerParams)
                return path.MakeIndexerCall((object[])edge.Value, path.Type);
            var methodInfo = (MethodInfo)edge.Value;
            throw new InvalidOperationException();
        }
    }
}