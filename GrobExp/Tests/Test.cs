using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

using GrobExp;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class Test // todo растащить на куски
    {
        [Test]
        public void TestConstsAreFreedAfterGarbageCollecting()
        {
            var weakRef = DoTestConstsAreFreedAfterGarbageCollecting();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Assert.IsFalse(weakRef.IsAlive);
        }

        private static WeakReference DoTestConstsAreFreedAfterGarbageCollecting()
        {
            var a = new TestClassA { S = "qxx" };
            var result = new WeakReference(a);
            Expression<Func<TestClassA, string>> path = o => o.S;
            var exp = Expression.Lambda<Func<TestClassA, bool>>(Expression.Equal(path.Body, Expression.MakeMemberAccess(Expression.Constant(a), typeof(TestClassA).GetProperty("S"))), path.Parameters);
            var f = LambdaCompiler.Compile(exp);
            Assert.IsTrue(f(new TestClassA {S = "qxx"}));
            Assert.IsFalse(f(new TestClassA {S = "qzz"}));
            return result;
        }

        [Test]
        public void TestMultiThread()
        {
            ParameterExpression list = Expression.Variable(typeof(List<string>));
            Expression listCreate = Expression.Assign(list, Expression.New(typeof(List<string>)));
            Expression<Func<TestClassA, TestClassB[]>> pathToArray = a => a.ArrayB;
            Expression<Func<TestClassB, string>> pathToS = b => b.S;
            Expression addToList = Expression.Call(list, typeof(List<string>).GetMethod("Add", new[] {typeof(string)}), pathToS.Body);
            MethodCallExpression forEach = Expression.Call(forEachMethod.MakeGenericMethod(new[] {typeof(TestClassB)}), pathToArray.Body, Expression.Lambda<Action<TestClassB>>(addToList, pathToS.Parameters));
            var body = Expression.Block(typeof(List<string>), new[] {list}, listCreate, forEach, list);
            var exp = Expression.Lambda<Func<TestClassA, List<string>>>(body, pathToArray.Parameters);
            var f = LambdaCompiler.Compile(exp);
            wasBug = false;
            var thread = new Thread(Run);
            thread.Start(f);
            Run(f);
            Assert.IsFalse(wasBug);
        }

        [Test, Ignore]
        public void TestPopInt32()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(string), new[] {typeof(TestClassA)}, typeof(Test).Module, true);
            var il = method.GetILGenerator(); //new GroboIL(method);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Dup);
            il.EmitCall(OpCodes.Call, typeof(TestClassA).GetProperty("E", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), null);
            var temp = il.DeclareLocal(typeof(long));
            il.Emit(OpCodes.Stloc, temp);
            //il.Pop();
            il.EmitCall(OpCodes.Call, typeof(TestClassA).GetProperty("S", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), null);
            il.Emit(OpCodes.Ret);
            var func = (Func<TestClassA, string>)method.CreateDelegate(typeof(Func<TestClassA, string>));
            Assert.AreEqual("zzz", func(new TestClassA {S = "zzz"}));
        }

        [Test]
        public void TestConvertToEnum()
        {
            Expression<Func<TestEnum, Enum>> exp = x => (Enum)x;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(TestEnum.Two, f(TestEnum.Two));
        }

        [Test]
        public void TestConvertFromEnum()
        {
            Expression<Func<Enum, TestEnum>> exp = x => (TestEnum)x;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(TestEnum.Two, f(TestEnum.Two));
        }

        [Test]
        public void TestConditional1()
        {
            Expression<Func<TestClassA, int>> exp = a => a.Bool ? 1 : -1;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(1, f(new TestClassA {Bool = true}));
            Assert.AreEqual(-1, f(null));
            Assert.AreEqual(-1, f(new TestClassA()));
        }

        [Test]
        public void TestConditional2()
        {
            Expression<Func<TestClassA, string>> path = a => a.B.S;
            Expression<Func<TestClassA, bool>> condition = a => a.S == "zzz";
            Expression assign = Expression.Assign(path.Body, Expression.Constant("qxx"));
            Expression test = new ParameterReplacer(condition.Parameters[0], path.Parameters[0]).Visit(condition.Body);
            Expression<Action<TestClassA>> exp = Expression.Lambda<Action<TestClassA>>(Expression.IfThenElse(test, assign, Expression.Default(typeof(void))), path.Parameters);
            var action = LambdaCompiler.Compile(exp);
            var o = new TestClassA {S = "zzz"};
            action(o);
            Assert.IsNotNull(o.B);
            Assert.AreEqual(o.B.S, "qxx");
        }

        [Test]
        public void TestConditional3()
        {
            Expression<Func<TestClassA, string>> path = a => a.B.S;
            Expression<Func<TestClassA, string>> path2 = a => a.S;
            Expression<Func<TestClassA, bool>> condition = a => a.S == "zzz";
            Expression assign1 = Expression.Assign(path.Body, Expression.Constant("qxx"));
            Expression assign2 = Expression.Assign(path.Body, Expression.Constant("qzz"));
            Expression test = new ParameterReplacer(condition.Parameters[0], path.Parameters[0]).Visit(condition.Body);
            Expression assign3 = Expression.Assign(new ParameterReplacer(path2.Parameters[0], path.Parameters[0]).Visit(path2.Body), Expression.Block(typeof(string), Expression.IfThenElse(test, assign1, assign2), path.Body));
            //Expression<Action<TestClassA>> exp = Expression.Lambda<Action<TestClassA>>(Expression.IfThenElse(test, assign1, assign2), path.Parameters);
            Expression<Action<TestClassA>> exp = Expression.Lambda<Action<TestClassA>>(assign3, path.Parameters);
            var action = LambdaCompiler.Compile(exp);
            var o = new TestClassA {S = "zzz"};
            action(o);
            Assert.IsNotNull(o.B);
            Assert.AreEqual(o.B.S, "qxx");
            Assert.AreEqual(o.S, "qxx");
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

        private void Run(object param)
        {
            var f = (Func<TestClassA, List<string>>)param;
            for(int i = 0; i < 1000000; ++i)
            {
                string s = Guid.NewGuid().ToString();
                var a = new TestClassA {ArrayB = new[] {new TestClassB {S = s}}};
                var list = f(a);
                if(list.Count == 0 || list[0] != s)
                    wasBug = true;
            }
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

        private volatile bool wasBug;

        private static readonly MethodInfo forEachMethod = ((MethodCallExpression)((Expression<Action<int[]>>)(ints => Array.ForEach(ints, null))).Body).Method.GetGenericMethodDefinition();

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
            public TestEnum E { get; set; }
            public int? X;
            public Guid Guid = Guid.Empty;
            public Guid? NullableGuid;
            public bool? NullableBool;
            public int Y;
            public bool Bool;

            ~TestClassA()
            {
                S = null;
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

        private enum TestEnum
        {
            Zero = 0,
            One = 1,
            Two = 2
        }
    }
}