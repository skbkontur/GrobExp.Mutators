using System;
using System.Linq.Expressions;

using GrobExp.Mutators;

using NUnit.Framework;

namespace Mutators.Tests
{
    [Parallelizable(ParallelScope.All)]
    public class ExpressionCompilerTest : TestBase
    {
        [Test]
        public void TestCompileSimple()
        {
            var f = ExpressionCompiler.Compile<Z, int>(zz => zz.Q[0].Zzz);
            Assert.AreEqual(23, f(new Z
                {
                    Q = new[]
                        {
                            new Q
                                {
                                    Zzz = 23
                                }
                        }
                }));
        }

        [Test /*, Timeout(20000)*/]
        public void TestCompilePerformance()
        {
            for (int i = 0; i < 100000; i++)
                ExpressionCompiler.Compile<Z, string>(z => z.Q[22].Qzz);
        }

        [Test /*, Timeout(20000)*/]
        public void TestCompilePerformance2()
        {
            Func<string, Expression<Func<Z, bool>>> f = qzz => z => z.Q[0].Qzz == qzz;
            for (int i = 0; i < 100000; i++)
            {
                var g = ExpressionCompiler.Compile(f(i.ToString()));
                Assert.That(g(new Z {Q = new[] {new Q {Qzz = i.ToString()}}}));
            }
        }

        public class Z
        {
            public Q[] Q { get; set; }
        }

        public class Q
        {
            public int Zzz { get; set; }
            public string Qzz { get; set; }
        }
    }
}