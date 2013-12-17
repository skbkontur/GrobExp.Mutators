using System.Linq.Expressions;

using GroBuf;
using GroBuf.DataMembersExtracters;

using GrobExp.Mutators;

using NUnit.Framework;

namespace Mutators.Tests
{
    public static class ObjectComparer
    {
        public static void AssertEqualsToUsingGrobuf<T>(this T actual, T expected, string message = "", params object[] args)
        {
            var expectedBytes = serializer.Serialize(expected);
            var actualBytes = serializer.Serialize(actual);
            CollectionAssert.AreEqual(expectedBytes, actualBytes, message, args);
        }

        public static void AssertEqualsExpression(this Expression actual, Expression expected)
        {
            Assert.AreEqual(ExpressionCompiler.DebugViewGetter(actual.Simplify()), ExpressionCompiler.DebugViewGetter(expected.Simplify()));
        }

        public static void AssertEqualsExpression<T>(this Expression<T> actual, Expression<T> expected)
        {
            Assert.AreEqual(ExpressionCompiler.DebugViewGetter(actual.Simplify()), ExpressionCompiler.DebugViewGetter(expected.Simplify()));
        }

        private static readonly ISerializer serializer = new Serializer(new AllFieldsExtractor());
    }

    public class Serializer: SerializerBase
    {
        public Serializer(IDataMembersExtractor dataMembersExtractor, IGroBufCustomSerializerCollection customSerializerCollection = null, GroBufOptions options = GroBufOptions.None)
            : base(dataMembersExtractor, customSerializerCollection, options)
        {
        }
    }
}