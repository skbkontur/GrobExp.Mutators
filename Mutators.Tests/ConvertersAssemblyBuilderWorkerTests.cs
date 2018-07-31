using System;
using System.IO;

using GrobExp.Compiler;
using GrobExp.Mutators;

using NUnit.Framework;

namespace Mutators.Tests
{
    [TestFixture]
    public class ConvertersAssemblyBuilderWorkerTests
    {
        private AppDomain appDomain;
        private TestConvertersAssemblyBuilderWorker assemblyBuilderWorker;

        [SetUp]
        public void Setup()
        {
            var appDomainSetup = new AppDomainSetup
                {
                    ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                    DisallowCodeDownload = true,
                    DisallowBindingRedirects = false,
                    ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
                };
            appDomain = AppDomain.CreateDomain("TestDomain", null, appDomainSetup);
            var assemblyEditorType = typeof(TestConvertersAssemblyBuilderWorker);
            assemblyBuilderWorker = (TestConvertersAssemblyBuilderWorker)appDomain.CreateInstanceFromAndUnwrap(assemblyEditorType.Assembly.Location, assemblyEditorType.FullName);
        }

        [TearDown]
        public void TearDown()
        {
            AppDomain.Unload(appDomain);
            if (File.Exists($"{Environment.CurrentDirectory}\\Converters.dll"))
                File.Delete($"{Environment.CurrentDirectory}\\Converters.dll");
        }

        [Test]
        public void IntContextTest()
        {
            assemblyBuilderWorker.IntContextTest();
        }

        [Test]
        public void StringContextTest()
        {
            assemblyBuilderWorker.StringContextTest();
        }

        [Test]
        public void EnumContextTest()
        {
            assemblyBuilderWorker.EnumContextTest();
        }

        [Test]
        public void ClassContextTest()
        {
            assemblyBuilderWorker.ClassContextTest();
        }

        [Test]
        public void InstanceConfigurationTest()
        {
            assemblyBuilderWorker.InstanceConfigurationTest();
        }

        [Test]
        public void InstanceConfigurationFixTest()
        {
            assemblyBuilderWorker.InstanceConfigurationFixTest();
        }

        [Test]
        public void GetTreeMutatorFromDllTest()
        {
            assemblyBuilderWorker.DllCreationTest(false);
        }

        [Test]
        public void GetSeveralTreeMutatorsFromDllTest()
        {
            assemblyBuilderWorker.DllCreationTest(true);
        }
    }
}