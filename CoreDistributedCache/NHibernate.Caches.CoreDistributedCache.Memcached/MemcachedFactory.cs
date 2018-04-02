using System;
using System.Collections.Generic;
using Enyim.Caching;
using Enyim.Caching.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace NHibernate.Caches.CoreDistributedCache.Memcached
{
	/// <summary>
	/// A Memcached distributed cache factory.
	/// </summary>
	public class MemcachedFactory : IDistributedCacheFactory
	{
		private static readonly INHibernateLogger Log = NHibernateLogger.For(typeof(MemcachedFactory));
		private const string _configuration = "configuration";

		private readonly IDistributedCache _cache;

		/// <summary>
		/// Constructor with configuration properties. It supports <c>configuration</c>, which has to be a JSON string
		/// structured like the value part of the <c>"enyimMemcached"</c> property in an appsettings.json file.
		/// </summary>
		/// <param name="properties">The configurations properties.</param>
		public MemcachedFactory(IDictionary<string, string> properties) : this()
		{
			MemcachedClientOptions options;
			if (properties != null && properties.TryGetValue(_configuration, out var configuration) && !string.IsNullOrWhiteSpace(configuration))
			{
				options = JsonConvert.DeserializeObject<MemcachedClientOptions>(configuration);
			}
			else
			{
				Log.Warn("No {0} property provided", _configuration);
				options = new MemcachedClientOptions();
			}

			var loggerFactory = new LoggerFactory();

			_cache = new MemcachedClient(loggerFactory, new MemcachedClientConfiguration(loggerFactory, options));
		}

		private MemcachedFactory()
		{
			Constraints = new CacheConstraints
			{
				MaxKeySize = 250,
				KeySanitizer = SanitizeKey
			};
		}

		// According to https://groups.google.com/forum/#!topic/memcached/Tz1RE0FUbNA,
		// memcached key can't contain space, newline, return, tab, vertical tab or form feed.
		// Since keys contains entity identifiers which may be anything, purging them all.
		private static readonly char[] ForbiddenChar = new [] { ' ', '\n', '\r', '\t', '\v', '\f' };

		private static string SanitizeKey(string key)
		{
			foreach (var forbidden in ForbiddenChar)
			{
				key = key.Replace(forbidden, '-');
			}
			return key;
		}

		/// <inheritdoc />
		public CacheConstraints Constraints { get; }

		/// <inheritdoc />
		[CLSCompliant(false)]
		public IDistributedCache BuildCache()
		{
			return _cache;
		}

		private class LoggerFactory : Microsoft.Extensions.Logging.ILoggerFactory
		{
			public void Dispose()
			{
			}

			public ILogger CreateLogger(string categoryName)
			{
				return new LoggerWrapper(NHibernateLogger.For(categoryName));
			}

			public void AddProvider(ILoggerProvider provider)
			{
			}
		}

		private class LoggerWrapper : ILogger
		{
			private readonly INHibernateLogger _logger;

			public LoggerWrapper(INHibernateLogger logger)
			{
				_logger = logger;
			}

			void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
				Func<TState, Exception, string> formatter)
			{
				if (!IsEnabled(logLevel))
					return;

				if (formatter == null)
					throw new ArgumentNullException(nameof(formatter));

				_logger.Log(
					TranslateLevel(logLevel),
					new NHibernateLogValues("EventId {0}: {1}", new object[] { eventId, formatter(state, exception) }),
					// Avoid double logging of exception by not providing it to the logger, but only to the formatter.
					null);
			}

			public bool IsEnabled(LogLevel logLevel)
				=> _logger.IsEnabled(TranslateLevel(logLevel));

			public IDisposable BeginScope<TState>(TState state)
				=> NoopScope.Instance;

			private NHibernateLogLevel TranslateLevel(LogLevel level)
			{
				switch (level)
				{
					case LogLevel.None:
						return NHibernateLogLevel.None;
					case LogLevel.Trace:
						return NHibernateLogLevel.Trace;
					case LogLevel.Debug:
						return NHibernateLogLevel.Debug;
					case LogLevel.Information:
						return NHibernateLogLevel.Info;
					case LogLevel.Warning:
						return NHibernateLogLevel.Warn;
					case LogLevel.Error:
						return NHibernateLogLevel.Error;
					case LogLevel.Critical:
						return NHibernateLogLevel.Fatal;
				}

				return NHibernateLogLevel.Trace;
			}

			private class NoopScope : IDisposable
			{
				public static readonly NoopScope Instance = new NoopScope();

				public void Dispose()
				{
				}
			}
		}
	}
}
