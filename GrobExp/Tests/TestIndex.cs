using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using GrobExp;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class TestIndex
    {

        [Test]
        public void TestMultidimensionalArray1()
        {
            Expression<Func<TestClassA, string>> exp = o => o.StringArray[1, 2];
            var f = LambdaCompiler.Compile(exp);
            var a = new TestClassA {StringArray = new string[2,3]};
            a.StringArray[1, 2] = "zzz";
            Assert.AreEqual("zzz", f(a));
        }

        [Test]
        public void TestMultidimensionalArray2()
        {
            Expression<Func<TestClassA, string[,]>> path = o => o.StringArray;
            Expression<Func<TestClassA, string>> exp = Expression.Lambda<Func<TestClassA, string>>(Expression.ArrayAccess(path.Body, Expression.Constant(1), Expression.Constant(2)), path.Parameters);
            var f = LambdaCompiler.Compile(exp);
            var a = new TestClassA {StringArray = new string[2,3]};
            a.StringArray[1, 2] = "zzz";
            Assert.AreEqual("zzz", f(a));
        }

        private struct TestStructA
        {
            public string S { get; set; }
            public TestStructB[] ArrayB { get; set; }
            public int? X { get; set; }
            public int Y { get; set; }
        }

        private struct TestStructB
        {
            public string S { get; set; }
            public int Y { get; set; }
        }

        private class TestClassA
        {
            public string S { get; set; }
            public TestClassA A { get; set; }
            public TestClassB B { get; set; }
            public TestClassB[] ArrayB { get; set; }
            public int[] IntArray { get; set; }
            public int? X;
            public Guid Guid = Guid.Empty;
            public Guid? NullableGuid;
            public bool? NullableBool;
            public int Y;
            public bool Bool;
            public string[,] StringArray { get; set; }

            public int F(bool b)
            {
                return b ? 1 : 0;
            }
        }

        private class TestClassB
        {
            public int? F2(int? x)
            {
                return x;
            }

            public int? F( /*Qzz*/ int a, int b)
            {
                return b;
            }

            public string S { get; set; }

            public TestClassC C { get; set; }
            public int? X;
            public int Y;
        }

        private class TestClassC
        {
            public string S { get; set; }

            public TestClassD D { get; set; }

            public TestClassD[] ArrayD { get; set; }
        }

        private class TestClassD
        {
            public TestClassE E { get; set; }
            public TestClassE[] ArrayE { get; set; }
            public string Z { get; set; }

            public int? X { get; set; }

            public string S;
        }

        private class TestClassE
        {
            public string S { get; set; }
            public int X { get; set; }
        }

    }
}