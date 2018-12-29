using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using GrobExp.Mutators;

using NUnit.Framework;

namespace Mutators.Tests
{
    public class AbstractPathResolverTest : TestBase
    {
        [Test]
        public void TestResolveAbstractPath1()
        {
            Expression<Func<A, string>> path = a => a.B.C[13].D.E[10].F;
            Expression<Func<A, D>> abstractPath = a => a.B.C.Current().D;
            var resolved = path.ResolveAbstractPath(abstractPath);
            Expression<Func<A, D>> expected = a => a.B.C[13].D;
            resolved.AssertEqualsExpression(expected);
        }

        [Test]
        public void TestResolveAbstractPath2()
        {
            Expression<Func<A, string>> path = a => a.B.C[13].D.E[10].F;
            Expression<Func<A, string>> abstractPath = a => a.B.C.Current().D.E.Current().F;
            var resolved = path.ResolveAbstractPath(abstractPath);
            Expression<Func<A, string>> expected = a => a.B.C[13].D.E[10].F;
            resolved.AssertEqualsExpression(expected);
        }

        [Test]
        public void TestResolveAbstractPath3()
        {
            Expression<Func<A, string>> path = a => a.B.C[13].D.E[10].F;
            Expression<Func<A, string>> abstractPath = a => a.B.C.Current().D.E[13].Z;
            var resolved = path.ResolveAbstractPath(abstractPath);
            Expression<Func<A, string>> expected = a => a.B.C[13].D.E[13].Z;
            resolved.AssertEqualsExpression(expected);
        }

        [Test]
        public void TestResolveAbstractPath4()
        {
            Expression<Func<A, string>> path = a => a.B.C[13].D.E[10].F;
            Expression<Func<A, string>> abstractPath = a => a.B.C[13].D.E.Current().Z;
            var resolved = path.ResolveAbstractPath(abstractPath);
            Expression<Func<A, string>> expected = a => a.B.C[13].D.E[10].Z;
            resolved.AssertEqualsExpression(expected);
        }

        [Test]
        public void TestResolveAbstractPathDict1()
        {
            Expression<Func<A, string>> path = a => a.B.CDict["13"].D.E[10].F;
            Expression<Func<A, D>> abstractPath = a => a.B.CDict.Current().Value.D;
            var resolved = path.ResolveAbstractPath(abstractPath);
            Expression<Func<A, D>> expected = a => a.B.CDict["13"].D;
            resolved.AssertEqualsExpression(expected);
        }

        [Test]
        public void TestResolveAbstractPathDict2()
        {
            Expression<Func<A, string>> path = a => a.B.CDict["13"].D.EDict["10"].F;
            Expression<Func<A, string>> abstractPath = a => a.B.CDict.Current().Value.D.EDict.Current().Value.F;
            var resolved = path.ResolveAbstractPath(abstractPath);
            Expression<Func<A, string>> expected = a => a.B.CDict["13"].D.EDict["10"].F;
            resolved.AssertEqualsExpression(expected);
        }

        [Test]
        public void TestResolveAbstractPathDict3()
        {
            Expression<Func<A, string>> path = a => a.B.CDict["13"].D.EDict["10"].F;
            Expression<Func<A, string>> abstractPath = a => a.B.CDict.Current().Value.D.EDict["13"].Z;
            var resolved = path.ResolveAbstractPath(abstractPath);
            Expression<Func<A, string>> expected = a => a.B.CDict["13"].D.EDict["13"].Z;
            resolved.AssertEqualsExpression(expected);
        }

        [Test]
        public void TestResolveAbstractPathDict4()
        {
            Expression<Func<A, string>> path = a => a.B.CDict["13"].D.EDict["10"].F;
            Expression<Func<A, string>> abstractPath = a => a.B.CDict["13"].D.EDict.Current().Value.Z;
            var resolved = path.ResolveAbstractPath(abstractPath);
            Expression<Func<A, string>> expected = a => a.B.CDict["13"].D.EDict["10"].Z;
            resolved.AssertEqualsExpression(expected);
        }

        [Test]
        public void TestResolveAbstractPathTemplateIndex()
        {
            Expression<Func<A, string>> path = a => a.B.C.TemplateIndex().D.E.TemplateIndex().F;
            Expression<Func<A, D>> abstractPath = a => a.B.C.Current().D;
            var resolved = path.ResolveAbstractPath(abstractPath);
            Expression<Func<A, D>> expected = a => a.B.C.TemplateIndex().D;
            resolved.AssertEqualsExpression(expected);
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
            public Dictionary<string, C> CDict { get; set; }
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
            public Dictionary<string, E> EDict { get; set; }
        }

        private class E
        {
            public string F { get; set; }
            public string Z { get; set; }
            public int? X { get; set; }
        }
    }
}