using System;
using System.Text;

namespace NHibernate.Caches.StackExchangeRedis
{
	/// <summary>
	/// Cache configuration for locking keys.
	/// </summary>
	public class RedisCacheLockConfiguration
	{
		private static readonly ICacheLockValueProvider DefaultValueProvider = new DefaultCacheLockValueProvider();
		private static readonly ICacheLockRetryDelayProvider DefaultRetryDelayProvider = new DefaultCacheLockRetryDelayProvider();

		private ICacheLockValueProvider _valueProvider;
		private ICacheLockRetryDelayProvider _retryDelayProvider;

		/// <summary>
		/// The timeout for a lock key to expire.
		/// </summary>
		public TimeSpan KeyTimeout { get; set; } = TimeSpan.FromSeconds(5);

		/// <summary>
		/// The suffix for the lock key.
		/// </summary>
		public string KeySuffix { get; set; } = ":lock";

		/// <summary>
		/// The time limit to acquire the lock.
		/// </summary>
		public TimeSpan AcquireTimeout { get; set; } = TimeSpan.FromSeconds(5);

		/// <summary>
		/// The number of retries for acquiring the lock.
		/// </summary>
		public int RetryTimes { get; set; } = 3;

		/// <summary>
		/// The maximum delay before retrying to acquire the lock.
		/// </summary>
		public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMilliseconds(400);

		/// <summary>
		/// The minimum delay before retrying to acquire the lock.
		/// </summary>
		public TimeSpan MinRetryDelay { get; set; } = TimeSpan.FromMilliseconds(10);

		/// <summary>
		/// The <see cref="ICacheLockValueProvider"/> instance.
		/// </summary>
		public ICacheLockValueProvider ValueProvider
		{
			get => _valueProvider ?? DefaultValueProvider;
			set => _valueProvider = value;
		}

		/// <summary>
		/// The <see cref="ICacheLockRetryDelayProvider"/> instance.
		/// </summary>
		public ICacheLockRetryDelayProvider RetryDelayProvider
		{
			get => _retryDelayProvider ?? DefaultRetryDelayProvider;
			set => _retryDelayProvider = value;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendFormat("KeyTimeout={0}s", KeyTimeout.TotalSeconds);
			sb.AppendFormat("KeySuffix=({0})", KeySuffix);
			sb.AppendFormat("AcquireTimeout={0}s", AcquireTimeout.TotalSeconds);
			sb.AppendFormat("RetryTimes={0}", RetryTimes);
			sb.AppendFormat("MaxRetryDelay={0}ms", MaxRetryDelay.TotalMilliseconds);
			sb.AppendFormat("MinRetryDelay={0}ms", MinRetryDelay.TotalMilliseconds);
			sb.AppendFormat("ValueProvider={0}", ValueProvider);
			sb.AppendFormat("RetryDelayProvider={0}", RetryDelayProvider);
			return sb.ToString();
		}
	}
}
