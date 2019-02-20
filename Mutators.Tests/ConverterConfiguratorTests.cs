using System;
using System.Linq;
using System.Linq.Expressions;

using FluentAssertions;
using FluentAssertions.Equivalency;

using GrobExp.Mutators;
using GrobExp.Mutators.AutoEvaluators;
using GrobExp.Mutators.Visitors;

using NUnit.Framework;

namespace Mutators.Tests
{
    [TestFixture]
    public class ConverterConfiguratorTests
    {
        [SetUp]
        public void SetUp()
        {
            reporter = new ConfiguratorReporter();
            configurator = new ConverterConfigurator<SourceRoot, DestRoot>(reporter, ModelConfigurationNode.CreateRoot(null, typeof(DestRoot)));
        }

        [Test]
        public void TestSimplePath()
        {
            configurator.Target(d => d.RootS).Set(s => s.RootS);

            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.RootS), null);
            AssertEquivalentConfigurations(equalsToConfiguration, reporter.Mutator);
            AssertEquivalentPaths(d => d.RootS, reporter.Path);
        }

        [Test]
        public void TestPathWithEachAndCurrent()
        {
            configurator.Target(d => d.As.Each().S).Set(s => s.As.Current().S);

            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.As.Current().S), null);
            AssertEquivalentConfigurations(equalsToConfiguration, reporter.Mutator);
            AssertEquivalentPaths(d => d.As.Each().S, reporter.Path);
        }
        
        [Test]
        public void TestPathWithEachAndEach()
        {
            configurator.Target(d => d.As.Each().S).Set(s => s.As.Each().S);

            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.As.Each().S), null);
            AssertEquivalentConfigurations(equalsToConfiguration, reporter.Mutator);
            AssertEquivalentPaths(d => d.As.Each().S, reporter.Path);
        }

        [Test]
        public void TestWithCondition()
        {
            configurator.Target(d => d.RootS).If(s => s.A.S == "zzz").Set(s => s.RootS);

            var equalsToIfConfiguration = EqualsToIfConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, bool?>>)(s => s.A.S == "zzz"), 
                                                                                 (Expression<Func<SourceRoot, string>>)(s => s.RootS), 
                                                                                 null);
            AssertEquivalentConfigurations(equalsToIfConfiguration, reporter.Mutator);
            AssertEquivalentPaths(d => d.RootS, reporter.Path);
        }
        
        [Test]
        public void TestWithConditionBeforeTarget()
        {
            configurator.If(s => s.A.S == "zzz").Target(d => d.RootS).Set(s => s.RootS);

            var equalsToIfConfiguration = EqualsToIfConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, bool?>>)(s => s.A.S == "zzz"), 
                                                                                 (Expression<Func<SourceRoot, string>>)(s => s.RootS), 
                                                                                 null);
            AssertEquivalentConfigurations(equalsToIfConfiguration, reporter.Mutator);
            AssertEquivalentPaths(d => d.RootS, reporter.Path);
        }
        
        [Test]
        public void TestWithTwoConditions()
        {
            Expression<Func<SourceRoot, bool?>> firstCondition = s => s.A.S == "zzz";
            Expression<Func<SourceRoot, bool?>> secondCondition = s => s.RootS.Length == 3;
            configurator.Target(d => d.RootS).If(firstCondition).If(secondCondition).Set(s => s.RootS);

            var equalsToIfConfiguration = EqualsToIfConfiguration.Create<DestRoot>(firstCondition.AndAlso(secondCondition),
                                                                                 (Expression<Func<SourceRoot, string>>)(s => s.RootS), 
                                                                                 null);
            AssertEquivalentConfigurations(equalsToIfConfiguration, reporter.Mutator);
            AssertEquivalentPaths(d => d.RootS, reporter.Path);
        }
        
        [Test]
        public void TestTwoConditionsBeforeTarget()
        {
            Expression<Func<SourceRoot, bool?>> firstCondition = s => s.A.S == "zzz";
            Expression<Func<SourceRoot, bool?>> secondCondition = s => s.RootS.Length == 3;
            configurator.If(firstCondition).If(secondCondition).Target(d => d.RootS).Set(s => s.RootS);

            var equalsToIfConfiguration = EqualsToIfConfiguration.Create<DestRoot>(firstCondition.AndAlso(secondCondition),
                                                                                   (Expression<Func<SourceRoot, string>>)(s => s.RootS), 
                                                                                   null);
            AssertEquivalentConfigurations(equalsToIfConfiguration, reporter.Mutator);
            AssertEquivalentPaths(d => d.RootS, reporter.Path);
        }

        [Test]
        public void TestConditionWithCurrent()
        {
            configurator.Target(d => d.As.Each().S)
                        .If(s => s.As.Current().S.Length == 5)
                        .Set(s => s.As.Current().S);

            var equalsToIfConfiguration = EqualsToIfConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, bool?>>)(s => s.As.Current().S.Length == 5), 
                                                                                 (Expression<Func<SourceRoot, string>>)(s => s.As.Current().S), 
                                                                                 null);
            AssertEquivalentConfigurations(equalsToIfConfiguration, reporter.Mutator);
            AssertEquivalentPaths(d => d.As.Each().S, reporter.Path);
        }
        
        [Test]
        public void TestConditionWithCurrentBeforeTarget()
        {
            configurator.If(s => s.As.Current().S.Length == 5)
                        .Target(d => d.As.Each().S)
                        .Set(s => s.As.Current().S);

            var equalsToIfConfiguration = EqualsToIfConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, bool?>>)(s => s.As.Current().S.Length == 5), 
                                                                                   (Expression<Func<SourceRoot, string>>)(s => s.As.Current().S), 
                                                                                   null);
            AssertEquivalentConfigurations(equalsToIfConfiguration, reporter.Mutator);
            AssertEquivalentPaths(d => d.As.Each().S, reporter.Path);
        }
        
        [Test]
        public void TestConditionWithEach()
        {
            configurator.Target(d => d.As.Each().S)
                        .If(s => s.As.Each().S.Length == 5)
                        .Set(s => s.As.Current().S);

            var equalsToIfConfiguration = EqualsToIfConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, bool?>>)(s => s.As.Current().S.Length == 5), 
                                                                                 (Expression<Func<SourceRoot, string>>)(s => s.As.Current().S), 
                                                                                 null);
            AssertEquivalentConfigurations(equalsToIfConfiguration, reporter.Mutator);
            AssertEquivalentPaths(d => d.As.Each().S, reporter.Path);
        }
        
        [Test]
        public void TestConditionWithDest()
        {
            configurator.Target(d => d.A.S)
                        .If((s, d) => s.As.Any(x => x.S == "zzz") && d.RootS == "Dest")
                        .Set(s => s.A.S);

            var equalsToIfConfiguration = EqualsToIfConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, DestRoot, bool?>>)((s, d) => s.As.Any(x => x.S == "zzz") && d.RootS == "Dest"), 
                                                                                 (Expression<Func<SourceRoot, string>>)(s => s.A.S), 
                                                                                 null);
            AssertEquivalentConfigurations(equalsToIfConfiguration, reporter.Mutator);
            AssertEquivalentPaths(d => d.A.S, reporter.Path);
        }
        
        [Test]
        public void TestConditionWithDestBeforeTarget()
        {
            configurator.If((s, d) => s.As.Any(x => x.S == "zzz") && d.RootS == "Dest")
                        .Target(d => d.A.S)
                        .Set(s => s.A.S);

            var equalsToIfConfiguration = EqualsToIfConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, DestRoot, bool?>>)((s, d) => s.As.Any(x => x.S == "zzz") && d.RootS == "Dest"), 
                                                                                   (Expression<Func<SourceRoot, string>>)(s => s.A.S), 
                                                                                   null);
            AssertEquivalentConfigurations(equalsToIfConfiguration, reporter.Mutator);
            AssertEquivalentPaths(d => d.A.S, reporter.Path);
        }


        [Test]
        public void TestWithoutCondition()
        {
            configurator.Target(d => d.RootS).If(s => s.A.S == "zzz").WithoutCondition().Set(s => s.RootS);

            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.RootS), 
                                                                                 null);
            AssertEquivalentConfigurations(equalsToConfiguration, reporter.Mutator);
            AssertEquivalentPaths(d => d.RootS, reporter.Path);
        }
        
        [Test]
        public void TestWithoutConditionBeforeTarget()
        {
            configurator.If(s => s.A.S == "zzz").WithoutCondition().Target(d => d.RootS).Set(s => s.RootS);

            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.RootS), 
                                                                               null);
            AssertEquivalentConfigurations(equalsToConfiguration, reporter.Mutator);
            AssertEquivalentPaths(d => d.RootS, reporter.Path);
        }

        [Test]
        public void TestGotoInDest()
        {
            var subConfigurator = configurator.GoTo(d => d.As.Each());
            subConfigurator.Target(d => d.S).Set(s => s.RootS);

            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.RootS), 
                                                                               null);
            AssertEquivalentConfigurations(equalsToConfiguration, reporter.Mutator);
            AssertEquivalentPaths(d => d.As.Each().S, reporter.Path);
        }
        
        [Test]
        public void TestGotoInDestTwice()
        {
            var subConfigurator = configurator.GoTo(d => d.As.Each()).GoTo(d => d.S);
            subConfigurator.Target(d => d).Set(s => s.RootS);

            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.RootS), 
                                                                               null);
            AssertEquivalentConfigurations(equalsToConfiguration, reporter.Mutator);
            AssertEquivalentPaths(d => d.As.Each().S, reporter.Path);
        }
        
        [Test]
        public void TestGotoInDestAndSource()
        {
            var subConfigurator = configurator.GoTo(d => d.As.Each(), s => s.As.Current());
            subConfigurator.Target(d => d.S).Set(s => s.S);

            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.As.Current().S), 
                                                                               null);
            AssertEquivalentConfigurations(equalsToConfiguration, reporter.Mutator);
            AssertEquivalentPaths(d => d.As.Each().S, reporter.Path);
        }

        [Test]
        public void TestGotoWithCondition()
        {
            var subConfigurator = configurator.GoTo(d => d.As.Each(), s => s.As.Current());
            subConfigurator.Target(d => d.S).If(s => s.S.Length % 2 == 0).Set(s => s.S);

            var equalsToConfiguration = EqualsToIfConfiguration.Create<DestRoot>( (Expression<Func<SourceRoot, bool?>>)(s => s.As.Current().S.Length % 2 == 0),
                                                                                  (Expression<Func<SourceRoot, string>>)(s => s.As.Current().S), 
                                                                                  null);
            AssertEquivalentConfigurations(equalsToConfiguration, reporter.Mutator);
            AssertEquivalentPaths(d => d.As.Each().S, reporter.Path);
        }
        
        [Test]
        public void TestReplaceEachToCurrentOnlyInGotoPath()
        {
            var subConfigurator = configurator.GoTo(d => d.As.Each(), s => s.As.Each());
            subConfigurator.Target(d => d.Bs.Each().S).If((s, d) => s.S.Length % 2 == 0 && d.S.Length % 2 == 1).Set(s => s.Bs.Each().S);

            var equalsToConfiguration = EqualsToIfConfiguration.Create<DestRoot>( (Expression<Func<SourceRoot, DestRoot, bool?>>)((s, d) => s.As.Current().S.Length % 2 == 0 && d.As.Current().S.Length % 2 == 1),
                                                                                  (Expression<Func<SourceRoot, string>>)(s => s.As.Current().Bs.Each().S), 
                                                                                  null);
            AssertEquivalentConfigurations(equalsToConfiguration, reporter.Mutator);
            AssertEquivalentPaths(d => d.As.Each().Bs.Each().S, reporter.Path);
        }
        
        [Test]
        public void TestResolvingInterfaceMembersInTargetPath()
        {
            configurator.Target(d =>  ((Interface)d.Impl).S).Set(s => s.RootS);
            
            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.RootS), 
                                                                               null);
            AssertEquivalentConfigurations(equalsToConfiguration, reporter.Mutator);
            AssertEquivalentPaths(d => d.Impl.S, reporter.Path);
        }

        private void AssertEquivalentConfigurations(MutatorConfiguration expectedConfiguration, MutatorConfiguration actualConfiguration)
        {
            EquivalencyAssertionOptions<MutatorConfiguration> AssertionOptions(EquivalencyAssertionOptions<MutatorConfiguration> options)
            {
                return options.Using<Expression>(x => AssertEquivalentExpressions(x.Expectation, x.Subject))
                              .WhenTypeIs<Expression>()
                              .IncludingAllRuntimeProperties();
            }

            actualConfiguration.GetType().Should().Be(expectedConfiguration.GetType());
            actualConfiguration.Should().BeEquivalentTo(expectedConfiguration, AssertionOptions);
        }

        private static void AssertEquivalentPaths<T>(Expression<Func<DestRoot, T>> expected, Expression actual)
        {
            AssertEquivalentExpressions(expected, actual);
        }

        private static void AssertEquivalentExpressions(Expression expected, Expression actual)
        {
            ExpressionEquivalenceChecker.Equivalent(expected, actual, false, true)
                                        .Should().BeTrue($"because\nExpected:\n{expected}\n\nActual:\n{actual}");
        }

        private ConverterConfigurator<SourceRoot, DestRoot> configurator;
        private ConfiguratorReporter reporter;

        private class SourceRoot
        {
            public SourceA A { get; set; }
            
            public SourceA[] As { get; set; }

            public string RootS { get; set; }
            
            public Impl Impl { get; set; }
        }

        private class DestRoot
        {
            public DestA A { get; set; }
            
            public DestA[] As { get; set; }

            public string RootS { get; set; }
            
            public Impl Impl { get; set; }
        }

        private interface Interface
        {
            string S { get; }
        }

        private class Impl : Interface
        {
            public string S { get; set; }
        }

        private class SourceA
        {
            public string S { get; set; }
            
            public SourceB[] Bs { get; set; }
        }

        private class DestA
        {
            public string S { get; set; }
            
            public DestB[] Bs { get; set; }
        }

        private class SourceB
        {
            public string S { get; set; }
        }
        
        private class DestB
        {
            public string S { get; set; }
        }
    }
}