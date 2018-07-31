using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

using GrobExp.Mutators;

using NUnit.Framework;

namespace Mutators.Tests
{
    public class TestConvertersAssemblyBuilderWorker : MarshalByRefObject
    {
        public void DllCreationTest(bool forSeveralContexts)
        {
            var contexts = new List<ValidatorsTest.TestMutatorsContext>();
            var context = new ValidatorsTest.TestMutatorsContext() {Key = "someGoodKey"};
            contexts.Add(context);
            if (forSeveralContexts)
                contexts.Add(new ValidatorsTest.TestMutatorsContext() {Key = "someGoodKey1"});

            var tb = assemblyBuilderWorker.CreateConverterClass(defaultTestConfigurator.GetType());

            AddConverters(defaultTestConfigurator, contexts, tb);

            assemblyBuilderWorker.SaveAssembly();

            Assert.IsTrue(File.Exists($"{Environment.CurrentDirectory}\\Converters.dll"));
            var convertersAssembly = Assembly.LoadFrom($"{Environment.CurrentDirectory}\\Converters.dll");

            foreach (var c in contexts)
            {
                var converterType = convertersAssembly.GetType(ConvertersAssemblyBuilderWorker.CreateConverterTypeName(defaultTestConfigurator.GetType()));
                Assert.NotNull(converterType);
                var converter = converterType.GetMethod(c.GetKey());
                Assert.NotNull(converter);

                var testDataSource = new TestDataSource();
                var actualData = new TestDataDest();
                converter.Invoke(null, new object[] {actualData, testDataSource });

                Assert.AreEqual(12, actualData.C);
                Assert.AreEqual(13, actualData.D);
            }
        }

        public enum MyEnum
        {
            Mine,
            Mine1,
            Mine2
        }

        public void IntContextTest()
        {
            var context = new IntContext() {Key = "someGoodKey"};

            var tb = assemblyBuilderWorker.CreateConverterClass(defaultTestConfigurator.GetType());

            Assert.DoesNotThrow(() => defaultTestConfigurator.AddConverterWithContext(tb, context));
        }

        public void StringContextTest()
        {
            var context = new StringContext() {Key = "someGoodKey"};

            var tb = assemblyBuilderWorker.CreateConverterClass(defaultTestConfigurator.GetType());

            Assert.DoesNotThrow(() => defaultTestConfigurator.AddConverterWithContext(tb, context));
        }

        public void EnumContextTest()
        {
            var context = new EnumContext() {Key = "someGoodKey"};

            var tb = assemblyBuilderWorker.CreateConverterClass(defaultTestConfigurator.GetType());

            Assert.DoesNotThrow(() => defaultTestConfigurator.AddConverterWithContext(tb, context));
        }

        public void ClassContextTest()
        {
            var context = new ClassContext() { Key = "someGoodKey" };


            var tb = assemblyBuilderWorker.CreateConverterClass(defaultTestConfigurator.GetType());

            Assert.DoesNotThrow(() => defaultTestConfigurator.AddConverterWithContext(tb, context));
        }

        public void InstanceConfigurationTest()
        {
            var w = new MyClass();
            var testConfigurator = new TestConverterCollection<TestDataSource, TestDataDest>(new PathFormatterCollection(),
                                                                                             configurator =>
                                                                                                 {
                                                                                                     configurator.Target(x => x.C).Set(x => w.Convert(x.A));
                                                                                                     configurator.Target(x => x.D).Set(x => x.B);
                                                                                                 });
            var context = new ClassContext() {Key = "someGoodKey"};


            var tb = assemblyBuilderWorker.CreateConverterClass(testConfigurator.GetType());

            Assert.Throws<InvalidOperationException>(() => testConfigurator.AddConverterWithContext(tb, context));
        }

        public void InstanceConfigurationFixTest()
        {
            var w = new MyClass();
            var testConfigurator = new TestConverterCollection<TestDataSource, TestDataDest>(new PathFormatterCollection(),
                                                                                             configurator =>
                                                                                                 {
                                                                                                     configurator.Target(x => x.C).Set(x => ZzzConverter.Convert(x.A));
                                                                                                     configurator.Target(x => x.D).Set(x => x.B);
                                                                                                 });
            var context = new ClassContext() { Key = "someGoodKey" };


            var tb = assemblyBuilderWorker.CreateConverterClass(testConfigurator.GetType());

            Assert.DoesNotThrow(() => testConfigurator.AddConverterWithContext(tb, context));
        }

        private void AddConverters(TestConverterCollection<TestDataSource, TestDataDest> testConfigurator, List<ValidatorsTest.TestMutatorsContext> contexts, TypeBuilder tb)
        {
            foreach (var c in contexts)
                testConfigurator.AddConverterWithContext(tb, c);
        }

        private readonly ConvertersAssemblyBuilderWorker assemblyBuilderWorker = new ConvertersAssemblyBuilderWorker();

        private TestConverterCollection<TestDataSource, TestDataDest> defaultTestConfigurator = new TestConverterCollection<TestDataSource, TestDataDest>(new PathFormatterCollection(),
                                                                                                                                                          configurator =>
                                                                                                                                                           {
                                                                                                                                                               configurator.Target(x => x.C).Set(x => x.A);
                                                                                                                                                               configurator.Target(x => x.D).Set(x => x.B);
                                                                                                                                                           });

        public class IntContext : ValidatorsTest.TestMutatorsContext
        {
            public int a { get; set; }

            public override string GetKey()
            {
                return Key;
            }
        }

        public class StringContext : ValidatorsTest.TestMutatorsContext
        {
            public string a { get; set; }

            public override string GetKey()
            {
                return Key;
            }
        }

        public class EnumContext : ValidatorsTest.TestMutatorsContext
        {
            public MyEnum NotYours { get; set; }

            public override string GetKey()
            {
                return Key;
            }
        }

        public class ClassContext : ValidatorsTest.TestMutatorsContext
        {
            public EnumContext a { get; set; }

            public override string GetKey()
            {
                return Key;
            }
        }

        public static class ZzzConverter
        {
            public static MyClass W { get; } = new MyClass();

            public static T Convert<T>(T s)
            {
                return W.Convert(s);
            }
        }

        public class MyClass
        {
            public T Convert<T>(T s)
            {
                return s;
            }
        }
    }
}