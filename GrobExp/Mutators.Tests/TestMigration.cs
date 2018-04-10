using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators;
using GrobExp.Mutators.CustomFields;
using GrobExp.Mutators.MultiLanguages;
using GrobExp.Mutators.Validators;

using NUnit.Framework;

namespace Mutators.Tests
{
    public class TestMigration : TestBase
    {
        [Test]
        public void TestProperty()
        {
            var configuratorCollection = new TestDataConfiguratorCollection<SourceTestData>(null, null, pathFormatterCollection, configurator =>
                {
                    var subConfigurator = configurator.GoTo(x => x.As.Each());
                    subConfigurator.Target(x => x.Bs).Required(x => new TestText
                        {
                            Text = x.Info
                        });
                });
            var converterCollection = new TestConverterCollection<TargetTestData, SourceTestData>(pathFormatterCollection, configurator =>
                {
                    var subConfigurator = configurator.GoTo(x => x.As.Each(), x => x.As.Where(y => y.IsRemoved != true).Current());
                    subConfigurator.Target(x => x.Bs.Each().Value).Set(x => x.Bs.Current());
                    subConfigurator.Target(x => x.Info).Set(x => x.Info);
                });
            var mutatorsTree = configuratorCollection.GetMutatorsTree(MutatorsContext.Empty);
            var migratedTree = converterCollection.Migrate(mutatorsTree, MutatorsContext.Empty);

            var mutatorWithPath = migratedTree.GetAllMutatorsWithPaths().Single();
            ((RequiredIfConfiguration)mutatorWithPath.Mutator).Condition.Compile();
            ((RequiredIfConfiguration)mutatorWithPath.Mutator).Message.Compile();
        }

        [Category("Failing")]
        [Test(Description = "Test removing filters (like Where) on path to enum CustomField")]
        public void TestEnumCustomFieldWithWhereAndStringConverter()
        {
            var configuratorCollection = new TestDataConfiguratorCollection<SourceTestData>(null, null, pathFormatterCollection, configurator =>
                {
                    var subConfigurator = configurator.GoTo(x => x.As.Each());
                    subConfigurator.Target(x => x.EnumCustomField).Required(x => new TestText
                        {
                            Text = x.Info
                        });
                });
            var converterCollection = new TestConverterCollection<TargetTestData, SourceTestData>(pathFormatterCollection, configurator =>
                {
                    var subConfigurator = configurator.GoTo(x => x.As.Each(), x => x.As.Where(y => y.IsRemoved != true).Current());
                    subConfigurator.Target(x => x.Info).Set(x => x.Info);
                }, new EnumStringConverter());
            var mutatorsTree = configuratorCollection.GetMutatorsTree(MutatorsContext.Empty);
            var migratedTree = converterCollection.Migrate(mutatorsTree, MutatorsContext.Empty);

            var mutatorWithPath = migratedTree.GetAllMutatorsWithPathsForWeb(x => x).Single();

            Expression<Func<TargetTestData, AEnum>> expectedPathToMutator = x => (AEnum)(x.As.Each().CustomFields["EnumCustomField"].Value ?? AEnum.Unknown);
            mutatorWithPath.PathToMutator.AssertEquivalentExpressions(expectedPathToMutator.Body.Simplify(), strictly : false, distinguishEachAndCurrent : true);
            Expression<Func<TargetTestData, object>> expectedPathToNode = x => x.As.Each().CustomFields["EnumCustomField"].Value;
            mutatorWithPath.PathToNode.AssertEquivalentExpressions(expectedPathToNode.Body, strictly : false, distinguishEachAndCurrent : true);

            var requiredIfConfiguration = (RequiredIfConfiguration)mutatorWithPath.Mutator;
            Expression<Func<TargetTestData, AEnum>> expectedMutatorPath = x => (AEnum)(x.As.Current().CustomFields["EnumCustomField"].Value ?? AEnum.Unknown);
            requiredIfConfiguration.Path.AssertEquivalentExpressions(expectedMutatorPath.Simplify(), strictly : false, distinguishEachAndCurrent : true);
            Expression<Func<TargetTestData, bool>> expectedMutatorCondition = x => x.As.Each().IsRemoved != true;
            requiredIfConfiguration.Condition.AssertEquivalentExpressions(expectedMutatorCondition, strictly : false, distinguishEachAndCurrent : true);
            Expression<Func<TargetTestData, TestText>> expectedMutatorMessage = x => new TestText {Text = x.As.Each().Info};
            requiredIfConfiguration.Message.AssertEquivalentExpressions(expectedMutatorMessage, strictly : false, distinguishEachAndCurrent : true);
        }

