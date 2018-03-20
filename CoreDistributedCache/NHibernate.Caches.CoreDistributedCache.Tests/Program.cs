#if !NETFX
namespace NHibernate.Caches.CoreDistributedCache.Tests
{
	public class Program
	{
		public static int Main(string[] args)
		{
			return new NUnitLite.AutoRun(typeof(Program).Assembly).Execute(args);
		}
	}
}
#endif
