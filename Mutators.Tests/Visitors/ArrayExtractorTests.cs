using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using FluentAssertions;

using GrobExp.Mutators;
using GrobExp.Mutators.Visitors;

using NUnit.Framework;

namespace Mutators.Tests.Visitors
{
    [TestFixture]
    public class ArrayExtractorTests
    {
        [Test]
        public void TestNoArrays()
        {
            Expression<Func<A, string>> exp = x => x.S;
            var (level, type, list) = Visit(exp, paramMustBeUnique : true);
            level.Should().Be(0);
            type.Should().Be(typeof(A));
            list.Should().BeEmpty();
        }

        [Test]
        public void TestSingleArrayEach()
        {
            Expression<Func<A, string>> exp = x => x.Bs.Each().S;
            var (level, type, list) = Visit(exp, paramMustBeUnique : true);
            level.Should().Be(1);
            type.Should().Be(typeof(A));
            AssertEquivalentPaths(list,
                                  new[] {(typeof(A), new List<Expression> {((Expression<Func<A, B>>)(a => a.Bs.Each())).Body})}
                );
        }

        [Test]
        public void TestSingleArrayCurrent()
        {
            Expression<Func<A, string>> exp = x => x.Bs.Current().S;
            var (level, type, list) = Visit(exp, paramMustBeUnique : true);
            level.Should().Be(1);
            type.Should().Be(typeof(A));
            AssertEquivalentPaths(list,
                                  new[] {(typeof(A), new List<Expression> {((Expression<Func<A, B>>)(a => a.Bs.Current())).Body})}
                );
        }

        [Test]
        public void TestTwoArrays()
        {
            Expression<Func<A, int>> exp = x => x.Bs.Each().Cs.Each().N;
            var (level, type, list) = Visit(exp, paramMustBeUnique : true);
            level.Should().Be(2);
            type.Should().Be(typeof(A));
            AssertEquivalentPaths(list,
                                  new[] {(typeof(A), new List<Expression> {((Expression<Func<A, B>>)(a => a.Bs.Each())).Body})},
                                  new[] {(typeof(A), new List<Expression> {((Expression<Func<A, C>>)(a => a.Bs.Each().Cs.Each())).Body})}
                );
        }

        [Test]
        public void TestFirstShardIsCall()
        {
            Expression<Func<A, int>> exp = a => StaticGetBs(a.Bs.Each().Cs).Each().Cs.Each().N;
            var (level, type, list) = Visit(exp, paramMustBeUnique : true);
            level.Should().Be(3);
            type.Should().Be(typeof(A));
            AssertEquivalentPaths(list,
                                  new[] {(typeof(A), new List<Expression> {((Expression<Func<A, B>>)(a => a.Bs.Each())).Body})},
                                  new[] {(typeof(A), new List<Expression> {((Expression<Func<A, B>>)(a => StaticGetBs(a.Bs.Each().Cs).Each())).Body})},
                                  new[] {(typeof(A), new List<Expression> {((Expression<Func<A, C>>)(a => StaticGetBs(a.Bs.Each().Cs).Each().Cs.Each())).Body})}
                );
        }

        [Test]
        public void TestFirstShardIsConstant()
        {
            Expression<Func<A, int>> exp = a => InstanceGetBs(a.Bs.Each().Cs).Each().Cs.Each().N;
            var (level, type, list) = Visit(exp, paramMustBeUnique : true);
            level.Should().Be(3);
            type.Should().Be(typeof(A));
            AssertEquivalentPaths(list,
                                  new[] {(typeof(A), new List<Expression> {((Expression<Func<A, B>>)(a => a.Bs.Each())).Body})},
                                  new[] {(typeof(A), new List<Expression> {((Expression<Func<A, B>>)(a => InstanceGetBs(a.Bs.Each().Cs).Each())).Body})},
                                  new[] {(typeof(A), new List<Expression> {((Expression<Func<A, C>>)(a => InstanceGetBs(a.Bs.Each().Cs).Each().Cs.Each())).Body})}
                );
        }

        [Test]
        public void TestFirstShardIsInvoke()
        {
            Func<C[], B[]> firstShard = null;
            Expression<Func<A, int>> exp = a => firstShard(a.Bs.Each().Cs).Each().Cs.Each().N;
            var (level, type, list) = Visit(exp, paramMustBeUnique : true);
            level.Should().Be(3);
            type.Should().Be(typeof(A));
            AssertEquivalentPaths(list,
                                  new[] {(typeof(A), new List<Expression> {((Expression<Func<A, B>>)(a => a.Bs.Each())).Body})},
                                  new[] {(typeof(A), new List<Expression> {((Expression<Func<A, B>>)(a => firstShard(a.Bs.Each().Cs).Each())).Body})},
                                  new[] {(typeof(A), new List<Expression> {((Expression<Func<A, C>>)(a => firstShard(a.Bs.Each().Cs).Each().Cs.Each())).Body})}
                );
        }