        [Test]
        public void TestNumericCustomFieldWithWhere()
        {
            var configuratorCollection = new TestDataConfiguratorCollection<SourceTestData>(null, null, pathFormatterCollection, configurator =>
                {
                    var subConfigurator = configurator.GoTo(x => x.As.Each());
                    subConfigurator.Target(x => x.CustomPrimitiveField).Required(x => new TestText
                        {
                            Text = x.Info
                        });
                });
            var converterCollection = new TestConverterCollection<TargetTestData, SourceTestData>(pathFormatterCollection, configurator =>
                {
                    var subConfigurator = configurator.GoTo(x => x.As.Each(), x => x.As.Where(y => y.IsRemoved != true).Current());
                    subConfigurator.Target(x => x.Info).Set(x => x.Info);
                });
            var mutatorsTree = configuratorCollection.GetMutatorsTree(MutatorsContext.Empty);
            var migratedTree = converterCollection.Migrate(mutatorsTree, MutatorsContext.Empty);

            var mutatorWithPath = migratedTree.GetAllMutatorsWithPathsForWeb(x => x).Single();

            Expression<Func<TargetTestData, int>> expectedPathToMutator = x => (int)(x.As.Each().CustomFields["CustomPrimitiveField"].Value ?? 0);
            mutatorWithPath.PathToMutator.AssertEquivalentExpressions(expectedPathToMutator.Body.Simplify(), strictly : false, distinguishEachAndCurrent : true);
            Expression<Func<TargetTestData, object>> expectedPathToNode = x => x.As.Each().CustomFields["CustomPrimitiveField"].Value;
            mutatorWithPath.PathToNode.AssertEquivalentExpressions(expectedPathToNode.Body, strictly : false, distinguishEachAndCurrent : true);

            var requiredIfConfiguration = (RequiredIfConfiguration)mutatorWithPath.Mutator;
            Expression<Func<TargetTestData, int>> expectedMutatorPath = x => (int)(x.As.Current().CustomFields["CustomPrimitiveField"].Value ?? 0);
            requiredIfConfiguration.Path.AssertEquivalentExpressions(expectedMutatorPath.Simplify(), strictly : false, distinguishEachAndCurrent : true);
            Expression<Func<TargetTestData, bool>> expectedMutatorCondition = x => x.As.Each().IsRemoved != true;
            requiredIfConfiguration.Condition.AssertEquivalentExpressions(expectedMutatorCondition, strictly : false, distinguishEachAndCurrent : true);
            Expression<Func<TargetTestData, TestText>> expectedMutatorMessage = x => new TestText {Text = x.As.Each().Info};
            requiredIfConfiguration.Message.AssertEquivalentExpressions(expectedMutatorMessage, strictly : false, distinguishEachAndCurrent : true);
        }

