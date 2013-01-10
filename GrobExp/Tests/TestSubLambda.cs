using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using GrobExp;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class TestSubLambda
    {
        [Test]
        public void TestSubLambda1()
        {
            Expression<Func<TestClassA, bool>> exp = a => a.ArrayB.Any(b => b.S == a.S);
            var f = LambdaCompiler.Compile(exp);
            Assert.IsTrue(f(new TestClassA { S = "zzz", ArrayB = new[] { new TestClassB { S = "zzz" }, } }));
            Assert.IsFalse(f(new TestClassA { S = "zzz", ArrayB = new[] { new TestClassB(), } }));
        }

        private static readonly MethodInfo anyMethod = ((MethodCallExpression)((Expression<Func<IEnumerable<int>, bool>>)(ints => ints.Any())).Body).Method.GetGenericMethodDefinition();

        private static readonly MethodInfo anyWithPredicateMethod = ((MethodCallExpression)((Expression<Func<IEnumerable<int>, bool>>)(ints => ints.Any(i => i == 0))).Body).Method.GetGenericMethodDefinition();

        [Test]
        public void TestSubLambda2()
        {
            Expression<Func<TestClassA, IEnumerable<TestClassB>>> exp = a => a.ArrayB.Where(b => b.S == a.S);
            Expression where = exp.Body;
            ParameterExpression temp = Expression.Variable(typeof(IEnumerable<TestClassB>));
            Expression assignTemp = Expression.Assign(temp, where);
            Expression assignS = Expression.Assign(Expression.MakeMemberAccess(exp.Parameters[0], typeof(TestClassA).GetProperty("S", BindingFlags.Public | BindingFlags.Instance)), Expression.Constant("zzz"));
            Expression any = Expression.Call(anyMethod.MakeGenericMethod(typeof(TestClassB)), temp);
            var exp2 = Expression.Lambda<Func<TestClassA, bool>>(Expression.Block(typeof(bool), new[] { temp }, assignTemp, assignS, any), exp.Parameters);

            var f = LambdaCompiler.Compile(exp2);
            Assert.IsTrue(f(new TestClassA { S = "qzz", ArrayB = new[] { new TestClassB { S = "zzz" }, } }));
        }


        private void CompileAndSave(LambdaExpression lambda)
        {
            var da = AppDomain.CurrentDomain.DefineDynamicAssembly(
            new AssemblyName("dyn"), // call it whatever you want
            AssemblyBuilderAccess.Save);

            var dm = da.DefineDynamicModule("dyn_mod", "dyn.dll");
            var dt = dm.DefineType("dyn_type");
            var method = dt.DefineMethod("Foo", MethodAttributes.Public | MethodAttributes.Static);

            lambda.CompileToMethod(method);
            dt.CreateType();

            da.Save("dyn.dll");
        }

        [Test, Ignore]
        public void CompileAndSave()
        {
            /*Expression<Func<TestStructA, IEnumerable<TestStructB>>> exp = a => a.ArrayB.Where(b => b.S == a.S);
            Expression where = exp.Body;
            ParameterExpression temp = Expression.Variable(typeof(IEnumerable<TestStructB>));
            Expression assignTemp = Expression.Assign(temp, where);
            Expression assignS = Expression.Assign(Expression.MakeMemberAccess(exp.Parameters[0], typeof(TestStructA).GetProperty("S", BindingFlags.Public | BindingFlags.Instance)), Expression.Constant("zzz"));
            Expression any = Expression.Call(anyMethod.MakeGenericMethod(typeof(TestStructB)), temp);
            var exp2 = Expression.Lambda<Func<TestStructA, bool>>(Expression.Block(typeof(bool), new[] { temp }, assignTemp, assignS, any), exp.Parameters);*/
            //Expression<Func<TestClassA, int?>> exp = o => o.ArrayB[0].C.ArrayD[0].X;
//            ParameterExpression a = Expression.Parameter(typeof(double?));
//            ParameterExpression b = Expression.Parameter(typeof(double?));
//            var exp = Expression.Lambda<Func<double?, double?, double?>>(Expression.Power(a, b), a, b);
//            ParameterExpression parameter = Expression.Parameter(typeof(int?));
//            var exp = Expression.Lambda<Func<int?, int?>>(Expression.Increment(parameter), parameter);
            var parameter = Expression.Parameter(typeof(object));
            var exp = Expression.Lambda<Func<object, int?>>(Expression.Unbox(parameter, typeof(int?)), parameter);
            CompileAndSave(exp);
        }

        [Test, Ignore]
        public void TestDebugInfo()
        {
            var asm = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("foo"), System.Reflection.Emit.AssemblyBuilderAccess.RunAndSave);

            var mod = asm.DefineDynamicModule("mymod", "tmp.dll", true);
            var type = mod.DefineType("baz", TypeAttributes.Public);
            var meth = type.DefineMethod("go", MethodAttributes.Public | MethodAttributes.Static);

            var sdi = Expression.SymbolDocument("TestDebug.txt");

            var di = Expression.DebugInfo(sdi, 2, 2, 2, 13);


            var exp = Expression.Divide(Expression.Constant(2), Expression.Subtract(Expression.Constant(4), Expression.Constant(4)));
            var block = Expression.Block(di, exp);

            var gen = DebugInfoGenerator.CreatePdbGenerator();

            LambdaExpression lambda = Expression.Lambda(block, new ParameterExpression[0]);
            lambda.CompileToMethod(meth, gen);

            var newtype = type.CreateType();
            asm.Save("tmp.dll");
            newtype.GetMethod("go").Invoke(null, new object[0]);
            //meth.Invoke(null, new object[0]);
            //lambda.DynamicInvoke(new object[0]);
            Console.WriteLine(" ");
        }

        [Test]
        public void TestSubLambda2x()
        {
            Expression<Func<TestStructA, IEnumerable<TestStructB>>> exp = a => a.ArrayB.Where(b => b.S == a.S);
            Expression where = exp.Body;
            ParameterExpression temp = Expression.Variable(typeof(IEnumerable<TestStructB>));
            Expression assignTemp = Expression.Assign(temp, where);
            Expression assignS = Expression.Assign(Expression.MakeMemberAccess(exp.Parameters[0], typeof(TestStructA).GetProperty("S", BindingFlags.Public | BindingFlags.Instance)), Expression.Constant("zzz"));
            Expression any = Expression.Call(anyMethod.MakeGenericMethod(typeof(TestStructB)), temp);
            var exp2 = Expression.Lambda<Func<TestStructA, bool>>(Expression.Block(typeof(bool), new[] { temp }, assignTemp, assignS, any), exp.Parameters);

            var f = LambdaCompiler.Compile(exp2);
            Assert.IsTrue(f(new TestStructA { S = "qzz", ArrayB = new[] { new TestStructB { S = "zzz" }, } }));
        }

        [Test]
        public void TestSubLambda2y()
        {
            ParameterExpression parameterB = Expression.Parameter(typeof(TestStructB));
            TestStructA aaa = default(TestStructA);
            Expression<Func<TestStructB, bool>> predicate = Expression.Lambda<Func<TestStructB, bool>>(Expression.Equal(Expression.MakeMemberAccess(parameterB, typeof(TestStructB).GetProperty("Y")), Expression.MakeMemberAccess(Expression.Constant(aaa), typeof(TestStructA).GetProperty("Y"))), parameterB);

            ParameterExpression parameterA = Expression.Parameter(typeof(TestStructA));
            Expression any = Expression.Call(anyWithPredicateMethod.MakeGenericMethod(typeof(TestStructB)), Expression.MakeMemberAccess(parameterA, typeof(TestStructA).GetProperty("ArrayB")), predicate);
            Expression<Func<TestStructA, bool>> exp = Expression.Lambda<Func<TestStructA, bool>>(any, parameterA);
            var f = LambdaCompiler.Compile(exp);
            aaa.Y = 1;
            Assert.IsFalse(f(new TestStructA { ArrayB = new[] { new TestStructB { Y = 1 }, } }));
        }

        [Test]
        public void TestSubLambda3()
        {
            Expression<Func<TestClassA, int>> exp = data => data.ArrayB.SelectMany(b => b.C.ArrayD, (classB, classD) => classD.ArrayE.FirstOrDefault(c => c.S == "zzz").X).Where(i => i > 0).FirstOrDefault();
            var f = LambdaCompiler.Compile(exp);
            var a = new TestClassA
            {
                ArrayB = new[]
                        {
                            new TestClassB
                                {
                                    C = new TestClassC
                                        {
                                            ArrayD = new[]
                                                {
                                                    new TestClassD
                                                        {
                                                            ArrayE = new[]
                                                                {
                                                                    new TestClassE {S = "qxx", X = -1},
                                                                    new TestClassE {S = "zzz", X = -1},
                                                                }
                                                        },
                                                }
                                        }
                                },
                            new TestClassB
                                {
                                    C = new TestClassC
                                        {
                                            ArrayD = new[]
                                                {
                                                    new TestClassD
                                                        {
                                                            ArrayE = new[]
                                                                {
                                                                    new TestClassE {S = "qxx", X = -1},
                                                                    new TestClassE {S = "zzz", X = 1},
                                                                }
                                                        },
                                                }
                                        }
                                },
                        }
            };
            Assert.AreEqual(1, f(a));
            Assert.AreEqual(0, f(null));
        }

        [Test]
        public void TestSubLambda4()
        {
            Expression<Func<TestClassA, IEnumerable<TestClassB>>> exp = a => a.ArrayB.Where(b => b.S == a.B.C.S);
            Expression where = exp.Body;
            ParameterExpression temp = Expression.Variable(typeof(IEnumerable<TestClassB>));
            Expression assignTemp = Expression.Assign(temp, where);
            Expression<Func<TestClassA, string>> path = a => a.B.C.S;
            Expression left = new ParameterReplacer(path.Parameters[0], exp.Parameters[0]).Visit(path.Body);
            Expression assignS = Expression.Assign(left, Expression.Constant("zzz"));
            Expression any = Expression.Call(anyMethod.MakeGenericMethod(typeof(TestClassB)), temp);
            var exp2 = Expression.Lambda<Func<TestClassA, bool>>(Expression.Block(typeof(bool), new[] { temp }, assignTemp, assignS, any), exp.Parameters);

            var f = LambdaCompiler.Compile(exp2);
            Assert.IsTrue(f(new TestClassA { ArrayB = new[] { new TestClassB { S = "zzz" }, } }));
        }

        [Test]
        public void TestSubLambda5()
        {
            Expression<Func<TestClassA, bool>> exp = a => a.ArrayB.Any(b => b.S == a.S && b.C.ArrayD.All(d => d.S == b.S && d.ArrayE.Any(e => e.S == a.S && e.S == b.S && e.S == d.S)));
            var f = LambdaCompiler.Compile(exp);
            Assert.IsTrue(f(new TestClassA
                {
                    S = "zzz",
                    ArrayB = new[]
                        {
                            new TestClassB
                                {
                                    S = "zzz",
                                    C = new TestClassC
                                        {
                                            ArrayD = new[]
                                                {
                                                    new TestClassD { S = "zzz", ArrayE = new[] { new TestClassE { S = "zzz" }, } },
                                                    new TestClassD { S = "zzz", ArrayE = new[] { new TestClassE { S = "zzz" }, } }
                                                }
                                        }
                                },
                        }
                }));
            Assert.IsFalse(f(new TestClassA
                {
                    S = "zzz",
                    ArrayB = new[]
                        {
                            new TestClassB
                                {
                                    S = "zzz",
                                    C = new TestClassC
                                        {
                                            ArrayD = new[]
                                                {
                                                    new TestClassD { S = "qxx", ArrayE = new[] { new TestClassE { S = "zzz" }, } },
                                                    new TestClassD { S = "zzz", ArrayE = new[] { new TestClassE { S = "zzz" }, } }
                                                }
                                        }
                                },
                        }
                }));
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