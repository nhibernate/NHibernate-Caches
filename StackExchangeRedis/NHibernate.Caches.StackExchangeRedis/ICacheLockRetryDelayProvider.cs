using System;

namespace NHibernate.Caches.StackExchangeRedis
{
	/// <summary>
	/// Defines a method to return a <see cref="TimeSpan"/> to be waited before the next lock attempt.
	/// </summary>
	public interface ICacheLockRetryDelayProvider
	{
		/// <summary>
		/// Get a delay value between two values.
		/// </summary>
		/// <param name="minDelay">The minimum delay value.</param>
		/// <param name="maxDelay">The maximum delay value.</param>
		/// <returns></returns>
		TimeSpan GetValue(TimeSpan minDelay, TimeSpan maxDelay);
	}
}
