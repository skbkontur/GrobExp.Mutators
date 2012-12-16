using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

using GrEmit;

using GrobExp;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class TestPerformance
    {
        [Test, Ignore]
        public void TestSimple()
        {
            Expression<Func<TestClassA, int?>> exp = o => o.ArrayB[0].C.ArrayD[0].X;
            var a = new TestClassA {ArrayB = new TestClassB[1] {new TestClassB {C = new TestClassC {ArrayD = new TestClassD[1] {new TestClassD {X = 5}}}}}};
            Console.WriteLine("Sharp");
            var ethalon = MeasureSpeed(Func1, a, 1000000000, null);
            Console.WriteLine("GroboCompile without checking");
            MeasureSpeed(LambdaCompiler.Compile(exp, CompilerOptions.None), a, 1000000000, ethalon);
            Console.WriteLine("GroboCompile with checking");
            MeasureSpeed(LambdaCompiler.Compile(exp), a, 1000000000, ethalon);
            Console.WriteLine("Compile");
            MeasureSpeed(exp.Compile(), a, 100000000, ethalon);
        }

        [Test, Ignore]
        public void TestSubLambda1()
        {
            Expression<Func<TestClassA, bool>> exp = o => o.ArrayB.Any(b => b.S == o.S);
            var a = new TestClassA {S = "zzz", ArrayB = new[] {new TestClassB {S = "zzz"},}};
            Console.WriteLine("Sharp");
            var ethalon = MeasureSpeed(Func2, a, 100000000, null);
            Console.WriteLine("GroboCompile without checking");
            MeasureSpeed(LambdaCompiler.Compile(exp, CompilerOptions.None), a, 100000000, ethalon);
            Console.WriteLine("GroboCompile with checking");
            MeasureSpeed(LambdaCompiler.Compile(exp), a, 100000000, ethalon);
            Console.WriteLine("Compile");
            MeasureSpeed(exp.Compile(), a, 1000000, ethalon);
        }

        [Test, Ignore]
        public void TestSubLambda1WithGarbageCollecting()
        {
            Expression<Func<TestClassA, bool>> exp = o => o.ArrayB.Any(b => b.S == o.S);
            var a = new TestClassA {S = "zzz", ArrayB = new[] {new TestClassB {S = "zzz"},}};
            Console.WriteLine("GroboCompile without checking");
            stop = false;
            var thread = new Thread(Collect);
            thread.Start();
            MeasureSpeed(LambdaCompiler.Compile(exp, CompilerOptions.None), a, 100000000, null);
            stop = true;
        }

        [Test, Ignore]
        public void TestSubLambda2()
        {
            Expression<Func<TestClassA, bool>> exp = o => o.ArrayB.Any(b => b.S == o.S && b.C.ArrayD.All(d => d.S == b.S && d.ArrayE.Any(e => e.S == o.S && e.S == b.S && e.S == d.S)));
            var a = new TestClassA
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
                };
            Console.WriteLine("Sharp");
            var ethalon = MeasureSpeed(Func3, a, 100000000, null);
            Console.WriteLine("GroboCompile without checking");
            MeasureSpeed(LambdaCompiler.Compile(exp, CompilerOptions.None), a, 100000000, ethalon);
            Console.WriteLine("GroboCompile with checking");
            MeasureSpeed(LambdaCompiler.Compile(exp), a, 100000000, ethalon);
            Console.WriteLine("Compile");
            MeasureSpeed(exp.Compile(), a, 1000000, ethalon);
        }

        [Test, Ignore]
        public void TestCalls()
        {
            var test = (ITest)new TestImpl();
            Console.WriteLine("Pure call");
            MeasureSpeed(test, 100000000);
            Console.WriteLine(x);
            test = BuildCall();
            Console.WriteLine("Call");
            MeasureSpeed(test, 100000000);
            Console.WriteLine(x);
            test = BuildDelegate();
            Console.WriteLine("Dynamic method through delegate");
            MeasureSpeed(test, 100000000);
            Console.WriteLine(x);
            test = BuildCalli();
            Console.WriteLine("Dynamic method through calli");
            MeasureSpeed(test, 100000000);
            Console.WriteLine(x);
        }

        [Test, Ignore]
        public void TestCalliWithGarbageCollecting()
        {
            stop = false;
            var thread = new Thread(Collect);
            thread.Start();

            var test = BuildCalli();
            Console.WriteLine("Dynamic method through calli");
            MeasureSpeed(test, 1000000000);
            Console.WriteLine(x);

            stop = true;
        }

        public static int x;

        public class TestImpl : ITest
        {
            public void DoNothing()
            {
                DoNothingImpl();
            }

            private void DoNothingImpl()
            {
                ++x;
            }
        }

        public interface ITest
        {
            void DoNothing();
        }

        private void Collect()
        {
            while(!stop)
            {
                Thread.Sleep(100);
                GC.Collect();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int? Func1(TestClassA a)
        {
            return a.ArrayB[0].C.ArrayD[0].X;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool Func2(TestClassA a)
        {
            return a.ArrayB.Any(b => b.S == a.S);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool Func3(TestClassA a)
        {
            return a.ArrayB.Any(b => b.S == a.S && b.C.ArrayD.All(d => d.S == b.S && d.ArrayE.Any(e => e.S == a.S && e.S == b.S && e.S == d.S)));
        }

        private ITest BuildCall()
        {
            var typeBuilder = LambdaCompiler.Module.DefineType(Guid.NewGuid().ToString(), TypeAttributes.Class | TypeAttributes.Public);
            var doNothingMethod = typeBuilder.DefineMethod("DoNothingImpl", MethodAttributes.Public, typeof(void), Type.EmptyTypes);
            var il = new GroboIL(doNothingMethod);
            il.Ldnull();
            il.Ldnull();
            il.Ldfld(xField);
            il.Ldc_I4(1);
            il.Add();
            il.Stfld(xField);
            il.Ret();
            var method = typeBuilder.DefineMethod("DoNothing", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), Type.EmptyTypes);
            il = new GroboIL(method);
            il.Ldarg(0);
            il.Call(doNothingMethod);
            il.Ret();
            typeBuilder.DefineMethodOverride(method, typeof(ITest).GetMethod("DoNothing"));
            typeBuilder.AddInterfaceImplementation(typeof(ITest));
            var type = typeBuilder.CreateType();
            return (ITest)Activator.CreateInstance(type);
        }

        private ITest BuildDelegate()
        {
            var action = Build();
            var typeBuilder = LambdaCompiler.Module.DefineType(Guid.NewGuid().ToString(), TypeAttributes.Class | TypeAttributes.Public);
            var actionField = typeBuilder.DefineField("action", typeof(Action), FieldAttributes.Private | FieldAttributes.InitOnly);
            var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] {typeof(Action)});
            var il = new GroboIL(constructor);
            il.Ldarg(0);
            il.Ldarg(1);
            il.Stfld(actionField);
            il.Ret();
            var method = typeBuilder.DefineMethod("DoNothing", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), Type.EmptyTypes);
            il = new GroboIL(method);
            il.Ldarg(0);
            il.Ldfld(actionField);
            il.Call(typeof(Action).GetMethod("Invoke", Type.EmptyTypes), typeof(Action));
            il.Ret();
            typeBuilder.DefineMethodOverride(method, typeof(ITest).GetMethod("DoNothing"));
            typeBuilder.AddInterfaceImplementation(typeof(ITest));
            var type = typeBuilder.CreateType();
            return (ITest)Activator.CreateInstance(type, new object[] {action.Item1});
        }

        private ITest BuildCalli()
        {
            var action = Build();
            var typeBuilder = LambdaCompiler.Module.DefineType(Guid.NewGuid().ToString(), TypeAttributes.Class | TypeAttributes.Public);
            var pointerField = typeBuilder.DefineField("pointer", typeof(IntPtr), FieldAttributes.Private | FieldAttributes.InitOnly);
            var delegateField = typeBuilder.DefineField("delegate", typeof(Delegate), FieldAttributes.Private | FieldAttributes.InitOnly);
            var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] {typeof(IntPtr), typeof(Delegate)});
            var il = new GroboIL(constructor);
            il.Ldarg(0);
            il.Ldarg(1);
            il.Stfld(pointerField);
            il.Ldarg(0);
            il.Ldarg(2);
            il.Stfld(delegateField);
            il.Ret();
            var method = typeBuilder.DefineMethod("DoNothing", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), Type.EmptyTypes);
            il = new GroboIL(method);
            il.Ldarg(0);
            il.Ldfld(pointerField);
            il.Calli(CallingConventions.Standard, typeof(void), Type.EmptyTypes);
            il.Ret();
            typeBuilder.DefineMethodOverride(method, typeof(ITest).GetMethod("DoNothing"));
            typeBuilder.AddInterfaceImplementation(typeof(ITest));
            var type = typeBuilder.CreateType();
            return (ITest)Activator.CreateInstance(type, new object[] {dynamicMethodPointerExtractor((DynamicMethod)action.Item2), action.Item1});
        }

        private Tuple<Action, MethodInfo> Build()
        {
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), Type.EmptyTypes, LambdaCompiler.Module, true);
            var il = new GroboIL(dynamicMethod);
            il.Ldnull();
            il.Ldnull();
            il.Ldfld(xField);
            il.Ldc_I4(1);
            il.Add();
            il.Stfld(xField);
            il.Ret();
            return new Tuple<Action, MethodInfo>((Action)dynamicMethod.CreateDelegate(typeof(Action)), dynamicMethod);
        }

        private static Func<DynamicMethod, IntPtr> EmitDynamicMethodPointerExtractor()
        {
            var method = new DynamicMethod("DynamicMethodPointerExtractor", typeof(IntPtr), new[] {typeof(DynamicMethod)}, LambdaCompiler.Module, true);
            var il = new GroboIL(method);
            il.Ldarg(0); // stack: [dynamicMethod]
            MethodInfo getMethodDescriptorMethod = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.Instance | BindingFlags.NonPublic);
            if(getMethodDescriptorMethod == null)
                throw new MissingMethodException(typeof(DynamicMethod).Name, "GetMethodDescriptor");
            il.Call(getMethodDescriptorMethod); // stack: [dynamicMethod.GetMethodDescriptor()]
            var runtimeMethodHandle = il.DeclareLocal(typeof(RuntimeMethodHandle));
            il.Stloc(runtimeMethodHandle);
            il.Ldloc(runtimeMethodHandle);
            MethodInfo prepareMethodMethod = typeof(RuntimeHelpers).GetMethod("PrepareMethod", new[] {typeof(RuntimeMethodHandle)});
            if(prepareMethodMethod == null)
                throw new MissingMethodException(typeof(RuntimeHelpers).Name, "PrepareMethod");
            il.Call(prepareMethodMethod);
            MethodInfo getFunctionPointerMethod = typeof(RuntimeMethodHandle).GetMethod("GetFunctionPointer", BindingFlags.Instance | BindingFlags.Public);
            if(getFunctionPointerMethod == null)
                throw new MissingMethodException(typeof(RuntimeMethodHandle).Name, "GetFunctionPointer");
            il.Ldloca(runtimeMethodHandle);
            il.Call(getFunctionPointerMethod); // stack: [dynamicMethod.GetMethodDescriptor().GetFunctionPointer()]
            il.Ret();
            return (Func<DynamicMethod, IntPtr>)method.CreateDelegate(typeof(Func<DynamicMethod, IntPtr>));
        }

        private void MeasureSpeed(ITest test, int iter)
        {
            var stopwatch = Stopwatch.StartNew();
            for(int i = 0; i < iter; ++i)
                test.DoNothing();
            var elapsed = stopwatch.Elapsed;
            Console.WriteLine(string.Format("{0:0.000} millions runs per second", iter * 1.0 / elapsed.TotalSeconds / 1000000.0));
        }

        private double MeasureSpeed<T1, T2>(Func<T1, T2> func, T1 arg, int iter, double? ethalon)
        {
            var stopwatch = Stopwatch.StartNew();
            for(int i = 0; i < iter; ++i)
                func(arg);
            var elapsed = stopwatch.Elapsed;
            Console.WriteLine(string.Format("{0:0.000} millions runs per second = {1:0.000X}", iter * 1.0 / elapsed.TotalSeconds / 1000000.0, ethalon == null ? 1 : elapsed.TotalSeconds / iter / ethalon));
            return elapsed.TotalSeconds / iter;
        }

        private volatile bool stop;

        private static readonly FieldInfo xField = (FieldInfo)((MemberExpression)((Expression<Func<int>>)(() => x)).Body).Member;
        private static readonly Func<DynamicMethod, IntPtr> dynamicMethodPointerExtractor = EmitDynamicMethodPointerExtractor();

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

            public string S;
        }

        private class TestClassE
        {
            public string S { get; set; }
            public int X { get; set; }
        }
    }
}