        [Test]
        public void TestNormalFieldsWithWhere()
        {
            var configuratorCollection = new TestDataConfiguratorCollection<SourceTestData>(null, null, pathFormatterCollection, configurator =>
                {
                    var subConfigurator = configurator.GoTo(x => x.As.Each());
                    subConfigurator.Target(x => x.SomethingNormal).Required(x => new TestText
                        {
                            Text = x.Info
                        });
                });
            var converterCollection = new TestConverterCollection<TargetTestData, SourceTestData>(pathFormatterCollection, configurator =>
                {
                    var subConfigurator = configurator.GoTo(x => x.As.Each(), x => x.As.Where(y => y.IsRemoved != true).Current());
                    subConfigurator.Target(x => x.Info).Set(x => x.Info);
                    subConfigurator.Target(x => x.SomethingNormal).Set(x => x.SomethingNormal);
                });
            var mutatorsTree = configuratorCollection.GetMutatorsTree(MutatorsContext.Empty);
            var migratedTree = converterCollection.Migrate(mutatorsTree, MutatorsContext.Empty);

            var mutatorWithPath = migratedTree.GetAllMutatorsWithPathsForWeb(x => x).Single();

            Expression<Func<TargetTestData, string>> expectedPathToMutator = x => x.As.Current().SomethingNormal;
            mutatorWithPath.PathToMutator.AssertEquivalentExpressions(expectedPathToMutator.Body, strictly : false, distinguishEachAndCurrent : true);
            Expression<Func<TargetTestData, object>> expectedPathToNode = x => x.As.Each().SomethingNormal;
            mutatorWithPath.PathToNode.AssertEquivalentExpressions(expectedPathToNode.Body, strictly : false, distinguishEachAndCurrent : true);

            var requiredIfConfiguration = (RequiredIfConfiguration)mutatorWithPath.Mutator;
            Expression<Func<TargetTestData, string>> expectedMutatorPath = x => x.As.Current().SomethingNormal;
            requiredIfConfiguration.Path.AssertEquivalentExpressions(expectedMutatorPath, strictly : false, distinguishEachAndCurrent : true);
            Expression<Func<TargetTestData, bool>> expectedMutatorCondition = x => x.As.Current().IsRemoved != true;
            requiredIfConfiguration.Condition.AssertEquivalentExpressions(expectedMutatorCondition, strictly : false, distinguishEachAndCurrent : true);
            Expression<Func<TargetTestData, TestText>> expectedMutatorMessage = x => new TestText {Text = x.As.Current().Info};
            requiredIfConfiguration.Message.AssertEquivalentExpressions(expectedMutatorMessage, strictly : false, distinguishEachAndCurrent : true);
        }

        protected override void SetUp()
        {
            base.SetUp();
            pathFormatterCollection = new PathFormatterCollection();
        }

        private PathFormatterCollection pathFormatterCollection;
    }

    public class EnumStringConverter : StringConverterBase
    {
        public override bool CanConvert(Type type)
        {
            return type.IsEnum;
        }

        public override object ConvertFromString(string value, Type type)
        {
            if (type.IsEnum)
            {
                return Enum.Parse(type, value);
            }

            throw new NotSupportedException("Convert to type '" + type + "' is not supported");
        }

        public override string ConvertToString(object value, Type type)
        {
            return type.IsEnum ? value.ToString() : null;
        }
    }

    [MultiLanguageTextType("TestText")]
    public class TestText : MultiLanguageTextBase
    {
        protected override void Register()
        {
            Register("RU", () => Text);
            Register("RU", Web, () => Text);
        }

        public string Text { get; set; }
    }

    public class SourceTestData
    {
        public SourceA[] As { get; set; }
    }

    public class SourceA
    {
        public SourceB[] Bs { get; set; }
        public string Info { get; set; }
        public string SomethingNormal { get; set; }

        [CustomField]
        public int CustomPrimitiveField { get; set; }

        [CustomField]
        public AEnum EnumCustomField { get; set; }
    }

    public enum AEnum
    {
        Unknown,
        Abc,
        Cde
    }

    public class SourceB
    {
        public string Value { get; set; }
    }

    public class TargetTestData
    {
        public TargetA[] As { get; set; }
    }

    public class TargetA
    {
        public string[] Bs { get; set; }
        public string Info { get; set; }
        public bool IsRemoved { get; set; }
        public string SomethingNormal { get; set; }

        [CustomFieldsContainer]
        public Dictionary<string, CustomFieldValue> CustomFields { get; set; }
    }
}