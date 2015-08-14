using System.Linq;
using System.Threading.Tasks;

using GrobExp.Mutators;

using NUnit.Framework;

namespace Mutators.Tests
{
    public class MutatorsAssignRecorderTest : TestBase
    {
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
                configurator =>
                {
                    configurator.Target(x => x.C).Set(x => x.A);
                    configurator.Target(x => x.D).Set(x => x.B);
                });
            var converter = testConfigurator.GetConverter(MutatorsContext.Empty);

            var testDataSource = new TestDataSource();
            var actualData = converter(testDataSource);

            recorder.Stop();
            Assert.AreEqual(actualData.C, 12);

            Assert.IsNotEmpty(recorder.GetRecords());

            var converterNode = recorder.GetRecords()[0];
            Assert.AreEqual(1, converterNode.Records.Count);
            Assert.AreEqual("TestConverterCollection`2", converterNode.Name);

            var objectTypeNode = converterNode.Records[0];
            Assert.AreEqual(2, objectTypeNode.Records.Count);
            Assert.AreEqual("TestDataDest", objectTypeNode.Name);

            var dataCNode = objectTypeNode.Records[0];
            Assert.AreEqual("C", dataCNode.Name);
            Assert.AreEqual(1, dataCNode.Records.Count);
            Assert.AreEqual("source.A", dataCNode.Records[0].Name);

            var dataDNode = objectTypeNode.Records[1];
            Assert.AreEqual("D", dataDNode.Name);
            Assert.AreEqual(1, dataDNode.Records.Count);
            Assert.AreEqual("source.B", dataDNode.Records[0].Name);
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

            Assert.IsEmpty(recorder.GetRecords());
        }

        [Test]
        [Description("Повторная компиляция/исполнение строк не логируются заново, но считаются. Создание конвертора считается за компиляцию.")]
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

            var converterNode = recorder.GetRecords()[0];
            Assert.AreEqual(3, converterNode.CompiledCount);
            Assert.AreEqual(4, converterNode.ExecutedCount);
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

            var converterNode = recorder.GetRecords()[0];
            Assert.AreEqual(2, converterNode.ExecutedCount);
            Assert.AreEqual(3, converterNode.CompiledCount);
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

            var converterNode = recorder.GetRecords()[0];
            Assert.AreEqual(3, converterNode.CompiledCount);
            Assert.AreEqual(1, converterNode.ExecutedCount);
            Assert.AreEqual(1, converterNode.Records[0].Records[0].ExecutedCount);
            Assert.AreEqual(0, converterNode.Records[0].Records[1].ExecutedCount);
        }

        private IPathFormatterCollection pathFormatterCollection;
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