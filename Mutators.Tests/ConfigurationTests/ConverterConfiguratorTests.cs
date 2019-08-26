using System;
using System.Linq;
using System.Linq.Expressions;

using FluentAssertions;
using FluentAssertions.Equivalency;

using GrobExp.Mutators;
using GrobExp.Mutators.AutoEvaluators;
using GrobExp.Mutators.MultiLanguages;
using GrobExp.Mutators.Validators;
using GrobExp.Mutators.Visitors;

using NUnit.Framework;

namespace Mutators.Tests.ConfigurationTests
{
    [TestFixture]
    [Parallelizable(ParallelScope.Self)]
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

            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.RootS));
            AssertEquivalentConfigurations(equalsToConfiguration);
            AssertEquivalentPaths(d => d.RootS);
        }

        [Test]
        public void TestSetFromSourceAndDest()
        {
            configurator.Target(d => d.RootS).Set((s, d) => s.RootS + d.A.S);
            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, DestRoot, string>>)((s, d) => s.RootS + d.A.S));
            AssertEquivalentConfigurations(equalsToConfiguration);
            AssertEquivalentPaths(d => d.RootS);
        }

        [Test]
        public void TestPathWithEachAndCurrent()
        {
            configurator.Target(d => d.As.Each().S).Set(s => s.As.Current().S);

            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.As.Current().S));
            AssertEquivalentConfigurations(equalsToConfiguration);
            AssertEquivalentPaths(d => d.As.Each().S);
        }

        [Test]
        public void TestPathWithEachAndEach()
        {
            configurator.Target(d => d.As.Each().S).Set(s => s.As.Each().S);

            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.As.Each().S), null);
            AssertEquivalentConfigurations(equalsToConfiguration);
            AssertEquivalentPaths(d => d.As.Each().S);
        }

        [Test]
        public void TestWithCondition()
        {
            configurator.Target(d => d.RootS).If(s => s.A.S == "zzz").Set(s => s.RootS);

            var equalsToIfConfiguration = EqualsToIfConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, bool?>>)(s => s.A.S == "zzz"),
                                                                                   (Expression<Func<SourceRoot, string>>)(s => s.RootS),
                                                                                   null);
            AssertEquivalentConfigurations(equalsToIfConfiguration);
            AssertEquivalentPaths(d => d.RootS);
        }

        [Test]
        public void TestWithConditionBeforeTarget()
        {
            configurator.If(s => s.A.S == "zzz").Target(d => d.RootS).Set(s => s.RootS);

            var equalsToIfConfiguration = EqualsToIfConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, bool?>>)(s => s.A.S == "zzz"),
                                                                                   (Expression<Func<SourceRoot, string>>)(s => s.RootS),
                                                                                   null);
            AssertEquivalentConfigurations(equalsToIfConfiguration);
            AssertEquivalentPaths(d => d.RootS);
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
            AssertEquivalentConfigurations(equalsToIfConfiguration);
            AssertEquivalentPaths(d => d.RootS);
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
            AssertEquivalentConfigurations(equalsToIfConfiguration);
            AssertEquivalentPaths(d => d.RootS);
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
            AssertEquivalentConfigurations(equalsToIfConfiguration);
            AssertEquivalentPaths(d => d.As.Each().S);
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
            AssertEquivalentConfigurations(equalsToIfConfiguration);
            AssertEquivalentPaths(d => d.As.Each().S);
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
            AssertEquivalentConfigurations(equalsToIfConfiguration);
            AssertEquivalentPaths(d => d.As.Each().S);
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
            AssertEquivalentConfigurations(equalsToIfConfiguration);
            AssertEquivalentPaths(d => d.A.S);
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
            AssertEquivalentConfigurations(equalsToIfConfiguration);
            AssertEquivalentPaths(d => d.A.S);
        }

        [Test]
        public void TestWithoutCondition()
        {
            configurator.Target(d => d.RootS).If(s => s.A.S == "zzz").WithoutCondition().Set(s => s.RootS);

            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.RootS),
                                                                               null);
            AssertEquivalentConfigurations(equalsToConfiguration);
            AssertEquivalentPaths(d => d.RootS);
        }

        [Test]
        public void TestWithoutConditionBeforeTarget()
        {
            configurator.If(s => s.A.S == "zzz").WithoutCondition().Target(d => d.RootS).Set(s => s.RootS);

            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.RootS),
                                                                               null);
            AssertEquivalentConfigurations(equalsToConfiguration);
            AssertEquivalentPaths(d => d.RootS);
        }

        [Test]
        public void TestGotoInDest()
        {
            var subConfigurator = configurator.GoTo(d => d.As.Each());
            subConfigurator.Target(d => d.S).Set(s => s.RootS);

            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.RootS),
                                                                               null);
            AssertEquivalentConfigurations(equalsToConfiguration);
            AssertEquivalentPaths(d => d.As.Each().S);
        }

        [Test]
        public void TestGotoInDestTwice()
        {
            var subConfigurator = configurator.GoTo(d => d.As.Each()).GoTo(d => d.S);
            subConfigurator.Target(d => d).Set(s => s.RootS);

            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.RootS),
                                                                               null);
            AssertEquivalentConfigurations(equalsToConfiguration);
            AssertEquivalentPaths(d => d.As.Each().S);
        }

        [Test]
        public void TestGotoInDestAndSource()
        {
            var subConfigurator = configurator.GoTo(d => d.As.Each(), s => s.As.Current());
            subConfigurator.Target(d => d.S).Set(s => s.S);

            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.As.Current().S),
                                                                               null);
            AssertEquivalentConfigurations(equalsToConfiguration);
            AssertEquivalentPaths(d => d.As.Each().S);
        }

        [Test]
        public void TestGotoWithCondition()
        {
            var subConfigurator = configurator.GoTo(d => d.As.Each(), s => s.As.Current());
            subConfigurator.Target(d => d.S).If(s => s.S.Length % 2 == 0).Set(s => s.S);

            var equalsToConfiguration = EqualsToIfConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, bool?>>)(s => s.As.Current().S.Length % 2 == 0),
                                                                                 (Expression<Func<SourceRoot, string>>)(s => s.As.Current().S),
                                                                                 null);
            AssertEquivalentConfigurations(equalsToConfiguration);
            AssertEquivalentPaths(d => d.As.Each().S);
        }

        [Test]
        public void TestReplaceEachToCurrentOnlyInGotoPath()
        {
            var subConfigurator = configurator.GoTo(d => d.As.Each(), s => s.As.Each());
            subConfigurator.Target(d => d.Bs.Each().S).If((s, d) => s.S.Length % 2 == 0 && d.S.Length % 2 == 1).Set(s => s.Bs.Each().S);

            var equalsToConfiguration = EqualsToIfConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, DestRoot, bool?>>)((s, d) => s.As.Current().S.Length % 2 == 0 && d.As.Current().S.Length % 2 == 1),
                                                                                 (Expression<Func<SourceRoot, string>>)(s => s.As.Current().Bs.Each().S),
                                                                                 null);
            AssertEquivalentConfigurations(equalsToConfiguration);
            AssertEquivalentPaths(d => d.As.Each().Bs.Each().S);
        }

        [Test]
        public void TestResolvingInterfaceMembersInTargetPath()
        {
            configurator.Target(d => ((Interface)d.Impl).S).Set(s => s.RootS);

            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.RootS),
                                                                               null);
            AssertEquivalentConfigurations(equalsToConfiguration);
            AssertEquivalentPaths(d => d.Impl.S);
        }

        [Test]
        public void TestSetWithValidator()
        {
            configurator.Target(d => d.RootS).Set(s => s.RootS, x => x, x => ValidationResult.Ok);
            var sourceValidator = StaticValidatorConfiguration.Create<SourceRoot>(MutatorsCreator.Sharp, "SetWithValidator", 0, null,
                                                                                  (Expression<Func<SourceRoot, string>>)(x => x.RootS),
                                                                                  (Expression<Func<SourceRoot, string>>)(x => x.RootS),
                                                                                  (Expression<Func<string, ValidationResult>>)(x => ValidationResult.Ok));
            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.RootS), sourceValidator);
            AssertEquivalentConfigurations(equalsToConfiguration);
            AssertEquivalentPaths(d => d.RootS);
        }

        [Test]
        public void TestSetWithValueAndConverter()
        {
            configurator.Target(d => d.RootS).Set(s => s.RootS, x => x + "def");
            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.RootS + "def"));
            AssertEquivalentConfigurations(equalsToConfiguration);
            AssertEquivalentPaths(d => d.RootS);
        }

        [Test]
        public void TestSetWithValueAndValidatorAndConverter()
        {
            var validationResult = ValidationResult.Warning(new TestText {Text = "test text"});
            configurator.Target(d => d.RootS).Set(s => s.RootS, x => x + "abc", x => x + "def", x => validationResult);
            var sourceValidator = StaticValidatorConfiguration.Create<SourceRoot>(MutatorsCreator.Sharp, "SetWithValidator", 0, null,
                                                                                  (Expression<Func<SourceRoot, string>>)(x => x.RootS),
                                                                                  (Expression<Func<SourceRoot, string>>)(x => x.RootS + "abc"),
                                                                                  (Expression<Func<string, ValidationResult>>)(x => validationResult));
            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.RootS + "abc" + "def"), sourceValidator);
            AssertEquivalentConfigurations(equalsToConfiguration);
            AssertEquivalentPaths(d => d.RootS);
        }

        [Test]
        public void TestNullifyIf()
        {
            configurator.Target(d => d.RootS).NullifyIf(d => d.A.S == "null");
            var nullifyIfConfiguration = NullifyIfConfiguration.Create<DestRoot>(null, d => d.A.S == "null");
            AssertEquivalentConfigurations(nullifyIfConfiguration);
            AssertEquivalentPaths(d => d.RootS);
        }

        [Test]
        public void TestSetConstant()
        {
            configurator.Target(d => d.RootS).Set("zzz");
            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => "zzz"));
            AssertEquivalentConfigurations(equalsToConfiguration);
            AssertEquivalentPaths(d => d.RootS);
        }

        [Test]
        public void TestSetWithMessage()
        {
            configurator.Target(d => d.RootS).Set(s => s.RootS, x => x + "def", x => x.Length == 5, s => new TestText {Text = "text!"}, 0, ValidationResultType.Warning);
            var sourceValidator = StaticValidatorConfiguration.Create<SourceRoot>(MutatorsCreator.Sharp, "SetWithValidator", 0, null,
                                                                                  (Expression<Func<SourceRoot, string>>)(x => x.RootS),
                                                                                  (Expression<Func<SourceRoot, string>>)(x => x.RootS),
                                                                                  (Expression<Func<string, ValidationResult>>)(x => (bool?)(x.Length == 5) == true
                                                                                                                                        ? new ValidationResult(ValidationResultType.Warning, new TestText {Text = "text!"})
                                                                                                                                        : ValidationResult.Ok));
            var equalsToConfiguration = EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.RootS + "def"), sourceValidator);
            AssertEquivalentConfigurations(equalsToConfiguration);
            AssertEquivalentPaths(d => d.RootS);
        }

        [Test]
        public void TestBatchSet()
        {
            configurator.GoTo(d => d.A).BatchSet((d, s) => new Batch
                {
                    {d.S.NotNull(), s.RootS},
                    {d.Bs[0].S, s.As[0].S},
                    {d.Bs[1].S.NotNull(), s.As[1].S},
                    {d.Bs.Each().S, s.As.Each().S}
                });

            AssertEquivalentConfigurations(
                EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.RootS)),
                EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.As[0].S)),
                EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.As[1].S)),
                EqualsToConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, string>>)(s => s.As.Current().S)),
                NullifyIfConfiguration.Create<DestRoot>(null, (d => d.A.S == null && d.A.Bs[1].S == null)),
                NullifyIfConfiguration.Create<DestRoot>(null, (d => d.A.S == null && d.A.Bs[1].S == null))
                );
            AssertEquivalentPathBodies(d => d.A.S, d => d.A.Bs[0].S, d => d.A.Bs[1].S, d => d.A.Bs.Each().S, d => d.A.Bs[0].S, d => d.A.Bs.Each().S);
        }
        
        [Test]
        public void TestBatchSetWithCondition()
        {
            configurator.If(s => s.RootS == "zzz").GoTo(d => d.A).BatchSet((d, s) => new Batch{{d.S, s.RootS}});

            AssertEquivalentConfigurations(
                EqualsToIfConfiguration.Create<DestRoot>((Expression<Func<SourceRoot, bool?>>)(s => s.RootS == "zzz"), 
                                                         (Expression<Func<SourceRoot, string>>)(s => s.RootS), 
                                                         null));
            AssertEquivalentPathBodies(d => d.A.S);
        }

        private void AssertEquivalentConfigurations(params MutatorConfiguration[] expectedConfiguration)
        {
            reporter.Reports.Select(x => x.Mutator).Should().BeEquivalentTo(expectedConfiguration, AssertionOptions);
        }

        private void AssertEquivalentPaths<T>(Expression<Func<DestRoot, T>> expected)
        {
            reporter.Reports.Should().ContainSingle().Which.Path.Should().BeEquivalentTo(expected, AssertionOptions);
        }

        private void AssertEquivalentPathBodies<T>(params Expression<Func<DestRoot, T>>[] expected)
        {
            reporter.Reports.Select(x => x.Path).Should().BeEquivalentTo(expected.Select(x => x.Body), AssertionOptions);
        }

        EquivalencyAssertionOptions<T> AssertionOptions<T>(EquivalencyAssertionOptions<T> options)
        {
            return options.WithStrictOrdering()
                          .Using<Expression>(x => AssertEquivalentExpressions(x.Expectation, x.Subject))
                          .WhenTypeIs<Expression>()
                          .IncludingAllRuntimeProperties();
        }

        private static void AssertEquivalentExpressions(Expression expected, Expression actual)
        {
            ExpressionEquivalenceChecker.Equivalent(expected, actual, false, true)
                                        .Should().BeTrue($"because\nExpected:\n{expected}\n\nActual:\n{actual}");
        }

        private interface Interface
        {
            string S { get; }
        }

        private ConverterConfigurator<SourceRoot, DestRoot> configurator;
        private ConfiguratorReporter reporter;

        [MultiLanguageTextType("TestText")]
        private class TestText : MultiLanguageTextBase
        {
            protected override void Register()
            {
                Register("RU", () => Text);
                Register("RU", Web, () => Text);
            }

            public string Text { get; set; }
        }

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