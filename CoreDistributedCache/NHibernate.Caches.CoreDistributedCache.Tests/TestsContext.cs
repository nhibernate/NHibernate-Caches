#if !NETFX
using log4net.Repository.Hierarchy;
using NHibernate.Caches.Common.Tests;
using NUnit.Framework;

namespace NHibernate.Caches.CoreDistributedCache.Tests
{
	[SetUpFixture]
	public class TestsContext
	{
		[OneTimeSetUp]
		public void RunBeforeAnyTests()
		{
			TestsContextHelper.RunBeforeAnyTests(typeof(TestsContext).Assembly, "coredistributedcache");
			//When .NET Core App 2.0 tests run from VS/VSTest the entry assembly is "testhost.dll"
			//so we need to explicitly load the configuration
			if (TestsContextHelper.ExecutingWithVsTest)
			{
				ConfigureLog4Net();
			}
		}

		[OneTimeTearDown]
		public void RunAfterAnyTests()
		{
			TestsContextHelper.RunAfterAnyTests();
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
