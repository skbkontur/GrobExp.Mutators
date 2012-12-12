using System;
using System.Linq.Expressions;

using GrobExp;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class TestCheckArrayIndex
    {

        [Test]
        public void Test5()
        {
            Expression<Func<TestClassA, int?>> exp = o => o.ArrayB[0].X;
            Func<TestClassA, int?> compiledExp = LambdaCompiler.Compile(exp);
            Assert.That(compiledExp(null), Is.EqualTo(null));
            Assert.That(compiledExp(new TestClassA()), Is.EqualTo(null));
            Assert.That(compiledExp(new TestClassA { ArrayB = new TestClassB[] { null } }), Is.EqualTo(null));
            Assert.That(compiledExp(new TestClassA { ArrayB = new[] { new TestClassB() } }), Is.EqualTo(null));
            int? actual = compiledExp(new TestClassA { ArrayB = new[] { new TestClassB { X = 1 } } });
            Assert.That(actual, Is.EqualTo(1));
        }

        [Test]
        public void Test5x()
        {
            Expression<Func<TestClassA, int?>> exp = o => o.ArrayB[0].C.ArrayD[1].X;
            Func<TestClassA, int?> compiledExp = LambdaCompiler.Compile(exp);
            Assert.That(compiledExp(null), Is.EqualTo(null));
            Assert.That(compiledExp(new TestClassA()), Is.EqualTo(null));
            Assert.That(compiledExp(new TestClassA { ArrayB = new TestClassB[] { null } }), Is.EqualTo(null));
            Assert.That(compiledExp(new TestClassA { ArrayB = new[] { new TestClassB() } }), Is.EqualTo(null));
            Assert.That(compiledExp(new TestClassA { ArrayB = new[] { new TestClassB { C = new TestClassC() } } }), Is.EqualTo(null));
            Assert.That(compiledExp(new TestClassA { ArrayB = new[] { new TestClassB { C = new TestClassC { ArrayD = new TestClassD[0] } } } }), Is.EqualTo(null));
            Assert.That(compiledExp(new TestClassA { ArrayB = new[] { new TestClassB { C = new TestClassC { ArrayD = new[] { new TestClassD(), } } } } }), Is.EqualTo(null));
            Assert.That(compiledExp(new TestClassA { ArrayB = new[] { new TestClassB { C = new TestClassC { ArrayD = new[] { new TestClassD(), null, } } } } }), Is.EqualTo(null));
            Assert.That(compiledExp(new TestClassA { ArrayB = new[] { new TestClassB { C = new TestClassC { ArrayD = new[] { new TestClassD(), new TestClassD(), } } } } }), Is.EqualTo(null));
            Assert.That(compiledExp(new TestClassA { ArrayB = new[] { new TestClassB { C = new TestClassC { ArrayD = new[] { new TestClassD(), new TestClassD { X = 1 }, } } } } }), Is.EqualTo(1));
        }

        [Test]
        public void TestBadArrayIndex()
        {
            Expression<Func<TestClassA, int>> exp = a => a.IntArray[314159265];
            var compiledExp = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, compiledExp(new TestClassA { IntArray = new[] { 1, 2, 3 } }));
        }

        [Test]
        public void TestBadArrayIndex2()
        {
#pragma warning disable 251
            Expression<Func<TestClassA, int>> exp = a => a.IntArray[-1];
#pragma warning restore 251
            var compiledExp = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, compiledExp(new TestClassA { IntArray = new[] { 1, 2, 3 } }));
        }

        [Test]
        public void TestBadArrayIndex3()
        {
            Expression<Func<TestClassA, string>> exp = a => a.ArrayB[271828183].C.D.E.S;
            var compiledExp = LambdaCompiler.Compile(exp);
            Assert.AreEqual(null, compiledExp(new TestClassA { ArrayB = new[] { new TestClassB() } }));
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

            public readonly string S;
        }

        private class TestClassE
        {
            public string S { get; set; }
            public int X { get; set; }
        }
    }
}