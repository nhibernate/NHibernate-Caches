using System;

namespace NHibernate.Caches.StackExchangeRedis
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
