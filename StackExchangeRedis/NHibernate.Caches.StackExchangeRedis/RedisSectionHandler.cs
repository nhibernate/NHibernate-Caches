using System;
using System.Collections.Generic;
using System.Xml;
using NHibernate.Caches.Common;

namespace NHibernate.Caches.StackExchangeRedis
{
	/// <summary>
	/// Configuration file provider.
	/// </summary>
	public class RedisSectionHandler : ICacheConfigurationSectionHandler
	{
		private static readonly INHibernateLogger Log = NHibernateLogger.For(typeof(RedisSectionHandler));

		#region IConfigurationSectionHandler Members

		/// <inheritdoc />
		/// <returns>A <see cref="CacheConfig" /> object.</returns>
		public object Create(object parent, object configContext, XmlNode section)
		{
			var configuration = section.Attributes?["configuration"]?.Value;
			var regions = new List<RegionConfig>();

			var nodes = section.SelectNodes("cache");
			foreach (XmlNode node in nodes)
			{
				var region = node.Attributes?["region"]?.Value;
				if (region != null)
				{
					regions.Add(new RegionConfig(
						region,
						GetTimespanFromSeconds(node, "expiration"),
						GetBoolean(node, "sliding"),
						GetInteger(node, "database"),
						GetType(node, "strategy"),
						GetBoolean(node, "append-hashcode")
					));
				}
				else
				{
					Log.Warn("Found a cache region node lacking a region name: ignored. Node: {0}",
						node.OuterXml);
				}
			}
			return new CacheConfig(configuration, regions.ToArray());
		}

		private static TimeSpan? GetTimespanFromSeconds(XmlNode node, string attributeName)
		{
			var seconds = GetInteger(node, attributeName);
			if (!seconds.HasValue)
			{
				return null;
			}
			return TimeSpan.FromSeconds(seconds.Value);
		}

		private static bool? GetBoolean(XmlNode node, string attributeName)
		{
			var value = node.Attributes?[attributeName]?.Value;
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			if (bool.TryParse(value, out var boolean))
			{
				return boolean;
			}

			Log.Warn("Invalid value for boolean attribute {0}: ignored. Node: {1}", attributeName, node.OuterXml);
			return null;
		}

		private static System.Type GetType(XmlNode node, string attributeName)
		{
			var value = node.Attributes?[attributeName]?.Value;
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			try
			{
				return System.Type.GetType(value, true);
			}
			catch (Exception e)
			{
				Log.Warn("Unable to acquire type for attribute {0}: ignored. Node: {1}, Exception: {2}", attributeName, node.OuterXml, e);
				return null;
			}
		}

		private static int? GetInteger(XmlNode node, string attributeName)
		{
			var value = node.Attributes?[attributeName]?.Value;
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			if (int.TryParse(value, out var number))
			{
				return number;
			}

			Log.Warn("Invalid value for integer attribute {0}: ignored. Node: {1}", attributeName, node.OuterXml);
			return null;
		}

		#endregion

		/// <inheritdoc />
		public string ConfigurationSectionName => "redis";
	}
}
