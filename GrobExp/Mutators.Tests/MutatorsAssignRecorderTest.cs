using System.Collections.Generic;
using System.Linq;

using GrobExp.Mutators;

using NUnit.Framework;

namespace Mutators.Tests
{
    public class MutatorsAssignRecorderTest : TestBase
    {
        private IPathFormatterCollection pathFormatterCollection;

        protected override void SetUp()
        {
            base.SetUp();
            pathFormatterCollection = new PathFormatterCollection();
        }

        [Test]
        public void Test()
        {
            var recorder = AssignRecorderInitializer.StartAssignRecorder();
            var testConfigurator = new TestConverterCollection<TestDataSource, TestDataDest>(pathFormatterCollection,
                configurator => configurator.Target(x => x.C).Set(x => x.A)
                );
            var converter = testConfigurator.GetConverter(MutatorsContext.Empty);

            var testDataSource = new TestDataSource();
            var actualData = converter(testDataSource);

            recorder.Stop();
            Assert.AreEqual(actualData.C, 12);
            var compiledRecords = recorder.GetCompiledRecords();
            var executedRecords = recorder.GetExecutedRecords();
            Assert.IsNotEmpty(compiledRecords);
            Assert.IsNotEmpty(executedRecords);
        }

        [Test]
        [Description("После остановки рекордера, логи не записываются")]
        public void TestStop()
        {
            var recorder = AssignRecorderInitializer.StartAssignRecorder();

            var testConfigurator = new TestConverterCollection<TestDataSource, TestDataDest>(pathFormatterCollection,
                configurator =>
                {
                    configurator.Target(x => x.C).Set(x => x.A);
                    configurator.Target(x => x.D).Set(x => x.B);
                });

            recorder.Stop();
            var converter = testConfigurator.GetConverter(MutatorsContext.Empty);
            converter(new TestDataSource());
            Assert.IsEmpty(recorder.GetExecutedRecords());
        }

        [Test]
        [Description("Повторная компиляция/исполнение строк не логируются")]
        public void TestRecordsAreDistinct()
        {
            var recorder = AssignRecorderInitializer.StartAssignRecorder();

            var testConfigurator = new TestConverterCollection<TestDataSource, TestDataDest>(pathFormatterCollection,
                configurator =>
                {
                    configurator.Target(x => x.C).Set(x => x.A);
                    configurator.Target(x => x.D).Set(x => x.B);
                });

            var converter = testConfigurator.GetConverter(MutatorsContext.Empty);
            converter(new TestDataSource());
            converter(new TestDataSource());
            recorder.Stop();
            var executedRecords = recorder.GetExecutedRecords();
            var compiledWithoutAliases = ListWithoutAliases(recorder.GetCompiledRecords());
            Assert.AreEqual(executedRecords.Distinct().ToList(), executedRecords);
            Assert.AreEqual(compiledWithoutAliases.Distinct().ToList(), compiledWithoutAliases);
            
        }

        [Test]
        [Description("Все строки конфигурации использовались")]
        public void TestCoverAll()
        {
            var recorder = AssignRecorderInitializer.StartAssignRecorder();

            var testConfigurator = new TestConverterCollection<TestDataSource, TestDataDest>(pathFormatterCollection,
                configurator =>
                {
                    configurator.Target(x => x.C).Set(x => x.A);
                    configurator.If(x => x.B == 13).Target(x => x.D).Set(x => x.B);
                });

            var converter = testConfigurator.GetConverter(MutatorsContext.Empty);
            var actualData = converter(new TestDataSource());
            recorder.Stop();

            Assert.AreEqual(12, actualData.C);
            Assert.AreEqual(13, actualData.D);

            var compiledWithoutAliases = ListWithoutAliases(recorder.GetCompiledRecords());
            var expectedRecords = new List<string> { "TestDataDest.C = source.A", "TestDataDest.D = source.B" };
            Assert.AreEqual(expectedRecords, recorder.GetExecutedRecords());
            Assert.AreEqual(expectedRecords, compiledWithoutAliases);
        }

        [Test]
        [Description("Не все строки конфигурации использовались")]
        public void TestNotCovered()
        {
            var recorder = AssignRecorderInitializer.StartAssignRecorder();

            var testConfigurator = new TestConverterCollection<TestDataSource, TestDataDest>(pathFormatterCollection,
                configurator =>
                {
                    configurator.Target(x => x.C).Set(x => x.A);
                    configurator.If(x => x.B == 10000).Target(x => x.D).Set(x => x.B);
                });

            var converter = testConfigurator.GetConverter(MutatorsContext.Empty);
            var actualData = converter(new TestDataSource());
            recorder.Stop();

            Assert.AreEqual(12, actualData.C);
            Assert.AreEqual(2, actualData.D);

            var compiledWithoutAliases = ListWithoutAliases(recorder.GetCompiledRecords());
            var executedRecords = recorder.GetExecutedRecords();
            var expectedCompiledRecords = new List<string> { "TestDataDest.C = source.A", "TestDataDest.D = source.B" };
            var expectedExecutedRecords = new List<string> { "TestDataDest.C = source.A" };
            Assert.AreEqual(expectedCompiledRecords, compiledWithoutAliases);
            Assert.AreEqual(expectedExecutedRecords, executedRecords);
        }

        private List<string> ListWithoutAliases(List<string> source)
        {
            return source.Select(x => x).Where(x => !x.StartsWith("Aliases") && x != "").ToList();
        } 
    }

    public class TestDataSource
    {
        public int A = 12;
        public int B = 13;
    }

    public class TestDataDest
    {
        public int C = 1;
        public int D = 2;
    }
}