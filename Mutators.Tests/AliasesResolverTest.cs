using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators;

using NUnit.Framework;

namespace Mutators.Tests
{
    [Parallelizable(ParallelScope.All)]
    public class AliasesResolverTest : TestBase
    {
        [Test]
        public void Test1()
        {
            Expression<Func<A, B>> path1 = a => a.B;
            var aliases = new List<KeyValuePair<Expression, Expression>>
                {
                    new KeyValuePair<Expression, Expression>(Expression.Parameter(typeof(B)), path1.Body)
                };
            Expression<Func<A, string>> exp = a => a.S;
            var resolved = exp.Body.ResolveAliases(aliases);
            resolved.AssertEqualsExpression(exp.Body);
        }

        [Test]
        public void Test2()
        {
            Expression<Func<A, B>> path1 = a => a.B;
            var aliases = new List<KeyValuePair<Expression, Expression>>
                {
                    new KeyValuePair<Expression, Expression>(Expression.Parameter(typeof(B), "b"), path1.Body),
                };
            Expression<Func<A, B>> exp = a => a.B;
            var resolved = exp.Body.ResolveAliases(aliases);
            resolved.AssertEqualsExpression(Expression.Parameter(typeof(B), "b"));
        }

        [Test]
        public void Test3()
        {
            Expression<Func<A, B>> path1 = a => a.B;
            var aliases = new List<KeyValuePair<Expression, Expression>>
                {
                    new KeyValuePair<Expression, Expression>(Expression.Parameter(typeof(B), "b"), path1.Body),
                };
            Expression<Func<A, string>> exp = a => a.B.S;
            var resolved = exp.Body.ResolveAliases(aliases);
            resolved.AssertEqualsExpression(((Expression<Func<B, string>>)(b => b.S)).Body);
        }

        [Test]
        public void Test4()
        {
            Expression<Func<A, B>> path1 = a => a.B;
            Expression<Func<A, C>> path2 = a => a.B.C.Each();
            var parameters = new List<KeyValuePair<Expression, Expression>>
                {
                    new KeyValuePair<Expression, Expression>(Expression.Parameter(typeof(B), "b"), path1.Body),
                    new KeyValuePair<Expression, Expression>(Expression.Parameter(typeof(C), "c"), path2.Body),
                };
            Expression<Func<A, C>> exp = a => a.B.C.Each();
            var resolved = exp.Body.ResolveAliases(parameters);
            resolved.AssertEqualsExpression(Expression.Parameter(typeof(C), "c"));
        }

        [Test]
        public void Test5()
        {
            Expression<Func<A, B>> path1 = a => a.B;
            Expression<Func<A, C>> path2 = a => a.B.C.Each();
            var parameters = new List<KeyValuePair<Expression, Expression>>
                {
                    new KeyValuePair<Expression, Expression>(Expression.Parameter(typeof(B), "b"), path1.Body),
                    new KeyValuePair<Expression, Expression>(Expression.Parameter(typeof(C), "c"), path2.Body),
                };
            Expression<Func<A, D>> exp = a => a.B.C.Each().D;
            var resolved = exp.Body.ResolveAliases(parameters);
            resolved.AssertEqualsExpression(((Expression<Func<C, D>>)(c => c.D)).Body);
        }

        [Test]
        public void Test6()
        {
            Expression<Func<A, B>> path1 = a => a.B;
            Expression<Func<A, C>> path2 = a => a.B.C.Each();
            Expression<Func<A, E>> path3 = a => a.B.C.Each().D.E.Each();
            var parameters = new List<KeyValuePair<Expression, Expression>>
                {
                    new KeyValuePair<Expression, Expression>(Expression.Parameter(typeof(B), "b"), path1.Body),
                    new KeyValuePair<Expression, Expression>(Expression.Parameter(typeof(C), "c"), path2.Body),
                    new KeyValuePair<Expression, Expression>(Expression.Parameter(typeof(E), "e"), path3.Body),
                };
            Expression<Func<A, string>> exp = a => a.B.C.Each().D.E.Each().F;
            var resolved = exp.Body.ResolveAliases(parameters);
            resolved.AssertEqualsExpression(((Expression<Func<E, string>>)(e => e.F)).Body);
        }

        [Test]
        public void Test7()
        {
            Expression<Func<A, B>> path1 = a => a.B;
            Expression<Func<A, C>> path2 = a => a.B.C.Current();
            Expression<Func<A, int>> path3 = a => a.B.C.Current().CurrentIndex();
            var parameters = new List<KeyValuePair<Expression, Expression>>
                {
                    new KeyValuePair<Expression, Expression>(Expression.Parameter(typeof(B), "b"), path1.Body),
                    new KeyValuePair<Expression, Expression>(Expression.Parameter(typeof(C), "c"), path2.Body),
                    new KeyValuePair<Expression, Expression>(Expression.Parameter(typeof(int), "i"), path3.Body),
                };
            Expression<Func<A, C>> exp = a => a.B.C[a.B.C.Current().CurrentIndex()];
            var resolved = exp.Body.ResolveAliases(parameters);
            resolved.AssertEqualsExpression(((Expression<Func<B, int, C>>)((b, i) => b.C[i])).Body);
        }

