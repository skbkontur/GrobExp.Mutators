using System;
using System.Linq;
using System.Linq.Expressions;

using GrobExp;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class Test // todo растащить на куски
    {
        [Test]
        public void TestConditional()
        {
            Expression<Func<TestClassA, int>> exp = a => a.Bool ? 1 : -1;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(1, f(new TestClassA {Bool = true}));
            Assert.AreEqual(-1, f(null));
            Assert.AreEqual(-1, f(new TestClassA()));
        }

        [Test]
        public void TestToStringOfGuid()
        {
            Expression<Func<TestClassA, string>> exp = f => "_xxx_" + f.Guid.ToString();
            Func<TestClassA, string> compiledExp = LambdaCompiler.Compile(exp);
            Assert.That(compiledExp(new TestClassA {Guid = new Guid("8DCBF7DF-772A-4A9C-81F0-D4B25C183ACE")}), Is.EqualTo("_xxx_8DCBF7DF-772A-4A9C-81F0-D4B25C183ACE").IgnoreCase);
        }

        [Test]
        public void Test9()
        {
            Expression<Func<TestClassA, int>> exp = o => (o.B ?? new TestClassB {Y = 3}).Y;
            Func<TestClassA, int> compiledExp = LambdaCompiler.Compile(exp);
            Assert.That(compiledExp(null), Is.EqualTo(3));
            Assert.That(compiledExp(new TestClassA()), Is.EqualTo(3));
            Assert.That(compiledExp(new TestClassA {B = new TestClassB {Y = 2}}), Is.EqualTo(2));
        }

        [Test]
        public void Test23()
        {
            Expression<Func<TestClassA, bool>> exp = o => o.NullableBool == false;
            Func<TestClassA, bool> compiledExp = LambdaCompiler.Compile(exp);
            Assert.IsFalse(compiledExp(null));
            Assert.IsFalse(compiledExp(new TestClassA()));
        }

        [Test]
        public void TestNullableGuidNoCoalesce()
        {
            Expression<Func<TestClassA, bool>> exp = o => o.NullableGuid == null;
            Func<TestClassA, bool> compiledExp = LambdaCompiler.Compile(exp);
            Assert.IsFalse(compiledExp(new TestClassA {NullableGuid = Guid.Empty}));
        }

        [Test]
        public void TestCoalesce1()
        {
            Expression<Func<TestClassA, int>> exp = o => o.B.X ?? 1;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(1, f(null));
            Assert.AreEqual(1, f(new TestClassA()));
            Assert.AreEqual(1, f(new TestClassA {B = new TestClassB()}));
            Assert.AreEqual(2, f(new TestClassA {B = new TestClassB {X = 2}}));
        }

        [Test]
        public void TestLazyEvaluation()
        {
            Expression<Func<TestClassA, bool>> exp = o => o.A != null && Y(o.A) > 0;
            Func<TestClassA, bool> compiledExp = LambdaCompiler.Compile(exp);
            Assert.That(compiledExp(null), Is.False);
            Assert.That(compiledExp(new TestClassA()), Is.False);
            Assert.That(compiledExp(new TestClassA {A = new TestClassA()}), Is.False);
            Assert.That(compiledExp(new TestClassA {A = new TestClassA {Y = 1}}), Is.True);
        }

        [Test]
        public void TestLazyEvaluation2()
        {
            Expression<Func<int?, bool>> exp = o => o != null && o.ToString() == "1";
            Func<int?, bool> compiledExp = LambdaCompiler.Compile(exp);
            Assert.That(compiledExp(null), Is.False);
            Assert.That(compiledExp(1), Is.True);
        }

        [Test]
        public void TestStaticMethod()
        {
            Expression<Func<TestClassA, TestClassA, int>> exp = (x, y) => NotExtension(x, y);
            Func<TestClassA, TestClassA, int> compiledExp = LambdaCompiler.Compile(exp);

            Assert.That(compiledExp(new TestClassA(), new TestClassA()), Is.EqualTo(3), "!null,!null");
            Assert.That(compiledExp(new TestClassA(), null), Is.EqualTo(2), "!null,null");
            Assert.That(compiledExp(null, null), Is.EqualTo(1), "null,null");
        }

        [Test]
        public void TestStaticMethodAsSubChain()
        {
            Expression<Func<TestClassA, TestClassA, int>> exp = (x, y) => NotExtension2(x.A, y.A).Y;
            Func<TestClassA, TestClassA, int> compiledExp = LambdaCompiler.Compile(exp);

            Assert.That(compiledExp(new TestClassA {A = new TestClassA()}, new TestClassA {A = new TestClassA()}), Is.EqualTo(3), "!null,!null");
            Assert.That(compiledExp(new TestClassA {A = new TestClassA()}, null), Is.EqualTo(2), "!null,null");
            Assert.That(compiledExp(null, null), Is.EqualTo(1), "null,null");
        }

        [Test]
        public void TestCompileExtendedExpressionWithClosure()
        {
            var closure = 11;
            Expression<Func<TestClassA, bool>> exp = o => closure == o.Y;
            Func<TestClassA, bool> compiledExp = LambdaCompiler.Compile(exp);
            Assert.That(compiledExp(new TestClassA {Y = 11}), Is.True);
        }

        [Test]
        public void TestNullableValidType()
        {
            Expression<Func<int?, string>> exp = o => o == null ? "" : o.ToString();
            Func<int?, string> compiledExp = LambdaCompiler.Compile(exp);
            Assert.AreEqual(compiledExp(null), "");
        }

        [Test]
        public void TestExtendFunctionArguments()
        {
            Expression<Func<TestClassA, int>> exp = o => zzz(o.X > 0);
            Func<TestClassA, int> compiledExp = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, compiledExp(null));
            Assert.AreEqual(0, compiledExp(new TestClassA()));
            Assert.AreEqual(1, compiledExp(new TestClassA {X = 1}));
        }

        [Test]
        public void Test99()
        {
            Expression<Func<int[], bool>> exp = o => 11 == o[o.Count() - 1];
            Func<int[], bool> compiledExp = LambdaCompiler.Compile(exp);
            Assert.That(compiledExp(new[] {1, 11}), Is.True);
        }

        [Test]
        public void TestExtendForNullableValueTypes()
        {
            Expression<Func<int?, string>> exp = s => s.ToString();
            Func<int?, string> compiledExp = LambdaCompiler.Compile(exp);
            Assert.AreEqual(null, compiledExp(null));
            Assert.AreEqual("1", compiledExp(1));
        }

        [Test]
        public void TestExtendForNullableValueTypes2()
        {
            Expression<Func<int?, bool>> exp = s => s == 0;
            Func<int?, bool> compiledExp = LambdaCompiler.Compile(exp);
            Assert.IsFalse(compiledExp(null));
        }

        public struct TestStructA
        {
            public string S { get; set; }
            public TestStructB[] ArrayB { get; set; }
            public int? X { get; set; }
            public int Y { get; set; }
        }

        public struct TestStructB
        {
            public string S { get; set; }
        }

        private int zzz(bool qxx)
        {
            return qxx ? 1 : 0;
        }

        private static int NotExtension(TestClassA x, TestClassA y)
        {
            if(x == null) return 1;
            if(y == null) return 2;
            return 3;
        }

        private static TestClassA NotExtension2(TestClassA x, TestClassA y)
        {
            if(x == null) return new TestClassA {Y = 1};
            if(y == null) return new TestClassA {Y = 2};
            return new TestClassA {Y = 3};
        }

        private static int Y(TestClassA a)
        {
            return a.Y;
        }

        private class TestClassA
        {
            public int F(bool b)
            {
                return b ? 1 : 0;
            }

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