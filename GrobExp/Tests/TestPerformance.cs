using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

using GrobExp;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class TestPerformance
    {
        [Test, Ignore]
        public void Test1()
        {
            Expression<Func<TestClassA, int?>> exp = o => o.ArrayB[0].C.ArrayD[0].X;
            var a = new TestClassA { ArrayB = new TestClassB[1] { new TestClassB { C = new TestClassC { ArrayD = new TestClassD[1] { new TestClassD { X = 5 } } } } } };
            Console.WriteLine("Compile");
            MeasureSpeed(exp.Compile(), a, 10000000);
            Console.WriteLine("GroboCompile without checking");
            MeasureSpeed(LambdaCompiler.Compile(exp, CompilerOptions.None), a, 10000000);
            Console.WriteLine("GroboCompile with checking");
            MeasureSpeed(LambdaCompiler.Compile(exp), a, 10000000);
        }

        [Test, Ignore]
        public void Test2()
        {
            Expression<Func<TestClassA, bool>> exp = o => o.ArrayB.Any(b => b.S == o.S);
            var a = new TestClassA { S = "zzz", ArrayB = new[] { new TestClassB { S = "zzz" }, } };
            Console.WriteLine("GroboCompile without checking");
            MeasureSpeed(LambdaCompiler.Compile(exp, CompilerOptions.None), a, 10000000);
            Console.WriteLine("GroboCompile with checking");
            MeasureSpeed(LambdaCompiler.Compile(exp), a, 10000000);
            Console.WriteLine("Compile");
            MeasureSpeed(exp.Compile(), a, 1000000);
        }

        private void MeasureSpeed<T1, T2>(Func<T1, T2> func, T1 arg, int iter)
        {
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < iter; ++i)
            {
                func(arg);
            }
            var elapsed = stopwatch.Elapsed;
            Console.WriteLine(string.Format("{0:0.000} millions runs per second", iter * 1.0 / elapsed.TotalSeconds / 1000000.0));
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