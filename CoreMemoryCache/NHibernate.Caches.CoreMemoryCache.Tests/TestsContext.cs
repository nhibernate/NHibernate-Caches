#if !NETFX
using NUnit.Framework;
using System.Configuration;
using System.IO;
using System.Reflection;
using log4net.Repository.Hierarchy;
using NHibernate.Cfg;
using NHibernate.Cfg.ConfigurationSchema;
using Environment = NHibernate.Cfg.Environment;

namespace NHibernate.Caches.CoreMemoryCache.Tests
{
	[SetUpFixture]
	public class TestsContext
	{
		private static readonly bool ExecutingWithVsTest =
			Assembly.GetEntryAssembly()?.GetName().Name == "testhost";

		private static bool _removeTesthostConfig;

		[OneTimeSetUp]
		public void RunBeforeAnyTests()
		{
			//When .NET Core App 2.0 tests run from VS/VSTest the entry assembly is "testhost.dll"
			//so we need to explicitly load the configuration
			if (ExecutingWithVsTest)
			{
				Environment.InitializeGlobalProperties(GetTestAssemblyHibernateConfiguration());
				ReadCoreCacheSectionFromTesthostConfig();
			}

			ConfigureLog4Net();
		}

		[OneTimeTearDown]
		public void RunAfterAnyTests()
		{
			if (_removeTesthostConfig)
			{
				File.Delete(GetTesthostConfigPath());
			}
		}

		private static void ReadCoreCacheSectionFromTesthostConfig()
		{
			// For caches section, ConfigurationManager being directly used, the only workaround is to provide
			// the configuration with its expected file name...
			var assemblyPath =
				Path.Combine(TestContext.CurrentContext.TestDirectory, Path.GetFileName(typeof(TestsContext).Assembly.Location));
			var configPath = assemblyPath + ".config";
			// If this copy fails: either testconfig has started having its own file, and this hack can no more be used,
			// or a previous test run was interupted before its cleanup (RunAfterAnyTests): go clean it manually.
			// Discussion about this mess: https://github.com/dotnet/corefx/issues/22101
			File.Copy(configPath, GetTesthostConfigPath());
			_removeTesthostConfig = true;
			ConfigurationManager.RefreshSection("corememorycache");
		}

		private static string GetTesthostConfigPath()
		{
			return Assembly.GetEntryAssembly().Location + ".config";
		}

		private static IHibernateConfiguration GetTestAssemblyHibernateConfiguration()
		{
			var assemblyPath =
				Path.Combine(TestContext.CurrentContext.TestDirectory, Path.GetFileName(typeof(TestsContext).Assembly.Location));
			var configuration = ConfigurationManager.OpenExeConfiguration(assemblyPath);
			var section = configuration.GetSection(CfgXmlHelper.CfgSectionName);
			return HibernateConfiguration.FromAppConfig(section.SectionInformation.GetRawXml());
		}

		private static void ConfigureLog4Net()
		{
			var hierarchy = (Hierarchy)log4net.LogManager.GetRepository(typeof(TestsContext).Assembly);

			var consoleAppender = new log4net.Appender.ConsoleAppender
			{
				Layout = new log4net.Layout.PatternLayout("%d{ABSOLUTE} %-5p %c{1}:%L - %m%n"),
			};
			hierarchy.Root.Level = log4net.Core.Level.Info;
			hierarchy.Root.AddAppender(consoleAppender);
			hierarchy.Configured = true;
		}
	}
}
#endif