        [Test]
        public void Test8()
        {
            Expression<Func<A, B>> path1 = a => a.B;
            Expression<Func<A, C>> path2 = a => a.B.C.Where(c => c.S == "zzz").Current();
            var parameters = new List<KeyValuePair<Expression, Expression>>
                {
                    new KeyValuePair<Expression, Expression>(Expression.Parameter(typeof(B), "b"), path1.Body),
                    new KeyValuePair<Expression, Expression>(Expression.Parameter(typeof(C), "c"), path2.Body),
                };
            Expression<Func<A, string>> exp = a => a.B.C.Where(c => c.S == "zzz").Current().D.S;
            var resolved = exp.Body.ResolveAliases(parameters);
            resolved.AssertEqualsExpression(((Expression<Func<C, string>>)(c => c.D.S)).Body);
        }

        [Test]
        public void TestThatParameterNameDoesNotChangeOnAliasWithDiffentParams()
        {
            Expression<Func<F, H>> path1 = x => x.Gs.Current().Hs.Current();
            Expression<Func<F, H>> path2 = data => data.Gs.Where(x => x.IsRemoved != true).Each().Hs.Current();
            var parameters = new List<KeyValuePair<Expression, Expression>>
                {
                    new KeyValuePair<Expression, Expression>(path1.Body, path2.Body),
                };
            Expression<Func<F, string>> exp = name => name.Gs.Where(x => x.IsRemoved != true).Current().Hs.Current().Value;
            var resolved = exp.ResolveAliasesInLambda(parameters);

            var expected = Expression.Lambda(((Expression<Func<F, string>>)(x => x.Gs.Current().Hs.Current().Value)).Body, ((Expression<Func<F, F>>)(name => name)).Parameters); // name => x.Gs.Current().Hs.Current().Value
            resolved.AssertEqualsExpression(expected);
        }

        [Test]
        public void TestThatLambdaParameterNameChanges()
        {
            Expression<Func<F, H>> path1 = data => data.Gs.Current().Hs.Current();
            Expression<Func<F, H>> path2 = data => data.Gs.Where(inner => inner.IsRemoved != true).Each().Hs.Current();
            var parameters = new List<KeyValuePair<Expression, Expression>>
                {
                    new KeyValuePair<Expression, Expression>(path1.Body, path2.Body),
                };
            Expression<Func<F, string>> exp = lalala => lalala.Gs.Where(anyName => anyName.IsRemoved != true).Current().Hs.Current().Value;
            var resolved = exp.ResolveAliasesInLambda(parameters);
            resolved.AssertEqualsExpression((Expression<Func<F, string>>)(data => data.Gs.Current().Hs.Current().Value));
        }

        [Test]
        public void TestThatLambdaParameterNameChangesWhenThereAreTwoParameters()
        {
            Expression<Func<F, H>> path1 = data => data.Gs.Current().Hs.Current();
            Expression<Func<F, H>> path2 = data => data.Gs.Where(x => x.IsRemoved != true).Each().Hs.Current();
            var parameters = new List<KeyValuePair<Expression, Expression>>
                {
                    new KeyValuePair<Expression, Expression>(path1.Body, path2.Body),
                };
            Expression<Func<F, string, string>> exp = (name1, name2) => name1.Gs.Where(x => x.IsRemoved != true).Current().Hs.Current().Value + name2;
            var resolved = exp.ResolveAliasesInLambda(parameters);
            resolved.AssertEqualsExpression((Expression<Func<F, string, string>>)((data, name2) => data.Gs.Current().Hs.Current().Value + name2));
        }

        [Test]
        public void TestThatLambdaParameterNameChangesWhenThereAreTwoParametersOfSameType()
        {
            Expression<Func<F, H>> path1 = p1 => p1.Gs.Current().Hs.Current();
            Expression<Func<F, H>> path2 = p1 => p1.Gs.Where(x => x.IsRemoved != true).Each().Hs.Current();
            var parameters = new List<KeyValuePair<Expression, Expression>>
                {
                    new KeyValuePair<Expression, Expression>(path1.Body, path2.Body),
                };
            Expression<Func<F, F, string>> exp = (name1, name2) => name1.Gs.Where(x => x.IsRemoved != true).Current().Hs.Current().Value + name2.Gs.Current().IsRemoved;
            var resolved = exp.ResolveAliasesInLambda(parameters);
            resolved.AssertEqualsExpression((Expression<Func<F, F, string>>)((p1, name2) => p1.Gs.Current().Hs.Current().Value + name2.Gs.Current().IsRemoved));
        }

        private class A
        {
            public B B { get; set; }
            public string S { get; set; }
            public int? X { get; set; }
        }

        private class B
        {
            public C[] C { get; set; }
            public string S { get; set; }
            public int? X { get; set; }
        }

        private class C
        {
            public D D { get; set; }
            public string S { get; set; }
            public int? X { get; set; }
        }

        private class D
        {
            public E[] E { get; set; }
            public string S { get; set; }
            public int? X { get; set; }
            public int?[] Z { get; set; }
        }

        private class E
        {
            public string F { get; set; }
            public string Z { get; set; }
            public int? X { get; set; }
        }

        private class F
        {
            public G[] Gs { get; set; }
        }

        private class G
        {
            public H[] Hs { get; set; }
            public bool IsRemoved { get; set; }
        }

        private class H
        {
            public string Value { get; set; }
        }
    }
}