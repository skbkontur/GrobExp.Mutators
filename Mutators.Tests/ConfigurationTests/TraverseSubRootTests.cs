using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using FluentAssertions;

using GrobExp.Mutators;
using GrobExp.Mutators.ModelConfiguration;

using Mutators.Tests.Helpers;

using NUnit.Framework;

namespace Mutators.Tests.ConfigurationTests
{
    [TestFixture]
    public class TraverseSubRootTests
    {
        private ModelConfigurationNode root;

        [SetUp]
        public void SetUp()
        {
            root = ModelConfigurationNode.CreateRoot(null, typeof(Root));
        }

        [Test]
        public void TestEmptyPath()
        {
            Expression<Func<Root, Root>> path = x => x;

            root.Traverse(path.Body, root, out var child, create : false).Should().BeTrue();
            child.Should().BeSameAs(root);
        }

        [Test]
        public void TestMember()
        {
            Expression<Func<Root, string>> path = x => x.RootS;
            CheckSubRoot(path, path, result : true);
        }

        [Test]
        public void TestArrayLength()
        {
            Expression<Func<Root, int>> path = x => x.As.Length;
            CheckSubRoot(path, path, result: true);
        }

        [Test]
        public void TestEach()
        {
            Expression<Func<Root, A>> path = x => x.As.Each();
            CheckSubRoot(path, path, result: true);
        }

        [Test]
        public void TestCurrent()
        {
            Expression<Func<Root, A>> path = x => x.As.Current();
            CheckSubRoot(path, path, result: true);
        }

        [Test]
        public void TestConstantArrayIndex()
        {
            Expression<Func<Root, A>> path = x => x.As[2];
            CheckSubRoot(path, path, result: true);
        }

        [Test]
        public void TestConvert()
        {
            Expression<Func<Root, object>> path = x => (object)x;
            CheckSubRoot(path, path, result: true);
        }


        [Test]
        public void TestConstantArrayIndexNotExists()
        {
            Expression<Func<Root, string>> path1 = x => x.As.Each().S;
            Expression<Func<Root, string>> path2 = x => x.As[2].S;
            
            CheckSubRoot(path1, path2, result: true);
        }

        [Test]
        public void TestGetValueNotExists()
        {
            Expression<Func<Root, A>> path1 = x => x.As.Each();
            Expression<Func<Root, object>> path2 = x => x.As.GetValue(3);

            CheckSubRoot(path1, path2, result: true);
        }

        [Test]
        public void TestIndexer()
        {
            Expression<Func<Root, string>> path = x => x.Dict["zzz"];

            CheckSubRoot(path, path, result: true);
        }

        [Test]
        public void TestIndexerConcreteIndexNotExist()
        {
            Expression<Func<Root, string>> path = x => x.Dict.Each().Value;
            Expression<Func<Root, string>> path2 = x => x.Dict["qxx"];

            CheckSubRoot(path, path2, result : true);
        }

        [Test]
        public void TestDifferentPaths()
        {
            Expression<Func<Root, string>> path = x => x.As.Each().S;
            Expression<Func<Root, B[]>> path2 = x => x.As.Each().Bs;

            CheckSubRoot(path, path2, result: false);
        }

        [Test]
        public void TestPathPrefix()
        {
            Expression<Func<Root, B[]>> path1 = x => x.As.Each().Bs;
            Expression<Func<Root, string>> path2 = x => x.As.Each().Bs.Each().S;

            CheckSubRoot(path1, path2, result: true);
        }

        [Test]
        public void TestPathSuffix()
        {
            Expression<Func<Root, string>> path1 = x => x.As.Each().Bs.Each().S;
            Expression<Func<Root, B[]>> path2 = x => x.As.Each().Bs;

            CheckSubRoot(path1, path2, result: false);
        }

        [Test]
        public void TestEachAndCurrent()
        {
            Expression<Func<Root, B[]>> path1 = x => x.As.Each().Bs;
            Expression<Func<Root, B[]>> path2 = x => x.As.Current().Bs;

            CheckSubRoot(path1, path2, result: true);
        }

        private void CheckSubRoot(LambdaExpression pathToSubRoot, LambdaExpression pathToTraverse, bool result)
        {
            root.Traverse(pathToSubRoot.Body, null, out var child, create : true);
            root.Traverse(pathToTraverse.Body, child, out _, create : false).Should().Be(result);
        }

        private class Root
        {
            public A A { get; set; }

            public A[] As { get; set; }

            public string RootS { get; set; }

            public Dictionary<string, string> Dict { get; set; }
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