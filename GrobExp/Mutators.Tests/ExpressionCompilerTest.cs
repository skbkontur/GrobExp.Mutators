using System;
using System.Linq.Expressions;

using GrobExp.Mutators;

using NUnit.Framework;

namespace Mutators.Tests
{
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

        [Test]
        public void TestCompilerCreatesSingleConstantsClosure()
        {
            ExpressionTypeBuilder.TypeCache.Clear();
            var f1 = ExpressionCompiler.Compile<string, int>(s => (s + "abc").Length + 2);
            Assert.That(f1("abc"), Is.EqualTo(8));
            var f2 = ExpressionCompiler.Compile<string, int>(s => 3 + (s + "de").Length);
            Assert.That(f2("d"), Is.EqualTo(6));
            Assert.That(ExpressionTypeBuilder.TypeCache.Count, Is.EqualTo(1));
        }

        [Test, Timeout(20000)]
        public void TestCompilePerformance()
        {
            ExpressionTypeBuilder.TypeCache.Clear();
            for(int i = 0; i < 100000; i++)
                ExpressionCompiler.Compile<Z, string>(z => z.Q[22].Qzz);
            Assert.AreEqual(1, ExpressionTypeBuilder.TypeCache.Count);
        }

        [Test/*, Timeout(20000)*/]
        public void TestCompilePerformance2()
        {
            ExpressionTypeBuilder.TypeCache.Clear();
            Func<string, Expression<Func<Z, bool>>> f = qzz => z => z.Q[0].Qzz == qzz;
            for(int i = 0; i < 100000; i++)
            {
                var g = ExpressionCompiler.Compile(f(i.ToString()));
                Assert.That(g(new Z {Q = new[] {new Q {Qzz = i.ToString()}}}));
            }
            Assert.AreEqual(1, ExpressionTypeBuilder.TypeCache.Count);
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