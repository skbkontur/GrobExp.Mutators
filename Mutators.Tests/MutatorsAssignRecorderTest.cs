using System.Collections.Concurrent;
using System.Threading;

using GrobExp.Mutators;
using GrobExp.Mutators.MutatorsRecording.AssignRecording;

using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Mutators.Tests
{
    [Parallelizable(ParallelScope.None)]
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
            ClassicAssert.AreEqual(actualData.C, 12);

            ClassicAssert.IsNotEmpty(recorder.GetRecords());

            var converterNode = recorder.GetRecords()[0];
            ClassicAssert.AreEqual(1, converterNode.Records.Count);
            ClassicAssert.AreEqual("TestConverterCollection`2", converterNode.Name);

            var objectTypeNode = converterNode.Records["TestDataDest"];
            ClassicAssert.AreEqual(2, objectTypeNode.Records.Count);
            ClassicAssert.AreEqual("TestDataDest", objectTypeNode.Name);

            var dataCNode = objectTypeNode.Records["C"];
            ClassicAssert.AreEqual("C", dataCNode.Name);
            ClassicAssert.AreEqual(1, dataCNode.Records.Count);
            ClassicAssert.AreEqual("source.A", dataCNode.Records["source.A"].Name);

            var dataDNode = objectTypeNode.Records["D"];
            ClassicAssert.AreEqual("D", dataDNode.Name);
            ClassicAssert.AreEqual(1, dataDNode.Records.Count);
            ClassicAssert.AreEqual("source.B", dataDNode.Records["source.B"].Name);
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

            ClassicAssert.IsEmpty(recorder.GetRecords());
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
            ClassicAssert.AreEqual(3, converterNode.CompiledCount);
            ClassicAssert.AreEqual(4, converterNode.ExecutedCount);
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

            ClassicAssert.AreEqual(12, actualData.C);
            ClassicAssert.AreEqual(13, actualData.D);

            var converterNode = recorder.GetRecords()[0];
            ClassicAssert.AreEqual(2, converterNode.ExecutedCount);
            ClassicAssert.AreEqual(3, converterNode.CompiledCount);
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

            ClassicAssert.AreEqual(12, actualData.C);
            ClassicAssert.AreEqual(2, actualData.D);

            var converterNode = recorder.GetRecords()[0];
            ClassicAssert.AreEqual(3, converterNode.CompiledCount);
            ClassicAssert.AreEqual(1, converterNode.ExecutedCount);
            ClassicAssert.AreEqual(1, converterNode.Records["TestDataDest"].Records["C"].ExecutedCount);
            ClassicAssert.AreEqual(0, converterNode.Records["TestDataDest"].Records["D"].ExecutedCount);
        }

        [Test]
        [Description("Один лог для всех потоков")]
        public void MultithreadingTest()
        {
            var recorder = AssignRecorderInitializer.StartAssignRecorder();
            var actualDataList = new ConcurrentBag<TestDataDest>();
            var threads = new ConcurrentBag<Thread>();
            for (var i = 0; i < 10; i++)
            {
                var thread = new Thread(() =>
                    {
                        while (!start)
                        {
                        }
                        var converter = new TestConverterCollection<TestDataSource, TestDataDest>(pathFormatterCollection,
                                                                                                  configurator => { configurator.Target(x => x.C).Set(x => x.A); }).GetConverter(MutatorsContext.Empty);
                        actualDataList.Add(converter(new TestDataSource()));
                    });
                thread.Start();
                threads.Add(thread);
            }

            start = true;

            foreach (var thread in threads)
            {
                thread.Join();
            }

            ClassicAssert.AreEqual(11, recorder.GetRecords()[0].CompiledCount);
            ClassicAssert.AreEqual(10, recorder.GetRecords()[0].ExecutedCount);
            ClassicAssert.AreEqual(10, actualDataList.Count);
            foreach (var data in actualDataList)
            {
                ClassicAssert.AreEqual(12, data.C);
            }
        }

        [Test]
        public void TestDoubleGettingConverter()
        {
            var converterCollection = new TestConverterCollection<TestDataSource, TestDataDest>(pathFormatterCollection,
                                                                                                configurator =>
                                                                                                    {
                                                                                                        configurator.Target(x => x.C).Set(x => x.A);
                                                                                                        configurator.Target(x => x.D).Set(x => x.B);
                                                                                                    });
            var recorder = AssignRecorderInitializer.StartAssignRecorder();

            var converter = converterCollection.GetConverter(MutatorsContext.Empty);
            converter(new TestDataSource());
            recorder.Stop();

            var records = recorder.GetRecords();
            ClassicAssert.AreEqual(1, records.Count);
            ClassicAssert.AreEqual(3, records[0].CompiledCount);
            ClassicAssert.AreEqual(2, records[0].ExecutedCount);

            var newRecorder = AssignRecorderInitializer.StartAssignRecorder();

            ClassicAssert.AreNotSame(recorder, newRecorder);
            converter = converterCollection.GetConverter(MutatorsContext.Empty);
            converter(new TestDataSource());
            recorder.Stop();

            records = newRecorder.GetRecords();
            ClassicAssert.AreEqual(1, records.Count);
            ClassicAssert.AreEqual(2, records[0].CompiledCount);
            ClassicAssert.AreEqual(2, records[0].ExecutedCount);
        }

        [Test]
        [Description("При включенном рекордере кэш конверторов все равно используется")]
        public void TestCacheConverter()
        {
            var converterCollection = new TestConverterCollection<TestDataSource, TestDataDest>(pathFormatterCollection,
                                                                                                configurator =>
                                                                                                    {
                                                                                                        configurator.Target(x => x.C).Set(x => x.A);
                                                                                                        configurator.Target(x => x.D).Set(x => x.B);
                                                                                                    });

            var converter = converterCollection.GetConverter(MutatorsContext.Empty);
            ClassicAssert.AreSame(converter, converterCollection.GetConverter(MutatorsContext.Empty));

            var recorder = AssignRecorderInitializer.StartAssignRecorder();
            var converterWhileRecording = converterCollection.GetConverter(MutatorsContext.Empty);
            ClassicAssert.AreSame(converterWhileRecording, converter);

            recorder.Stop();
            ClassicAssert.AreSame(converter, converterCollection.GetConverter(MutatorsContext.Empty));
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
            ClassicAssert.AreEqual("TestComplexDataDest", record.Name);
            ClassicAssert.IsFalse(record.IsExcludedFromCoverage);

            records = record.Records;
            record = records["FieldC"];

            ClassicAssert.AreEqual("FieldC", record.Name);
            ClassicAssert.IsTrue(record.IsExcludedFromCoverage);
            ClassicAssert.AreEqual("A", record.Records["A"].Name);
            ClassicAssert.IsTrue(record.Records["A"].IsExcludedFromCoverage);
            ClassicAssert.AreEqual("B", record.Records["B"].Name);
            ClassicAssert.IsTrue(record.Records["B"].IsExcludedFromCoverage);

            record = records["FieldD"];
            ClassicAssert.AreEqual("FieldD", record.Name);
            ClassicAssert.IsFalse(record.IsExcludedFromCoverage);
            ClassicAssert.AreEqual("StrB", record.Records["StrB"].Name);
            ClassicAssert.IsFalse(record.Records["StrB"].IsExcludedFromCoverage);
            ClassicAssert.AreEqual("StrA", record.Records["StrA"].Name);
            ClassicAssert.IsFalse(record.Records["StrA"].IsExcludedFromCoverage);

            record = records["FieldY"];
            ClassicAssert.AreEqual("FieldY", record.Name);
            ClassicAssert.IsTrue(record.IsExcludedFromCoverage);

            record = records["IntField"];
            ClassicAssert.AreEqual("IntField", record.Name);
            ClassicAssert.IsFalse(record.IsExcludedFromCoverage);
            ClassicAssert.AreEqual("IntA", record.Records["IntA"].Name);
            ClassicAssert.IsTrue(record.Records["IntA"].IsExcludedFromCoverage);
            ClassicAssert.AreEqual("IntB", record.Records["IntB"].Name);
            ClassicAssert.IsFalse(record.Records["IntB"].IsExcludedFromCoverage);

            record = records["TestProperty"];
            ClassicAssert.AreEqual("TestProperty", record.Name);
            ClassicAssert.IsTrue(record.IsExcludedFromCoverage);
        }

        private static void DoTestSetNull<TSource, TDest>(TestConverterCollection<TSource, TDest> converterCollection, TSource source, int expectedCompiledCount, int expectedExecutedCount) where TDest : new()
        {
            var recorder = AssignRecorderInitializer.StartAssignRecorder();
            var converter = converterCollection.GetConverter(MutatorsContext.Empty);
            converter(source);
            recorder.Stop();

            var records = recorder.GetRecords();
            ClassicAssert.AreEqual(1, records.Count);
            ClassicAssert.AreEqual(expectedCompiledCount, records[0].CompiledCount);
            ClassicAssert.AreEqual(expectedExecutedCount, records[0].ExecutedCount);
        }

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