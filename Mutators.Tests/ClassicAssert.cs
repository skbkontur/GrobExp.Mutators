#if NET7_0 || NET48
using System.Collections;

// ReSharper disable once CheckNamespace
namespace NUnit.Framework.Legacy
{
    public static class ClassicAssert
    {
        public static void IsNull(object anObject)
        {
            Assert.IsNull(anObject);
        }

        public static void IsNotNull(object anObject)
        {
            Assert.IsNotNull(anObject);
        }

        public static void IsEmpty(IEnumerable collection)
        {
            Assert.IsEmpty(collection);
        }

        public static void IsNotEmpty(IEnumerable collection)
        {
            Assert.IsNotEmpty(collection);
        }

        public static void AreEqual(object expected, object actual, string message = null)
        {
            Assert.AreEqual(expected, actual, message);
        }

        public static void AreNotEqual(object expected, object actual)
        {
            Assert.AreNotEqual(expected, actual);
        }

        public static void AreSame(object expected, object actual)
        {
            Assert.AreSame(expected, actual);
        }

        public static void AreNotSame(object expected, object actual)
        {
            Assert.AreNotSame(expected, actual);
        }

        public static void IsTrue(bool condition, string message = null)
        {
            Assert.IsTrue(condition, message);
        }

        public static void IsFalse(bool condition)
        {
            Assert.IsFalse(condition);
        }

        public static void True(bool condition, string message, params object[] args)
        {
            Assert.True(condition, message, args);
        }

        public static void False(bool condition, string message, params object[] args)
        {
            Assert.False(condition, message, args);
        }
    }
}

#endif