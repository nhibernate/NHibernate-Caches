using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace NHibernate.Caches.CoreDistributedCache.Tests
{
	public class MemoryDistributedCacheFactory : IDistributedCacheFactory
	{
		public IDistributedCache BuildCache()
		{
			return new MemoryDistributedCache(new Options());
		}

		private class Options : MemoryDistributedCacheOptions, IOptions<MemoryDistributedCacheOptions>
		{
			MemoryDistributedCacheOptions IOptions<MemoryDistributedCacheOptions>.Value => this;
		}
	}
}
