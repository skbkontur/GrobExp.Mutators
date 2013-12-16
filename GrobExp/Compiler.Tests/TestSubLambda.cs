using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

using GrobExp.Compiler;

using Microsoft.CSharp.RuntimeBinder;

using NUnit.Framework;

using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace Compiler.Tests
{
    public class TestSubLambda : TestBase
    {
        [Test]
        public void TestSubLambda1()
        {
            Expression<Func<TestClassA, bool>> exp = a => a.ArrayB.Any(b => b.S == a.S);
            var f = Compile(exp, CompilerOptions.All);
            Assert.IsTrue(f(new TestClassA {S = "zzz", ArrayB = new[] {new TestClassB {S = "zzz"},}}));
            Assert.IsFalse(f(new TestClassA {S = "zzz", ArrayB = new[] {new TestClassB(),}}));
        }

        [Test]
        public void TestSubLambda1x()
        {
            Expression<Func<TestClassA, bool>> exp = a => a.ArrayB.Any(b => b.S == "zzz");
            var f = Compile(exp, CompilerOptions.All);
            Assert.IsTrue(f(new TestClassA {S = "zzz", ArrayB = new[] {new TestClassB {S = "zzz"},}}));
            Assert.IsFalse(f(new TestClassA {S = "zzz", ArrayB = new[] {new TestClassB(),}}));
        }

        [Test]
        public void TestSubLambda2()
        {
            Expression<Func<TestClassA, IEnumerable<TestClassB>>> exp = a => a.ArrayB.Where(b => b.S == a.S);
            Expression where = exp.Body;
            ParameterExpression temp = Expression.Variable(typeof(IEnumerable<TestClassB>));
            Expression assignTemp = Expression.Assign(temp, where);
            Expression assignS = Expression.Assign(Expression.MakeMemberAccess(exp.Parameters[0], typeof(TestClassA).GetProperty("S", BindingFlags.Public | BindingFlags.Instance)), Expression.Constant("zzz"));
            Expression any = Expression.Call(anyMethod.MakeGenericMethod(typeof(TestClassB)), temp);
            var exp2 = Expression.Lambda<Func<TestClassA, bool>>(Expression.Block(typeof(bool), new[] {temp}, assignTemp, assignS, any), exp.Parameters);

            var f = Compile(exp2, CompilerOptions.All);
            Assert.IsTrue(f(new TestClassA {S = "qzz", ArrayB = new[] {new TestClassB {S = "zzz"},}}));
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
//            var parameter = Expression.Parameter(typeof(object));
//            var exp = Expression.Lambda<Func<object, int?>>(Expression.Unbox(parameter, typeof(int?)), parameter);
//            Expression<Func<TestClassA, int>> exp = o => func(o.Y, o.Z);
//            Expression<Func<int, int, int>> lambda = (x, y) => x + y;
//            ParameterExpression parameter = Expression.Parameter(typeof(TestClassA));
//            Expression<Func<TestClassA, int>> exp = Expression.Lambda<Func<TestClassA, int>>(Expression.Invoke(lambda, Expression.MakeMemberAccess(parameter, typeof(TestClassA).GetField("Y")), Expression.MakeMemberAccess(parameter, typeof(TestClassA).GetField("Z"))), parameter);

//            var x = Expression.Parameter(typeof(object), "x");
//            var y = Expression.Parameter(typeof(object), "y");
//            var binder = Binder.BinaryOperation(
//                CSharpBinderFlags.None, ExpressionType.Add, typeof(TestDynamic),
//                new[]
//                    {
//                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
//                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
//                    });
//            var exp = Expression.Lambda<Func<object, object, object>>(
//                Expression.Dynamic(binder, typeof(object), x, y),
//                new[] {x, y}
//                );

            //Expression<Func<TestClassA, bool>> exp = a => a.ArrayB.Any(b => b.S == a.S);
            Expression<Func<TestClassA, bool?>> exp = o => o.A.X > 0 && o.B.Y > 0;

//            int? guid = 5;
//            ParameterExpression parameter = Expression.Parameter(typeof(int?));
//            Expression<Func<int?, bool>> exp = Expression.Lambda<Func<int?, bool>>(
//                Expression.Block(
//                    Expression.Call(threadSleepMethod, new[] { Expression.Constant(10) }),
//                    Expression.Equal(parameter, Expression.Constant(guid, typeof(int?)))
//                    ),
//                parameter);

            CompileAndSave(exp);
        }

        private static readonly MethodInfo threadSleepMethod = ((MethodCallExpression)((Expression<Action>)(() => Thread.Sleep(0))).Body).Method;

        [Test, Ignore]
        public void TestDebugInfo()
        {
            var asm = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("foo"), AssemblyBuilderAccess.RunAndSave);

            var mod = asm.DefineDynamicModule("mymod", "tmp.dll", true);
            var type = mod.DefineType("baz", TypeAttributes.Public | TypeAttributes.Class);
            var meth = type.DefineMethod("go", MethodAttributes.Public | MethodAttributes.Static, typeof(int), Type.EmptyTypes);

            var sdi = Expression.SymbolDocument("TestDebug2.txt", Guid.Empty, Guid.Empty, Guid.Empty);

            var di = Expression.DebugInfo(sdi, 2, 2, 2, 13);

            var exp = Expression.Divide(Expression.Constant(2), Expression.Subtract(Expression.Constant(4), Expression.Constant(4)));
            var block = Expression.Block(di, exp);

            var gen = DebugInfoGenerator.CreatePdbGenerator();

            LambdaExpression lambda = Expression.Lambda(block, new ParameterExpression[0]);
            LambdaCompiler.CompileToMethod(lambda, meth, gen, CompilerOptions.All);
            //lambda.CompileToMethod(meth, gen);

            var newtype = type.CreateType();
            asm.Save("tmp.dll");
            newtype.GetMethod("go").Invoke(null, new object[0]);
            //meth.Invoke(null, new object[0]);
            //lambda.DynamicInvoke(new object[0]);
            Console.WriteLine(" ");
        }

        [Test, Ignore]
        public void TestDebugInfo2()
        {
            var asm = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("foo"), AssemblyBuilderAccess.RunAndSave);

            var mod = asm.DefineDynamicModule("mymod", "tmp.dll", true);
            var type = mod.DefineType("baz", TypeAttributes.Public | TypeAttributes.Class);
            var meth = type.DefineMethod("go", MethodAttributes.Public | MethodAttributes.Static);

            var nestedType = type.DefineNestedType("qwerty", TypeAttributes.NestedPublic | TypeAttributes.Class);
            nestedType.DefineField("zzz", typeof(Guid), FieldAttributes.Static | FieldAttributes.Public);
            nestedType.CreateType();

            var document = mod.DefineDocument("TestDebug2.txt", Guid.Empty, Guid.Empty, Guid.Empty);//Expression.SymbolDocument("TestDebug2.txt");

            //var di = Expression.DebugInfo(sdi, 2, 2, 2, 13);

            //var exp = Expression.Divide(Expression.Constant(2), Expression.Subtract(Expression.Constant(4), Expression.Constant(4)));
            //var block = Expression.Block(di, exp);

            var il = meth.GetILGenerator();
            il.MarkSequencePoint(document, 2, 2, 2, 13);
            il.Emit(OpCodes.Ldc_I4_2);
            il.Emit(OpCodes.Ldc_I4_4);
            il.Emit(OpCodes.Ldc_I4_4);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Div);

            var newtype = type.CreateType();

            asm.Save("tmp.dll");
            newtype.GetMethod("go").Invoke(null, new object[0]);
            //meth.Invoke(null, new object[0]);
            //lambda.DynamicInvoke(new object[0]);
            Console.WriteLine(" ");
        }

        [Test, Ignore]
        public void TestDebug2()
        {
  // create a dynamic assembly and module 
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("foo"), AssemblyBuilderAccess.RunAndSave);

        ModuleBuilder module = assemblyBuilder.DefineDynamicModule("zzz", "HelloWorld.dll", true); // <-- pass 'true' to track debug info.

        // Tell Emit about the source file that we want to associate this with. 
        ISymbolDocumentWriter doc = module.DefineDocument("Source.txt", Guid.Empty, Guid.Empty, Guid.Empty);

        // create a new type to hold our Main method 
        TypeBuilder typeBuilder = module.DefineType("HelloWorldType", TypeAttributes.Public | TypeAttributes.Class);

        // create the Main(string[] args) method 
        MethodBuilder methodbuilder = typeBuilder.DefineMethod("Main", MethodAttributes.Static | MethodAttributes.Public, typeof(int), new Type[] { typeof(string[]) });

        // generate the IL for the Main method 
        ILGenerator ilGenerator = methodbuilder.GetILGenerator();

        // Create a local variable of type 'string', and call it 'xyz'
        LocalBuilder localXYZ = ilGenerator.DeclareLocal(typeof(string));
        localXYZ.SetLocalSymInfo("xyz"); // Provide name for the debugger. 

        // Emit sequence point before the IL instructions. This is start line, start col, end line, end column, 

        // Line 2: xyz = "hello"; 
        ilGenerator.MarkSequencePoint(doc, 2, 1, 2, 100);
        ilGenerator.Emit(OpCodes.Ldstr, "Hello world!");
        ilGenerator.Emit(OpCodes.Stloc, localXYZ);

        // Line 3: Write(xyz); 
        MethodInfo infoWriteLine = typeof(System.Console).GetMethod("WriteLine", new Type[] { typeof(string) });
        ilGenerator.MarkSequencePoint(doc, 3, 1, 3, 100);
        ilGenerator.Emit(OpCodes.Ldloc, localXYZ);
        ilGenerator.EmitCall(OpCodes.Call, infoWriteLine, null);

        LocalBuilder localResult = ilGenerator.DeclareLocal(typeof(string));
        localResult.SetLocalSymInfo("result"); // Provide name for the debugger. 

        // Line 4: result = 0/0; 
        ilGenerator.MarkSequencePoint(doc, 4, 1, 4, 100);
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Div);
        ilGenerator.Emit(OpCodes.Stloc, localResult);

        // Line 5: return result; 
        ilGenerator.MarkSequencePoint(doc, 5, 1, 5, 100);
        ilGenerator.Emit(OpCodes.Ldloc, localResult);
        ilGenerator.Emit(OpCodes.Ret);

        // bake it 
        Type helloWorldType = typeBuilder.CreateType();

        assemblyBuilder.Save("HelloWorld.dll");

        // This now calls the newly generated method. We can step into this and debug our emitted code!! 
        helloWorldType.GetMethod("Main").Invoke(null, new string[] { null }); // <-- step into        
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
            var exp2 = Expression.Lambda<Func<TestStructA, bool>>(Expression.Block(typeof(bool), new[] {temp}, assignTemp, assignS, any), exp.Parameters);

            var f = Compile(exp2, CompilerOptions.All);
            Assert.IsTrue(f(new TestStructA {S = "qzz", ArrayB = new[] {new TestStructB {S = "zzz"},}}));
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
            var f = Compile(exp, CompilerOptions.All);
            aaa.Y = 1;
            Assert.IsFalse(f(new TestStructA {ArrayB = new[] {new TestStructB {Y = 1},}}));
        }

        [Test]
        public void TestSubLambda3()
        {
            Expression<Func<TestClassA, int>> exp = data => data.ArrayB.SelectMany(b => b.C.ArrayD, (classB, classD) => classD.ArrayE.FirstOrDefault(c => c.S == "zzz").X).Where(i => i > 0).FirstOrDefault();
            var f = Compile(exp, CompilerOptions.All);
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
            var exp2 = Expression.Lambda<Func<TestClassA, bool>>(Expression.Block(typeof(bool), new[] {temp}, assignTemp, assignS, any), exp.Parameters);

            var f = Compile(exp2, CompilerOptions.All);
            Assert.IsTrue(f(new TestClassA {ArrayB = new[] {new TestClassB {S = "zzz"},}}));
        }

        [Test]
        public void TestSubLambda5()
        {
            Expression<Func<TestClassA, bool>> exp = a => a.ArrayB.Any(b => b.S == a.S && b.C.ArrayD.All(d => d.S == b.S && d.ArrayE.Any(e => e.S == a.S && e.S == b.S && e.S == d.S)));
            var f = Compile(exp, CompilerOptions.All);
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
                                                    new TestClassD {S = "zzz", ArrayE = new[] {new TestClassE {S = "zzz"},}},
                                                    new TestClassD {S = "zzz", ArrayE = new[] {new TestClassE {S = "zzz"},}}
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
                                                    new TestClassD {S = "qxx", ArrayE = new[] {new TestClassE {S = "zzz"},}},
                                                    new TestClassD {S = "zzz", ArrayE = new[] {new TestClassE {S = "zzz"},}}
                                                }
                                        }
                                },
                        }
                }));
        }

        private void CompileAndSave<TDelegate>(Expression<TDelegate> lambda) where TDelegate : class
        {
            var da = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("dyn"), // call it whatever you want
                AssemblyBuilderAccess.Save);

            var dm = da.DefineDynamicModule("dyn_mod", "dyn.dll");
            var dt = dm.DefineType("dyn_type");
            var method = dt.DefineMethod("Foo", MethodAttributes.Public | MethodAttributes.Static, lambda.ReturnType, lambda.Parameters.Select(parameter => parameter.Type).ToArray());

            //lambda.CompileToMethod(method);
            LambdaCompiler.CompileToMethod(lambda, method, CompilerOptions.All);
            dt.CreateType();

            da.Save("dyn.dll");
        }

        private static readonly MethodInfo anyMethod = ((MethodCallExpression)((Expression<Func<IEnumerable<int>, bool>>)(ints => ints.Any())).Body).Method.GetGenericMethodDefinition();

        private static readonly MethodInfo anyWithPredicateMethod = ((MethodCallExpression)((Expression<Func<IEnumerable<int>, bool>>)(ints => ints.Any(i => i == 0))).Body).Method.GetGenericMethodDefinition();
        private static Func<int, int, int> func = (x, y) => x + y;

        public class TestClassA
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
            public int Z;
            public bool Bool;
        }

        public class TestClassB
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

        public class TestClassC
        {
            public string S { get; set; }

            public TestClassD D { get; set; }

            public TestClassD[] ArrayD { get; set; }
        }

        public class TestClassD
        {
            public TestClassE E { get; set; }
            public TestClassE[] ArrayE { get; set; }
            public string Z { get; set; }

            public int? X { get; set; }

            public string S;
        }

        public class TestClassE
        {
            public string S { get; set; }
            public int X { get; set; }
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
    }
}