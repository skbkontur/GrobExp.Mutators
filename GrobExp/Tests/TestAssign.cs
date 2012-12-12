using System;
using System.Linq.Expressions;

using GrobExp;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class TestAssign
    {
        [Test]
        public void TestAssign1()
        {
            var parameter = Expression.Parameter(typeof(int));
            var assign = Expression.Assign(parameter, Expression.Constant(-1));
            var exp = Expression.Lambda<Func<int, int>>(assign, parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(-1, f(1));
        }

        [Test]
        public void TestAssign2()
        {
            var parameter = Expression.Parameter(typeof(int));
            var variable = Expression.Parameter(typeof(int));
            var assign = Expression.Assign(variable, parameter);
            var body = Expression.Block(typeof(int), new[] { variable }, assign);
            var exp = Expression.Lambda<Func<int, int>>(body, parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(1, f(1));
        }

        [Test]
        public void TestAssignWithExtend1()
        {
            Expression<Func<TestClassA, string>> exp = a => a.B.S;
            Expression<Func<TestClassA, string>> exp2 = Expression.Lambda<Func<TestClassA, string>>(Expression.Assign(exp.Body, Expression.Constant("zzz")), exp.Parameters);
            var f = LambdaCompiler.Compile(exp2);
            var o = new TestClassA();
            Assert.AreEqual("zzz", f(o));
            Assert.IsNotNull(o.B);
            Assert.AreEqual("zzz", o.B.S);
        }

        [Test]
        public void TestAssignWithExtend1Struct()
        {
            Expression<Func<TestClassA, string>> exp = a => a.structA.b.S;
            Expression<Func<TestClassA, string>> exp2 = Expression.Lambda<Func<TestClassA, string>>(Expression.Assign(exp.Body, Expression.Constant("zzz")), exp.Parameters);
            var f = LambdaCompiler.Compile(exp2);
            var o = new TestClassA();
            Assert.AreEqual("zzz", f(o));
            Assert.AreEqual("zzz", o.structA.b.S);
        }

        [Test]
        public void TestAssignWithExtend2()
        {
            Expression<Func<TestClassA, string>> path1 = a => a.B.S;
            Expression<Func<TestClassA, string>> path2 = a => a.B.C.S;
            Expression body = Expression.Block(typeof(string), Expression.Assign(path1.Body, Expression.Constant("zzz")), Expression.Assign(new ParameterReplacer(path2.Parameters[0], path1.Parameters[0]).Visit(path2.Body), Expression.Constant("qxx")));
            Expression<Func<TestClassA, string>> exp = Expression.Lambda<Func<TestClassA, string>>(body, path1.Parameters);
            var f = LambdaCompiler.Compile(exp);
            var o = new TestClassA();
            Assert.AreEqual("qxx", f(o));
            Assert.IsNotNull(o.B);
            Assert.AreEqual("zzz", o.B.S);
            Assert.IsNotNull(o.B.C);
            Assert.AreEqual("qxx", o.B.C.S);
        }

        [Test]
        public void TestAssignWithExtend3()
        {
            Expression<Func<TestClassA, string>> path1 = a => a.B.S;
            Expression<Func<TestClassA, string>> path2 = a => a.B.C.S;
            Expression body = Expression.Block(typeof(void), Expression.Assign(path1.Body, Expression.Constant("zzz")), Expression.Assign(new ParameterReplacer(path2.Parameters[0], path1.Parameters[0]).Visit(path2.Body), Expression.Constant("qxx")));
            Expression<Action<TestClassA>> exp = Expression.Lambda<Action<TestClassA>>(body, path1.Parameters);
            var f = LambdaCompiler.Compile(exp);
            var o = new TestClassA();
            f(o);
            Assert.IsNotNull(o.B);
            Assert.AreEqual("zzz", o.B.S);
            Assert.IsNotNull(o.B.C);
            Assert.AreEqual("qxx", o.B.C.S);
        }

        private struct TestStructA
        {
            public string S { get; set; }
            public TestStructB b;
            public int? X { get; set; }
            public int Y { get; set; }
        }

        private struct TestStructB
        {
            public string S { get; set; }
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
            public TestStructA structA;

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

        private struct Qzz
        {
            public long X;
        }
    }
}