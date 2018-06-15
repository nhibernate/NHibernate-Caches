using System;

namespace NHibernate.Caches.StackExRedis
{
	/// <inheritdoc />
	public class DefaultCacheLockValueProvider : ICacheLockValueProvider
	{
		/// <inheritdoc />
		public string GetValue()
		{
			return Guid.NewGuid().ToString();
		}
	}
}
