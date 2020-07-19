#if !NETFX
using System.Configuration;
using System.IO;
using System.Reflection;
using NHibernate.Cfg;
using NHibernate.Cfg.ConfigurationSchema;
using NUnit.Framework;
using Environment = NHibernate.Cfg.Environment;

namespace NHibernate.Caches.Common.Tests
{
	public static class TestsContextHelper
	{
		public static bool ExecutingWithVsTest { get; } =
			Assembly.GetEntryAssembly()?.GetName().Name == "testhost";

		private static bool _removeTesthostConfig;

		public static void RunBeforeAnyTests(Assembly testAssembly, string configSectionName)
		{
			//When .NET Core App 2.0 tests run from VS/VSTest the entry assembly is "testhost.dll"
			//so we need to explicitly load the configuration
			if (ExecutingWithVsTest)
			{
				var assemblyPath =
					Path.Combine(TestContext.CurrentContext.TestDirectory, Path.GetFileName(testAssembly.Location));
				Environment.InitializeGlobalProperties(GetTestAssemblyHibernateConfiguration(assemblyPath));
				ReadCoreCacheSectionFromTesthostConfig(assemblyPath, configSectionName);
			}
		}

		public static void RunAfterAnyTests()
		{
			if (_removeTesthostConfig)
			{
				File.Delete(GetTesthostConfigPath());
			}
		}

		private static void ReadCoreCacheSectionFromTesthostConfig(string assemblyPath, string configSectionName)
		{
			// For caches section, ConfigurationManager being used, the only general workaround is to provide
			// the configuration with its expected file name... (Another option would be to explicitly setup each cache
			// ConfigurationProvider.)
			var configPath = assemblyPath + ".config";
			// If this copy fails: either testconfig has started having its own file, and this hack can no more be used,
			// or a previous test run was interrupted before its cleanup (RunAfterAnyTests): go clean it manually.
			// Discussion about this mess: https://github.com/dotnet/corefx/issues/22101
			File.Copy(configPath, GetTesthostConfigPath());
			_removeTesthostConfig = true;
			ConfigurationManager.RefreshSection(configSectionName);
		}

		private static string GetTesthostConfigPath()
		{
			return Assembly.GetEntryAssembly().Location + ".config";
		}

		private static IHibernateConfiguration GetTestAssemblyHibernateConfiguration(string assemblyPath)
		{
			var configuration = ConfigurationManager.OpenExeConfiguration(assemblyPath);
			var section = configuration.GetSection(CfgXmlHelper.CfgSectionName);
			return HibernateConfiguration.FromAppConfig(section.SectionInformation.GetRawXml());
		}
	}
}
#endif