        [Test]
        public void TestDifferentParameterTypes()
        {
            Func<C, B, int> func = null;
            Expression<Func<A, B, int>> exp = (a, b) => func(b.Cs.Each(), a.Bs.Each());
            var (level, type, list) = Visit(exp, paramMustBeUnique : false);
            level.Should().Be(1);
            type.Should().Be(typeof(B));
            AssertEquivalentPaths(list,
                                  new[]
                                      {
                                          (typeof(A), new List<Expression> {((Expression<Func<A, B>>)(a => a.Bs.Each())).Body}),
                                          (typeof(B), new List<Expression> {((Expression<Func<B, C>>)(b => b.Cs.Each())).Body})
                                      }
                );
        }

        [Test]
        public void TestTwoArraysFromOneParameter()
        {
            Func<B, B, int> func = null;
            Expression<Func<A, int>> exp = a => func(a.Bs.Each(), a.Bs.Each());
            var (level, type, list) = Visit(exp, paramMustBeUnique : false);
            level.Should().Be(1);
            type.Should().Be(typeof(A));
            AssertEquivalentPaths(list,
                                  new[]
                                      {
                                          (typeof(A), new List<Expression>
                                              {
                                                  ((Expression<Func<A, B>>)(a => a.Bs.Each())).Body,
                                                  ((Expression<Func<A, B>>)(a => a.Bs.Each())).Body
                                              }),
                                      }
                );
        }

        [Test]
        public void TestParamTypeMustBeUnique()
        {
            Expression<Func<A, B, string>> exp = (x, y) => x.Bs.Each().S + y.S;
            Action action = () => new ArraysExtractorVisitor(new List<Dictionary<Type, List<Expression>>>(), paramTypeMustBeUnique : true).GetArrays(exp);
            action.Should().Throw<InvalidOperationException>();

            action = () => new ArraysExtractorVisitor(new List<Dictionary<Type, List<Expression>>>(), paramTypeMustBeUnique : false).GetArrays(exp);
            action.Should().NotThrow();
        }

        [Test]
        public void TestParamTypeMustBeUniqueIncludingSubLevel()
        {
            Expression<Func<A, B, C>> exp = (x, y) => StaticMethod(x.Bs.Each().Cs.Append(StaticMethod(y.Cs).Each())).Each();
            Action action = () => new ArraysExtractorVisitor(new List<Dictionary<Type, List<Expression>>>(), paramTypeMustBeUnique : false).GetArrays(exp);
            action.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void TestParamTypeMustBeUniqueInsideEach()
        {
            Expression<Func<B, A, C>> exp = (x, y) => StaticMethod(x.Cs.Concat(y.Bs.Each().Cs)).Each();
            Action action = () => new ArraysExtractorVisitor(new List<Dictionary<Type, List<Expression>>>(), paramTypeMustBeUnique : false).GetArrays(exp);
            action.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void TestEachOnArrayInit()
        {
            Expression<Func<A, string>> exp = a => new []{a.S, a.Bs[0].S}.Each();
            var (level, type, list) = Visit(exp, paramMustBeUnique : false);
            level.Should().Be(1);
            type.Should().Be(typeof(A));
            AssertEquivalentPaths(list,
                                  new[]
                                      {
                                          (typeof(A), new List<Expression>{((Expression<Func<A, string>>)(a => new []{a.S, a.Bs[0].S}.Each())).Body}),
                                      }
                );
        }
        
        [Test]
        public void TestEachOnListInit()
        {
            Expression<Func<A, string>> exp = a => new List<string>{a.S, a.Bs[0].S}.Each();
            var (level, type, list) = Visit(exp, paramMustBeUnique : false);
            level.Should().Be(1);
            type.Should().Be(typeof(A));
            AssertEquivalentPaths(list,
                                  new[]
                                      {
                                          (typeof(A), new List<Expression>{((Expression<Func<A, string>>)(a => new List<string>{a.S, a.Bs[0].S}.Each())).Body}),
                                      }
                );
        }

        private void AssertEquivalentPaths(List<Dictionary<Type, List<Expression>>> actual, params (Type, List<Expression>)[][] expected)
        {
            actual.Should().BeEquivalentTo(expected.Select(x => x.ToDictionary(y => y.Item1, y => y.Item2)).Prepend(new Dictionary<Type, List<Expression>>()),
                                           opt => opt.Using<Expression>(ctx => ExpressionEquivalenceChecker.Equivalent(ctx.Subject, ctx.Expectation, strictly : false, distinguishEachAndCurrent : true).Should().BeTrue($"Subject: {ctx.Subject}\nExpected:{ctx.Expectation}"))
                                                     .WhenTypeIs<Expression>());
        }

        private (int Level, Type Type, List<Dictionary<Type, List<Expression>>> List) Visit(Expression exp, bool paramMustBeUnique)
        {
            var list = new List<Dictionary<Type, List<Expression>>>();
            var (level, type) = new ArraysExtractorVisitor(list, paramMustBeUnique).GetArrays(exp);
            return (level, type, list);
        }

        private static T StaticMethod<T>(T value) => value;

        private B[] InstanceGetBs(C[] cs)
        {
            return new B[0];
        }

        private static B[] StaticGetBs(C[] cs)
        {
            return new B[0];
        }

        private class A
        {
            public string S { get; set; }

            public B[] Bs { get; set; }
        }

        private class B
        {
            public string S { get; set; }

            public C[] Cs { get; set; }
        }

        private class C
        {
            public int N { get; set; }
        }
    }
}