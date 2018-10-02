using System;
using System.Collections.Concurrent;
using System.Threading;

using GrobExp.Mutators;
using GrobExp.Mutators.MutatorsRecording.AssignRecording;

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

            var objectTypeNode = converterNode.Records["TestDataDest"];
            Assert.AreEqual(2, objectTypeNode.Records.Count);
            Assert.AreEqual("TestDataDest", objectTypeNode.Name);

            var dataCNode = objectTypeNode.Records["C"];
            Assert.AreEqual("C", dataCNode.Name);
            Assert.AreEqual(1, dataCNode.Records.Count);
            Assert.AreEqual("source.A", dataCNode.Records["source.A"].Name);

            var dataDNode = objectTypeNode.Records["D"];
            Assert.AreEqual("D", dataDNode.Name);
            Assert.AreEqual(1, dataDNode.Records.Count);
            Assert.AreEqual("source.B", dataDNode.Records["source.B"].Name);
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
            // Assert.AreEqual(1, converterNode.Records[0].Records[0].ExecutedCount);
            // Assert.AreEqual(0, converterNode.Records[0].Records[1].ExecutedCount);
        }

        [Test]
        [Description("Для каждого потока отдельный лог")]
        public void MultithreadingTest()
        {
            var actualDataList = new ConcurrentBag<TestDataDest>();
            var threads = new ConcurrentBag<Thread>();
            for (var i = 0; i < 10; i++)
            {
                var thread = new Thread(() =>
                    {
                        while (!start)
                        {
                        }

                        try
                        {
                            var recorder = AssignRecorderInitializer.StartAssignRecorder();
                            var converter = new TestConverterCollection<TestDataSource, TestDataDest>(pathFormatterCollection,
                                                                                                      configurator => { configurator.Target(x => x.C).Set(x => x.A); }).GetConverter(MutatorsContext.Empty);
                            actualDataList.Add(converter(new TestDataSource()));
                            recorder.Stop();
                            Assert.AreEqual(2, recorder.GetRecords()[0].CompiledCount);
                            Assert.AreEqual(1, recorder.GetRecords()[0].ExecutedCount);
                        }
                        catch (Exception e)
                        {
                            lastException = e;
                            Console.WriteLine(e);
                        }
                    });
                thread.Start();
                threads.Add(thread);
            }

            start = true;

            foreach (var thread in threads)
            {
                thread.Join();
            }

            Assert.AreEqual(10, actualDataList.Count);
            foreach (var data in actualDataList)
            {
                Assert.AreEqual(12, data.C);
            }

            if (lastException != null)
                throw lastException;
        }

        [Test]
        [Category("Failing")]
        [Description("После повторного запуска не должны появляться записи с предыдущего. После повторного взятия конвертора все компиляции должны логироваться.")]
        public void TestDoubleGettingConverter()
        {
            var converterCollection = new TestConverterCollection<TestDataSource, TestDataDest>(pathFormatterCollection,
                                                                                                configurator =>
                                                                                                    {
                                                                                                        configurator.Target(x => x.C).Set(x => x.A);
                                                                                                        configurator.Target(x => x.D).Set(x => x.B);
                                                                                                    });
            for (var i = 0; i < 2; i++)
            {
                var recorder = AssignRecorderInitializer.StartAssignRecorder();
                var converter = converterCollection.GetConverter(MutatorsContext.Empty);
                converter(new TestDataSource());
                recorder.Stop();

                var records = recorder.GetRecords();
                Assert.AreEqual(1, records.Count);
                Assert.AreEqual(3, records[0].CompiledCount);
                Assert.AreEqual(2, records[0].ExecutedCount);
            }
        }

        [Category("Failing")]
        [Test]
        [Description("При включенном рекордере кэш конверторов не должен использоваться")]
        public void TestCacheConverter()
        {
            var converterCollection = new TestConverterCollection<TestDataSource, TestDataDest>(pathFormatterCollection,
                                                                                                configurator =>
                                                                                                    {
                                                                                                        configurator.Target(x => x.C).Set(x => x.A);
                                                                                                        configurator.Target(x => x.D).Set(x => x.B);
                                                                                                    });

            var converter = converterCollection.GetConverter(MutatorsContext.Empty);
            Assert.AreSame(converter, converterCollection.GetConverter(MutatorsContext.Empty));

            var recorder = AssignRecorderInitializer.StartAssignRecorder();
            var converterWhileRecording = converterCollection.GetConverter(MutatorsContext.Empty);
            Assert.AreNotSame(converterWhileRecording, converter);

            recorder.Stop();
            Assert.AreSame(converter, converterCollection.GetConverter(MutatorsContext.Empty));
        }

        [Test]
        [Description("Если поле заполняем значением null, не считать покрытой конвертацией")]
        public void TestSetNullToString()
        {
            var converterCollection = new TestConverterCollection<TestDataSourceNullable, TestDataDestNullable>(pathFormatterCollection,
                                                                                                                configurator =>
                                                                                                                    {
                                                                                                                        configurator.Target(x => x.StrC).Set(x => x.StrA);
                                                                                                                        configurator.Target(x => x.StrD).Set(x => x.StrB);
                                                                                                                    });
            var source = new TestDataSourceNullable
                {
                    StrA = "qxx"
                };
            DoTestSetNull(converterCollection, source, 3, 1);
        }

        [Test]
        [Description("Если поле заполняем значением null, не считать покрытой конвертацией")]
        public void TestSetNullToNullableInt()
        {
            var converterCollection = new TestConverterCollection<TestDataSourceNullableInt, TestDataDestNullableInt>(pathFormatterCollection,
                                                                                                                      configurator =>
                                                                                                                          {
                                                                                                                              configurator.Target(x => x.IntC).Set(x => x.IntA);
                                                                                                                              configurator.Target(x => x.IntD).Set(x => x.IntB);
                                                                                                                          });
            var source = new TestDataSourceNullableInt
                {
                    IntA = 12
                };
            DoTestSetNull(converterCollection, source, 3, 1);
        }

        [Test]
        [Description("Если поле заполняем значением null, не считать покрытой конвертацией")]
        public void TestSetNullToNullableEnum()
        {
            var converterCollection = new TestConverterCollection<TestDataSourceNullableEnum, TestDataDestNullableEnum>(pathFormatterCollection,
                                                                                                                        configurator =>
                                                                                                                            {
                                                                                                                                configurator.Target(x => x.FieldC).Set(x => x.FieldA);
                                                                                                                                configurator.Target(x => x.FieldD).Set(x => x.FieldB);
                                                                                                                            });
            var source = new TestDataSourceNullableEnum
                {
                    FieldA = TestEnum.Black
                };
            DoTestSetNull(converterCollection, source, 3, 1);
        }

        [Test]
        [Description("Если поле заполняем константным значением null, и это правило прописано в конвертере, считать покрытой конвертацией")]
        public void TestSetNullInConverter()
        {
            var converterCollection = new TestConverterCollection<TestDataSourceNullableInt, TestDataDestNullableInt>(pathFormatterCollection,
                                                                                                                      configurator =>
                                                                                                                          {
                                                                                                                              configurator.Target(x => x.IntC).Set(x => null);
                                                                                                                              configurator.Target(x => x.IntD).Set(x => x.IntB);
                                                                                                                          });
            var source = new TestDataSourceNullableInt
                {
                    IntA = 12,
                    IntB = 13
                };
            DoTestSetNull(converterCollection, source, 3, 2);
        }

        [Test]
        [Description("Исключение из покрытия. " +
                     "Исключаются конвертации полей заданного типа или реализующих заданный интерфейс, включая дочерние поля. " +
                     "Если поле заданного типа встретилось в конвертации в составе условия, конвертация не исключается")]
        public void TestExcludeTypesFromCoverage()
        {
            var recorder = AssignRecorderInitializer.StartAssignRecorder()
                                                    .ExcludingType<TestDataSource>()
                                                    .ExcludingInterface<ITestInterface>()
                                                    .ExcludingProperty((TestComplexDataDest x) => x.FieldY)
                                                    .ExcludingGenericProperty((IGenericTestInterface<object> x) => x.IntA);
            var converterCollection = new TestConverterCollection<TestComplexDataSource, TestComplexDataDest>(pathFormatterCollection,
                                                                                                              configurator =>
                                                                                                                  {
                                                                                                                      configurator.Target(x => x.FieldC.A).Set(x => x.FieldA.B);
                                                                                                                      configurator.Target(x => x.FieldC.B).Set(x => x.FieldA.A);
                                                                                                                      configurator.Target(x => x.FieldD.StrB).Set(x => x.FieldB.StrA);
                                                                                                                      configurator.Target(x => x.FieldD.StrA).Set(x => x.FieldB.StrB);
                                                                                                                      configurator.Target(x => x.FieldY).If(x => x.FieldA.A > 10).Set(x => x.FieldX);
                                                                                                                      configurator.Target(x => x.IntField.IntA).Set(x => x.IntField.IntA);
                                                                                                                      configurator.Target(x => x.IntField.IntB).Set(x => x.IntField.IntB);
                                                                                                                      configurator.Target(x => x.TestProperty).Set(x => x.TestProperty.S);
                                                                                                                  });
            var source = new TestComplexDataSource
                {
                    FieldA = new TestDataSource(),
                    FieldB = new TestDataSourceNullable
                        {
                            StrA = "a",
                            StrB = "b"
                        },
                    FieldX = "aba",
                    IntField = new TestDataSourceNullableInt
                        {
                            IntA = 1
                        },
                    TestProperty = new TestInterfaceImpl {S = "GRobas"}
                };
            var converter = converterCollection.GetConverter(MutatorsContext.Empty);
            converter(source);
            recorder.Stop();

            var records = recorder.GetRecords()[0].Records;
            var record = records["TestComplexDataDest"];
            Assert.AreEqual("TestComplexDataDest", record.Name);
            Assert.IsFalse(record.IsExcludedFromCoverage);

            records = record.Records;
            record = records["FieldC"];

            Assert.AreEqual("FieldC", record.Name);
            Assert.IsTrue(record.IsExcludedFromCoverage);
            Assert.AreEqual("A", record.Records["A"].Name);
            Assert.IsTrue(record.Records["A"].IsExcludedFromCoverage);
            Assert.AreEqual("B", record.Records["B"].Name);
            Assert.IsTrue(record.Records["B"].IsExcludedFromCoverage);

            record = records["FieldD"];
            Assert.AreEqual("FieldD", record.Name);
            Assert.IsFalse(record.IsExcludedFromCoverage);
            Assert.AreEqual("StrB", record.Records["StrB"].Name);
            Assert.IsFalse(record.Records["StrB"].IsExcludedFromCoverage);
            Assert.AreEqual("StrA", record.Records["StrA"].Name);
            Assert.IsFalse(record.Records["StrA"].IsExcludedFromCoverage);

            record = records["FieldY"];
            Assert.AreEqual("FieldY", record.Name);
            Assert.IsTrue(record.IsExcludedFromCoverage);

            record = records["IntField"];
            Assert.AreEqual("IntField", record.Name);
            Assert.IsFalse(record.IsExcludedFromCoverage);
            Assert.AreEqual("IntA", record.Records["IntA"].Name);
            Assert.IsTrue(record.Records["IntA"].IsExcludedFromCoverage);
            Assert.AreEqual("IntB", record.Records["IntB"].Name);
            Assert.IsFalse(record.Records["IntB"].IsExcludedFromCoverage);

            record = records["TestProperty"];
            Assert.AreEqual("TestProperty", record.Name);
            Assert.IsTrue(record.IsExcludedFromCoverage);
        }

        private static void DoTestSetNull<TSource, TDest>(TestConverterCollection<TSource, TDest> converterCollection, TSource source, int expectedCompiledCount, int expectedExecutedCount) where TDest : new()
        {
            var recorder = AssignRecorderInitializer.StartAssignRecorder();
            var converter = converterCollection.GetConverter(MutatorsContext.Empty);
            converter(source);
            recorder.Stop();

            var records = recorder.GetRecords();
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual(expectedCompiledCount, records[0].CompiledCount);
            Assert.AreEqual(expectedExecutedCount, records[0].ExecutedCount);
        }

        private volatile Exception lastException;
        private IPathFormatterCollection pathFormatterCollection;
        private volatile bool start;
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

    public class TestDataSourceNullable
    {
        public string StrA { get; set; }
        public string StrB { get; set; }
    }

    public class TestDataDestNullable
    {
        public string StrC { get; set; }
        public string StrD { get; set; }
    }

    public class TestDataSourceNullableInt : IGenericTestInterface<int>
    {
        public int? IntA { get; set; }
        public int? IntB { get; set; }
    }

    public class TestDataDestNullableInt
    {
        public int? IntC { get; set; }
        public int? IntD { get; set; }
    }

    public class TestDataSourceNullableEnum
    {
        public TestEnum? FieldA { get; set; }
        public TestEnum? FieldB { get; set; }
    }

    public class TestDataDestNullableEnum
    {
        public TestEnum? FieldC { get; set; }
        public TestEnum? FieldD { get; set; }
    }

    public enum TestEnum
    {
        Black,
        White
    }

    public class TestComplexDataSource
    {
        public TestDataSource FieldA { get; set; }
        public TestDataSourceNullable FieldB { get; set; }
        public TestDataSourceNullableInt IntField { get; set; }
        public string FieldX { get; set; }
        public ITestInterface TestProperty { get; set; }
    }

    public class TestComplexDataDest
    {
        public TestDataSource FieldC { get; set; }
        public TestDataSourceNullable FieldD { get; set; }
        public TestDataSourceNullableInt IntField { get; set; }
        public string FieldY { get; set; }
        public string TestProperty { get; set; }
    }

    public interface IGenericTestInterface<T>
    {
        int? IntA { get; }
    }

    public interface ITestInterface
    {
        string S { get; set; }
    }

    public class TestInterfaceImpl : ITestInterface
    {
        public string S { get; set; }
    }
}