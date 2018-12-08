using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NHibernate.Caches.StackExchangeRedis
{
	/// <summary>
	/// A retry policy that can be applied to delegates returning a value of type <typeparamref name="TResult"/>.
	/// </summary>
	/// <typeparam name="TResult">The type of the execution result.</typeparam>
	/// <typeparam name="TContext">The context¸that can be used in callbacks.</typeparam>
	internal class RetryPolicy<TResult, TContext>
	{
		private readonly int _retryTimes;
		private readonly Func<TimeSpan> _retryDelayFunc;
		private readonly double _maxAllowedTime;
		private Predicate<TResult> _shouldRetryPredicate;
		private Action<int, long, TContext> _onFailureCallback;

		public RetryPolicy(int retryTimes, TimeSpan maxAllowedTime, Func<TimeSpan> retryDelayFunc)
		{
			_retryTimes = retryTimes;
			_retryDelayFunc = retryDelayFunc;
			_maxAllowedTime = maxAllowedTime.TotalMilliseconds;
		}

		public RetryPolicy<TResult, TContext> ShouldRetry(Predicate<TResult> predicate)
		{
			_shouldRetryPredicate = predicate;
			return this;
		}

		public RetryPolicy<TResult, TContext> OnFailure(Action<int, long, TContext> callback)
		{
			_onFailureCallback = callback;
			return this;
		}

		public TResult Execute(Func<TResult> func, TContext context)
		{
			var totalAttempts = 0;
			var timer = new Stopwatch();
			timer.Start();
			do
			{
				if (totalAttempts > 0)
				{
					var retryDelay = _retryDelayFunc();
					Thread.Sleep(retryDelay);
				}

				var result = func();
				if (_shouldRetryPredicate?.Invoke(result) != true)
				{
					return result;
				}
				totalAttempts++;

			} while (_retryTimes > totalAttempts - 1 && timer.ElapsedMilliseconds < _maxAllowedTime);
			timer.Stop();
			_onFailureCallback?.Invoke(totalAttempts, timer.ElapsedMilliseconds, context);
			return default(TResult);
		}

		public async Task<TResult> ExecuteAsync(Func<Task<TResult>> func, TContext context, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var totalAttempts = 0;
			var timer = new Stopwatch();
			timer.Start();
			do
			{
				if (totalAttempts > 0)
				{
					var retryDelay = _retryDelayFunc();
					await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
				}

				var result = await func().ConfigureAwait(false);
				if (_shouldRetryPredicate?.Invoke(result) != true)
				{
					return result;
				}
				totalAttempts++;

			} while (_retryTimes > totalAttempts - 1 && timer.ElapsedMilliseconds < _maxAllowedTime);
			timer.Stop();
			_onFailureCallback?.Invoke(totalAttempts, timer.ElapsedMilliseconds, context);
			return default(TResult);
		}
	}
}
