using System;
using System.Collections.Generic;
using NHibernate.Cache;

namespace NHibernate.Caches.StackExRedis
{
	/// <summary>
	/// Various methods to easier retrieve the configuration values.
	/// </summary>
	internal static class ConfigurationHelper
	{
		public static string GetString(string key, IDictionary<string, string> properties, string defaultValue)
		{
			if (properties == null)
			{
				return defaultValue;
			}
			return properties.TryGetValue(key, out var value) ? value : defaultValue;
		}

		public static bool GetBoolean(string key, IDictionary<string, string> properties, bool defaultValue)
		{
			if (properties == null)
			{
				return defaultValue;
			}
			return properties.TryGetValue(key, out var value) ? Convert.ToBoolean(value) : defaultValue;
		}

		public static int GetInteger(string key, IDictionary<string, string> properties, int defaultValue)
		{
			if (properties == null)
			{
				return defaultValue;
			}
			return properties.TryGetValue(key, out var value) ? Convert.ToInt32(value) : defaultValue;
		}

		public static TimeSpan GetTimeSpanFromSeconds(string key, IDictionary<string, string> properties, TimeSpan defaultValue)
		{
			if (properties == null)
			{
				return defaultValue;
			}
			var seconds = properties.TryGetValue(key, out var value)
				? Convert.ToInt64(value)
				: (long) defaultValue.TotalSeconds;
			return TimeSpan.FromSeconds(seconds);
		}

		public static TimeSpan GetTimeSpanFromMilliseconds(string key, IDictionary<string, string> properties, TimeSpan defaultValue)
		{
			if (properties == null)
			{
				return defaultValue;
			}
			var milliseconds = properties.TryGetValue(key, out var value)
				? Convert.ToInt64(value)
				: (long) defaultValue.TotalMilliseconds;
			return TimeSpan.FromMilliseconds(milliseconds);
		}

		public static System.Type GetSystemType(string key, IDictionary<string, string> properties, System.Type defaultValue)
		{
			var typeName = GetString(key, properties, null);
			if (typeName == null)
			{
				return defaultValue;
			}
			try
			{
				return System.Type.GetType(typeName, true);
			}
			catch (Exception e)
			{
				throw new CacheException($"Unable to aquire type '{typeName}' from the configuration property '{key}'", e);
			}
		}

		public static TType GetInstanceOfType<TType>(string key, IDictionary<string, string> properties, TType defaultValue)
		{
			var type = GetSystemType(key, properties, null);
			if (type == null)
			{
				return defaultValue;
			}
			if (!typeof(TType).IsAssignableFrom(type))
			{
				throw new CacheException($"Type '{type}' from the configuration property '{key}' is not assignable to '{typeof(TType)}'");
			}

			return (TType) Cfg.Environment.BytecodeProvider.ObjectsFactory.CreateInstance(type);
		}
	}
}
