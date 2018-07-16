using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;

using GrobExp.Mutators;

using NUnit.Framework;

namespace Mutators.Tests
{
    public class TestConvertersAssemblyEditor : ConvertersAssemblyEditor
    {
        public void MutatorExistanceTest(bool forSeveralContexts)
        {
            var assembly = Assembly.LoadFrom($"{AppDomain.CurrentDomain.BaseDirectory}Converters.dll");
            var mutatorType = assembly.GetType(typeof(TestConverterCollection<TestDataSource, TestDataDest>).Name);

            Assert.IsNotNull(mutatorType);
            Assert.IsNotNull(mutatorType.GetMethod("someGoodKey"));
            if(forSeveralContexts)
                Assert.IsNotNull(mutatorType.GetMethod("someGoodKey1"));
        }

        public void DllCreationTest(bool forSeveralContexts)
        {
            var pathFormatterCollection = new PathFormatterCollection();
            var testConfigurator = new TestConverterCollection<TestDataSource, TestDataDest>(pathFormatterCollection,
                configurator =>
                {
                    configurator.Target(x => x.C).Set(x => x.A);
                    configurator.Target(x => x.D).Set(x => x.B);
                });
            var contexts = new List<ValidatorsTest.TestMutatorsContext>();
            var context = new ValidatorsTest.TestMutatorsContext() { Key = "someGoodKey" };
            contexts.Add(context);
            if(forSeveralContexts)
                contexts.Add(new ValidatorsTest.TestMutatorsContext() { Key = "someGoodKey1" });

            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var assemblyName = "Converters";
            var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Save);
            var mb = ab.DefineDynamicModule(assemblyName, assemblyName + ".dll");
            var tb = mb.DefineType(testConfigurator.GetType().Name, TypeAttributes.Public | TypeAttributes.Class);

            AddMutator(testConfigurator, contexts, tb);
            tb.CreateType();

            ab.Save("Converters.dll");

            testConfigurator.LoadConvertersAssembly($"{AppDomain.CurrentDomain.BaseDirectory}Converters.dll");

            foreach(var c in contexts)
            {
                var converter = testConfigurator.GetConverter(c);

                var testDataSource = new TestDataSource();
                var actualData = converter(testDataSource);

                Assert.AreEqual(12, actualData.C);
                Assert.AreEqual(13, actualData.D);
                Assert.IsTrue(File.Exists("Converters.dll"));
            }

        }

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

        public enum MyEnum
        {
            Mine,
            Mine1,
            Mine2
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

        public void IntContextTest()
        {
            var pathFormatterCollection = new PathFormatterCollection();
            var testConfigurator = new TestConverterCollection<TestDataSource, TestDataDest>(pathFormatterCollection,
                configurator =>
                {
                    configurator.Target(x => x.C).Set(x => x.A);
                    configurator.Target(x => x.D).Set(x => x.B);
                });
            var context = new IntContext() { Key = "someGoodKey" };

            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var assemblyName = "Converters";
            var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Save);
            var mb = ab.DefineDynamicModule(assemblyName, assemblyName + ".dll");
            var tb = mb.DefineType(testConfigurator.GetType().Name, TypeAttributes.Public | TypeAttributes.Class);

            Assert.DoesNotThrow(() => testConfigurator.AddConverterWithContext(tb, context));
        }

        public void StringContextTest()
        {
            var pathFormatterCollection = new PathFormatterCollection();
            var testConfigurator = new TestConverterCollection<TestDataSource, TestDataDest>(pathFormatterCollection,
                configurator =>
                {
                    configurator.Target(x => x.C).Set(x => x.A);
                    configurator.Target(x => x.D).Set(x => x.B);
                });
            var context = new StringContext() { Key = "someGoodKey" };

            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var assemblyName = "Converters";
            var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Save);
            var mb = ab.DefineDynamicModule(assemblyName, assemblyName + ".dll");
            var tb = mb.DefineType(testConfigurator.GetType().Name, TypeAttributes.Public | TypeAttributes.Class);

            Assert.DoesNotThrow(() => testConfigurator.AddConverterWithContext(tb, context));
        }

        public void EnumContextTest()
        {
            var pathFormatterCollection = new PathFormatterCollection();
            var testConfigurator = new TestConverterCollection<TestDataSource, TestDataDest>(pathFormatterCollection,
                configurator =>
                {
                    configurator.Target(x => x.C).Set(x => x.A);
                    configurator.Target(x => x.D).Set(x => x.B);
                });
            var context = new EnumContext() { Key = "someGoodKey" };

            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var assemblyName = "Converters";
            var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Save);
            var mb = ab.DefineDynamicModule(assemblyName, assemblyName + ".dll");
            var tb = mb.DefineType(testConfigurator.GetType().Name, TypeAttributes.Public | TypeAttributes.Class);

            Assert.DoesNotThrow(() => testConfigurator.AddConverterWithContext(tb, context));
        }

        public void ClassContextTest()
        {
            var pathFormatterCollection = new PathFormatterCollection();
            var testConfigurator = new TestConverterCollection<TestDataSource, TestDataDest>(pathFormatterCollection,
                configurator =>
                {
                    configurator.Target(x => x.C).Set(x => ZzzConverter.Convert(x.A));
                    configurator.Target(x => x.D).Set(x => x.B);
                });
            var context = new ClassContext() { Key = "someGoodKey" };

            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var assemblyName = "Converters";
            var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Save);
            var mb = ab.DefineDynamicModule(assemblyName, assemblyName + ".dll");
            var tb = mb.DefineType(testConfigurator.GetType().Name, TypeAttributes.Public | TypeAttributes.Class);

            Assert.DoesNotThrow(() => testConfigurator.AddConverterWithContext(tb, context));
            var w = testConfigurator.GetConverter(context)(new TestDataSource());
        }

        private void AddMutator(TestConverterCollection<TestDataSource, TestDataDest> testConfigurator, List<ValidatorsTest.TestMutatorsContext> contexts, TypeBuilder tb)
        {
            foreach (var c in contexts)
                testConfigurator.AddConverterWithContext(tb, c);
        }

        public static class ZzzConverter
        {
            public static MyClass W {  get;  } = new MyClass();
        

            public static T Convert<T>(T s)
            {
                var q = W;
                var z = W;
                if(!ReferenceEquals(q, z))
                    throw new Exception();
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

        public TestConvertersAssemblyEditor()
            : base()
        {
        }
    }
}