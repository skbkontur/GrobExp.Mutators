using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using FluentAssertions;

using GrEmit.Utils;

using GrobExp.Mutators;
using GrobExp.Mutators.ModelConfiguration;

using Mutators.Tests.Helpers;

using NUnit.Framework;

namespace Mutators.Tests.ConfigurationTests
{
    [TestFixture]
    public class TraverseChildTests
    {
        private ModelConfigurationNode root;

        [SetUp]
        public void SetUp()
        {
            root = ModelConfigurationNode.CreateRoot(null, typeof(Root));
        }

        [Test]
        public void TestMember()
        {
            Expression<Func<Root, string>> path = x => x.RootS;
            TestExistsCreate(path, Edge.Create((Root r) => r.RootS));
        }

        [Test]
        public void TestArrayLength()
        {
            Expression<Func<Root, int>> path = x => x.As.Length;
            TestExistsCreate(path, Edge.Create((Root r) => r.As), ModelConfigurationEdge.ArrayLength);
        }

        [Test]
        public void TestEach()
        {
            Expression<Func<Root, A>> path = x => x.As.Each();
            TestExistsCreate(path, Edge.Create((Root r) => r.As), ModelConfigurationEdge.Each);
        }

        [Test]
        public void TestCurrent()
        {
            Expression<Func<Root, A>> path = x => x.As.Current();
            TestExistsCreate(path, Edge.Create((Root r) => r.As), ModelConfigurationEdge.Each);
        }

        [Test]
        public void TestConstantArrayIndex()
        {
            Expression<Func<Root, A>> path = x => x.As[2];
            TestExistsCreate(path, Edge.Create((Root r) => r.As), Edge.Create((A[] @as) => @as[2]));
        }

        [Test]
        public void TestEmptyPath()
        {
            Expression<Func<Root, Root>> path = x => x;
            TestExistsCreate(path, new ModelConfigurationEdge[0]);
        }

        [Test]
        public void TestConvert()
        {
            Expression<Func<Root, object>> path = x => (object)x;
            TestExistsCreate(path, Edge.Create((Root r) => (object)r));
        }

        [Test]
        public void TestNotCreatePrefix()
        {
            Expression<Func<Root, A>> path = x => x.As[2];
            root.Traverse(path.Body, create: true);
            Expression<Func<Root, A[]>> pathPrefix = x => x.As;
            TestExistsNotCreate(pathPrefix, Edge.Create((Root r) => r.As));
        }

        [Test]
        public void TestConstantArrayIndexNotExists()
        {
            Expression<Func<Root, string>> path1 = x => x.As.Each().S;
            root.Traverse(path1.Body, create : true);
            Expression<Func<Root, A>> path2 = x => x.As[2];
            TestExistsNotCreate(path2, Edge.Create((Root r) => r.As), ModelConfigurationEdge.Each);
        }

        [Test]
        public void TestGetValueNotExists()
        {
            Expression<Func<Root, string>> path1 = x => x.As.Each().S;
            root.Traverse(path1.Body, create: true);
            Expression<Func<Root, object>> path2 = x => x.As.GetValue(2);
            TestExistsNotCreate(path2, Edge.Create((Root r) => r.As), ModelConfigurationEdge.Each);
        }

        [Test]
        public void TestIdempotentTraverseWithCreate()
        {
            Expression<Func<Root, string>> path = x => x.As.Each().S;
            var child1 = root.Traverse(path.Body, create: true);
            var child2 = root.Traverse(path.Body, create: true);
            child1.Should().BeSameAs(child2);
        }

        [Test]
        public void TestIdempotentTraverseWithoutCreate()
        {
            Expression<Func<Root, string>> path = x => x.As.Each().S;
            var child1 = root.Traverse(path.Body, create: true);
            var child2 = root.Traverse(path.Body, create: false);
            child1.Should().BeSameAs(child2);
        }

        [Test]
        public void TestIndexer()
        {
            Expression<Func<Root, string>> path = x => x.Dict["zzz"];
            root.Traverse(path.Body, create: true);
            TestExistsCreate(path, Edge.Create((Root r) => r.Dict), Edge.Create((Dictionary<string, string> d) => d["zzz"]));
        }

        [Test]
        public void TestIndexerConcreteIndexNotExist()
        {
            Expression<Func<Root, string>> path = x => x.Dict.Each().Value;
            root.Traverse(path.Body, create: true);
            Expression<Func<Root, string>> path2 = x => x.Dict["qxx"];
            TestExistsNotCreate(path2, Edge.Create((Root r) => r.Dict), ModelConfigurationEdge.Each, Edge.Create((KeyValuePair<string, string> p) => p.Value));
        }

        [Test]
        public void TestGetOnlyIndexer()
        {
            Expression<Func<Root, string>> path = x => x.IndexerGet["abc"];

            Following.Code(() => root.Traverse(path.Body, create : true))
                     .Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void TestSetOnlyIndexer()
        {
            Expression<Func<Root, IndexerSet>> path = r => r.IndexerSet;
            var method = typeof(IndexerSet).GetProperty("Item").GetSetMethod();

            var path3 = Expression.Call(path.Body, method, Expression.Constant("zzz"), Expression.Constant("zzz"));

            Following.Code(() => root.Traverse(path3, create: true))
                     .Should().Throw<NotSupportedException>()
                     .Which.Message.Should().MatchRegex("^Method .* is not supported$");
        }

        private void TestExistsCreate(LambdaExpression path, params ModelConfigurationEdge[] edges)
        {
            DoTestExists(path, create : true, edges: edges);
        }

        private void TestExistsNotCreate(LambdaExpression path, params ModelConfigurationEdge[] edges)
        {
            DoTestExists(path, create: false, edges: edges);
        }

        private void DoTestExists(LambdaExpression path, bool create, params ModelConfigurationEdge[] edges)
        {
            root.Traverse(path.Body, null, out var child, create).Should().BeFalse();
            var node = root;
            foreach (var edge in edges)
                node = node.children[edge];
            node.Should().NotBeNull().And.BeSameAs(child);
        }

        private class Root
        {
            public A A { get; set; }

            public A[] As { get; set; }

            public string RootS { get; set; }

            public Dictionary<string, string> Dict { get; set; }

            public IndexerGet IndexerGet { get; set; }

            public IndexerSet IndexerSet { get; set; }
        }

        private class IndexerGet
        {
            public string this[string key] { get => "zzz"; }
        }

        private class IndexerSet
        {
            public string this[string key]
            {
                set => Console.WriteLine(key, value);
            }
        }

        private class A
        {
            public string S { get; set; }

            public B[] Bs { get; set; }
        }

        private class B
        {
            public string S { get; set; }
        }
    }
}