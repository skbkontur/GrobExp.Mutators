using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators;
using GrobExp.Mutators.Visitors;

using NUnit.Framework;

namespace Mutators.Tests.ExpressionExtensionsTests
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class SmashToSmithereensTest
    {
        [Test]
        public void TestOnlyParameter()
        {
            TestSmash(a => a, Path(a => a));
        }

        [Test]
        public void TestMemberAccess()
        {
            TestSmash(a => a.C,
                      Path(a => a),
                      Path(a => a.C)
                );
        }

        [Test]
        public void TestArrayIndex()
        {
            TestSmash(a => a.Bs[2],
                      Path(a => a),
                      Path(a => a.Bs),
                      Path(a => a.Bs[2])
                );
        }

        [Test]
        public void TestArrayLength()
        {
            TestSmash(a => a.Bs.Length,
                      Path(a => a),
                      Path(a => a.Bs),
                      Path(a => a.Bs.Length)
                );
        }

        [Test]
        public void TestConvert()
        {
            TestSmash(a => (long)a.C.Int,
                      Path(a => a),
                      Path(a => a.C),
                      Path(a => a.C.Int),
                      Path(a => (long)a.C.Int)
                );
        }

        [Test]
        public void TestCoalesce()
        {
            TestSmash(a => a.Bs[0].String ?? "Grobas",
                      Path(a => a),
                      Path(a => a.Bs),
                      Path(a => a.Bs[0]),
                      Path(a => a.Bs[0].String),
                      Path(a => a.Bs[0].String ?? "Grobas")
                );
        }

        [Test]
        public void TestStaticMethod()
        {
            TestSmash(a => StaticMethod(a.C).Int,
                      Path(a => StaticMethod(a.C)),
                      Path(a => StaticMethod(a.C).Int)
                );
        }

        [Test]
        public void TestInstanceMethod()
        {
            TestSmash(a => a.C.Add(3),
                      Path(a => a),
                      Path(a => a.C),
                      Path(a => a.C.Add(3))
                );
        }

        [Test]
        public void TestExtensionMethod()
        {
            TestSmash(a => a.C.Int.Multiply(5),
                      Path(a => a),
                      Path(a => a.C),
                      Path(a => a.C.Int),
                      Path(a => a.C.Int.Multiply(5))
                );
        }

        [Test]
        public void TestEach()
        {
            TestSmash(a => a.Bs.Each().String,
                      Path(a => a),
                      Path(a => a.Bs),
                      Path(a => a.Bs.Each()),
                      Path(a => a.Bs.Each().String)
                );
        }

        [Test]
        public void TestCurrent()
        {
            TestSmash(a => a.Bs.Current().String,
                      Path(a => a),
                      Path(a => a.Bs),
                      Path(a => a.Bs.Current()),
                      Path(a => a.Bs.Current().String)
                );
        }

        [Test]
        public void TestDictionaryGetItem()
        {
            TestSmash(a => a.Dictionary["zzz"].Length,
                      Path(a => a),
                      Path(a => a.Dictionary),
                      Path(a => a.Dictionary["zzz"]),
                      Path(a => a.Dictionary["zzz"].Length)
                );
        }

        [Test]
        public void TestNotSmashingNewArray()
        {
            TestNotSmashingExpression(a => new[] {a});
        }

        [Test]
        public void TestNotSmashingBinaryExpression()
        {
            TestNotSmashingExpression(a => a.C.Int + 5);
            TestNotSmashingExpression(a => a.C.Int == 42);
        }

        [Test]
        public void TestNotSmashingConditionalExpression()
        {
            TestNotSmashingExpression(a => a.C.Int == 0 ? 2 : 3);
        }

        [Test]
        public void TestNotSmashingUnaryExpression()
        {
            TestNotSmashingExpression(a => -a.C.Int);
            TestNotSmashingExpression(a => a.C as object);
        }

        [Test]
        public void TestNotSmashingConstant()
        {
            TestNotSmashingExpression(a => 42);
        }

        [Test]
        public void TestNotSmashingBlock()
        {
            var a = Expression.Variable(typeof(int));
            var b = Expression.Variable(typeof(int));
            var block = Expression.Block(typeof(int), new[] {a, b},
                                         Expression.Assign(a, Expression.Constant(2)),
                                         Expression.Assign(b, Expression.Constant(2)),
                                         Expression.Add(a, b));
            DoTest(block, block);
        }

        [Test]
        public void TestNotSmashingLambda()
        {
            Expression<Func<object, object>> lambda = x => x;
            DoTest(lambda, lambda);
        }

        private static T StaticMethod<T>(T value)
        {
            return value;
        }

        private LambdaExpression Path<T>(Expression<Func<A, T>> expression)
        {
            return expression;
        }

        private void TestNotSmashingExpression<T>(Expression<Func<A, T>> expression)
        {
            TestSmash(expression, expression);
        }

        private void TestSmash<T>(Expression<Func<A, T>> expression, params LambdaExpression[] expectedExpressions)
        {
            DoTest(expression.Body, expectedExpressions.Select(x => x.Body).ToArray());
        }

        private void DoTest(Expression expression, params Expression[] expectedExpressions)
        {
            var smithereens = expression.SmashToSmithereens();
            Assert.That(smithereens.Length, Is.EqualTo(expectedExpressions.Length),
                        $"Expected {expectedExpressions.Length} smithereens, but got {smithereens.Length}.\n" +
                        $"Result: {FormatExpressions(smithereens)}\n" +
                        $"Expected result: {FormatExpressions(expectedExpressions)}");
            for (var i = 0; i < expectedExpressions.Length; ++i)
            {
                var x = smithereens[i];
                var y = expectedExpressions[i];
                Assert.That(ExpressionEquivalenceChecker.Equivalent(x, y, strictly : false, distinguishEachAndCurrent : false),
                            $"Smithereens differ:\n{FormatExpression(x)}\n{FormatExpression(y)}\n\n" +
                            $"Result: {FormatExpressions(smithereens)}\n" +
                            $"Expected result: {FormatExpressions(expectedExpressions)}");
            }
        }

        private string FormatExpressions(IEnumerable<Expression> expressions)
        {
            return "[\n" + string.Join(",\n", expressions.Select(FormatExpression)) + "\n]\n";
        }

        private string FormatExpression(Expression expression)
        {
            return expression.ToString();
        }

        private class A
        {
            public B[] Bs { get; set; }

            public C C { get; set; }

            public Dictionary<string, string> Dictionary { get; set; }
        }

        private class B
        {
            public string String { get; set; }
        }

        private class C
        {
            public int Int { get; set; }

            public int Add(int value)
            {
                return Int + value;
            }
        }
    }

    internal static class TestExtensions
    {
        public static int Multiply(this int value, int multiplier)
        {
            return value * multiplier;
        }
    }
}