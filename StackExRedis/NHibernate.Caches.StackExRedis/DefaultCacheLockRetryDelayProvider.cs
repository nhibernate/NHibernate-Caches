using System;

namespace NHibernate.Caches.StackExRedis
{
	/// <inheritdoc />
	public class DefaultCacheLockRetryDelayProvider : ICacheLockRetryDelayProvider
	{
		private readonly Random _random = new Random();

		/// <inheritdoc />
		public TimeSpan GetValue(TimeSpan minDelay, TimeSpan maxDelay)
		{
			var delay = _random.NextDouble() * (maxDelay.TotalMilliseconds - minDelay.TotalMilliseconds) +
						minDelay.TotalMilliseconds;
			return TimeSpan.FromMilliseconds(delay);
		}
	}
}
