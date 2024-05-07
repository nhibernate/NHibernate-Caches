#if !NETFX
using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using NHibernate.Cfg.ConfigurationSchema;
using NUnit.Framework;

namespace NHibernate.Caches.Common.Tests
{
	public static class TestsContextHelper
	{
		public static void RunBeforeAnyTests(Assembly testAssembly, Action<Configuration> configure)
		{
			//When .NET Core App 2.0 tests run from VS/VSTest the entry assembly is "testhost.dll"
			//so we need to explicitly load the configuration
			if (Assembly.GetEntryAssembly() != null)
			{
				var assemblyPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
					Path.GetFileName(testAssembly.Location));
				var configuration = ConfigurationManager.OpenExeConfiguration(assemblyPath);
				Cfg.Environment.InitializeGlobalProperties(GetTestAssemblyHibernateConfiguration(configuration));
				configure?.Invoke(configuration);
			}
		}

		public static void RunAfterAnyTests()
		{
		}

		private static Cfg.IHibernateConfiguration GetTestAssemblyHibernateConfiguration(Configuration configuration)
		{
			var section = configuration.GetSection(CfgXmlHelper.CfgSectionName);
			return HibernateConfiguration.FromAppConfig(section.SectionInformation.GetRawXml());
		}
	}
}
#endif
