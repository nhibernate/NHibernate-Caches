using System;
using System.Collections.Generic;
using NHibernate.Bytecode;
using NHibernate.Cache;
using NHibernate.Util;

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
			return typeName == null ? defaultValue : ReflectHelper.ClassForName(typeName);
		}

		public static TType GetInstance<TType>(string key, IDictionary<string, string> properties, TType defaultValue,
			INHibernateLogger logger)
		{
			var objectsFactory = Cfg.Environment.ObjectsFactory;
			var className = GetString(key, properties, null);
			System.Type type = null;
			try
			{
				if (className != null)
				{
					type = ReflectHelper.ClassForName(className);
					return (TType) objectsFactory.CreateInstance(type);
				}

				// Try to get the instance from the base type if the user provided a custom IObjectsFactory
				if (!(objectsFactory is ActivatorObjectsFactory))
				{
					try
					{
						return (TType) objectsFactory.CreateInstance(typeof(TType));
					}
					catch (Exception e)
					{
						// The user most likely did not register the TType
						logger.Debug(
							"Failed to create an instance of type '{0}' by using IObjectsFactory, most probably was not registered. Exception: {1}",
							typeof(TType), e);
					}
				}

				return defaultValue;
			}
			catch (Exception e)
			{
				throw new HibernateException(
					$"Could not instantiate {typeof(TType).Name}: {type?.AssemblyQualifiedName ?? className}", e);
			}
		}
	}
}
