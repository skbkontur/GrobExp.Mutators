using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using FluentAssertions;

using GrobExp.Mutators;
using GrobExp.Mutators.ModelConfiguration;
using GrobExp.Mutators.Visitors;

using NUnit.Framework;

namespace Mutators.Tests.ConfigurationTests
{
    [TestFixture]
    public class TraverseStructuralTests
    {
        [Test]
        public void TestEmptyPath()
        {
            var treeBuilder = new NodeBuilder(typeof(Root));
            Expression<Func<Root, Root>> path = d => d;
            DoTest(treeBuilder, path);
        }

        [Test]
        public void TestConvert()
        {
            var treeBuilder = new NodeBuilder(typeof(Root))
                {
                    [Edge.Create((Root d) => (object)d)] = new NodeBuilder(typeof(object)),
                };
            Expression<Func<Root, object>> path = d => (object)d;
            DoTest(treeBuilder, path);
        }

        [Test]
        public void TestMember()
        {
            var treeBuilder = new NodeBuilder(typeof(Root))
                {
                    [Edge.Create((Root x) => x.RootS)] = new NodeBuilder(typeof(string)),
                };

            Expression<Func<Root, string>> path = d => d.RootS;
            DoTest(treeBuilder, path);
        }

        [Test]
        public void TestEach()
        {
            var treeBuilder = new NodeBuilder(typeof(Root))
                {
                    [Edge.Create((Root x) => x.As)] = new NodeBuilder(typeof(A[]))
                        {
                            [ModelConfigurationEdge.Each] = new NodeBuilder(typeof(A)),
                        },
                };

            Expression<Func<Root, A>> path = d => d.As.Each();
            DoTest(treeBuilder, path);
        }

        [Test]
        public void TestCurrent()
        {
            var treeBuilder = new NodeBuilder(typeof(Root))
                {
                    [Edge.Create((Root x) => x.As)] = new NodeBuilder(typeof(A[]))
                        {
                            [ModelConfigurationEdge.Each] = new NodeBuilder(typeof(A)),
                        },
                };

            Expression<Func<Root, A>> path = d => d.As.Current();
            DoTest(treeBuilder, path);
        }

        [Test]
        public void TestArrayIndex()
        {
            var treeBuilder = new NodeBuilder(typeof(Root))
                {
                    [Edge.Create((Root x) => x.As)] = new NodeBuilder(typeof(A[]))
                        {
                            [Edge.Create((A[] @as) => @as[0])] = new NodeBuilder(typeof(A)),
                        },
                };

            Expression<Func<Root, A>> path = d => d.As[0];
            DoTest(treeBuilder, path);
        }

        [Test]
        public void TestComplexArrayIndex()
        {
            var treeBuilder = new NodeBuilder(typeof(Root))
                {
                    [Edge.Create((Root x) => x.As)] = new NodeBuilder(typeof(A[]))
                        {
                            [Edge.Create((A[] @as) => @as[Index()])] = new NodeBuilder(typeof(A)),
                        },
                };

            Expression<Func<Root, A>> path = d => d.As[Index()];
            DoTest(treeBuilder, path);
        }

        [Test]
        public void TestGetValue()
        {
            var treeBuilder = new NodeBuilder(typeof(Root))
                {
                    [Edge.Create((Root x) => x.As)] = new NodeBuilder(typeof(A[]))
                        {
                            [Edge.Create((A[] @as) => @as[5])] = new NodeBuilder(typeof(A)),
                        },
                };

            Expression<Func<Root, object>> path = d => d.As.GetValue(5);
            DoTest(treeBuilder, path);
        }

        [Test]
        public void TestIndexerGetter()
        {
            var treeBuilder = new NodeBuilder(typeof(Root))
                {
                    [Edge.Create((Root x) => x.Dict)] = new NodeBuilder(typeof(Dictionary<string, string>))
                        {
                            [Edge.Create((Dictionary<string, string> dict) => dict["5"])] = new NodeBuilder(typeof(string)),
                        },
                };
            Expression<Func<Root, string>> path = d => d.Dict["5"];
            DoTest(treeBuilder, path);
        }

        [Test]
        public void TestArrayLength()
        {
            var treeBuilder = new NodeBuilder(typeof(Root))
                {
                    [Edge.Create((Root x) => x.As)] = new NodeBuilder(typeof(A[]))
                        {
                            [ModelConfigurationEdge.ArrayLength] = new NodeBuilder(typeof(int)),
                        },
                };
            Expression<Func<Root, int>> path = d => d.As.Length;
            DoTest(treeBuilder, path);
        }

        private int Index()
        {
            return 0;
        }

        [Test]
        public void TestMultiplePaths()
        {
            var rootBuilder = new NodeBuilder(typeof(Root))
                {
                    [Edge.Create((Root x) => x.As)] = new NodeBuilder(typeof(A[]))
                        {
                            [ModelConfigurationEdge.Each] = new NodeBuilder(typeof(A))
                                {
                                    [Edge.Create((A a) => a.S)] = new NodeBuilder(typeof(string)),
                                },
                            [Edge.Create((A[] @as) => @as[1])] = new NodeBuilder(typeof(A))
                                {
                                    [Edge.Create((A a) => a.Bs)] = new NodeBuilder(typeof(B[]))
                                        {
                                            [ModelConfigurationEdge.ArrayLength] = new NodeBuilder(typeof(int)),
                                        },
                                },
                        },
                };

            Expression<Func<Root, string>> path1 = d => d.As.Each().S;
            Expression<Func<Root, int>> path2 = d => d.As[1].Bs.Length;

            DoTest(rootBuilder, path1, path2);
        }

        private void DoTest(NodeBuilder treeBuilder, params LambdaExpression[] pathsToTraverse)
        {
            var expectedTree = treeBuilder.Build();
            var root = ModelConfigurationNode.CreateRoot(null, typeof(Root));
            foreach (var path in pathsToTraverse)
            {
                root.Traverse(path.Body, create : true);
            }
            AssertEquivalentTrees(expectedTree, root);
        }

        private void AssertEquivalentTrees(ModelConfigurationNode expected, ModelConfigurationNode actual)
        {
            actual.NodeType.Should().Be(expected.NodeType);
            actual.RootType.Should().Be(expected.RootType);
            actual.Edge.Should().Be(expected.Edge);
            AssertEquivalentExpressions(expected.Path, actual.Path);

            actual.children.Should().BeEquivalentTo(expected.children, config => config.Using<ModelConfigurationNode>(x => AssertEquivalentTrees(x.Expectation, x.Subject))
                                                                                       .WhenTypeIs<ModelConfigurationNode>());
        }

        private static void AssertEquivalentExpressions(Expression expected, Expression actual)
        {
            ExpressionEquivalenceChecker.Equivalent(expected, actual, false, true)
                                        .Should().BeTrue($"because\nExpected:\n{expected}\n\nActual:\n{actual}");
        }

        private class Root
        {
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