using log4net.Repository.Hierarchy;
#if !NETFX
using NHibernate.Caches.Common.Tests;
#endif
using NUnit.Framework;

namespace NHibernate.Caches.StackExRedis.Tests
{
	[SetUpFixture]
	public class TestsContext
	{
		[OneTimeSetUp]
		public void RunBeforeAnyTests()
		{
#if !NETFX
			TestsContextHelper.RunBeforeAnyTests(typeof(TestsContext).Assembly, "redis");
#endif
			ConfigureLog4Net();
		}

#if !NETFX
		[OneTimeTearDown]
		public void RunAfterAnyTests()
		{
			TestsContextHelper.RunAfterAnyTests();
		}
#endif

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
