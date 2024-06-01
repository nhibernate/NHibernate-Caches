using System;
using System.Configuration;
using System.Xml;

namespace NHibernate.Caches.Common
{
	/// <summary>
	/// Base generic class for the cache configuration settings.
	/// </summary>
	/// <typeparam name="TConfig">The configuration class.</typeparam>
	public abstract class ConfigurationProviderBase<TConfig>
		where TConfig : class
	{
		/// <summary>
		/// Get the cache configuration.
		/// </summary>
		/// <returns>The cache configuration.</returns>
		public abstract TConfig GetConfiguration();
	}

	/// <summary>
	/// Base generic class for the cache configuration settings.
	/// </summary>
	/// <typeparam name="TConfig">The configuration class.</typeparam>
	/// <typeparam name="TConfigHandler">The configuration class section handler.</typeparam>
	public abstract class ConfigurationProviderBase<TConfig, TConfigHandler> : ConfigurationProviderBase<TConfig>
		where TConfig : class
		where TConfigHandler : ICacheConfigurationSectionHandler, new()
	{
		private static ConfigurationProviderBase<TConfig> _current;
		private static readonly string ConfigurationSectionName;

		static ConfigurationProviderBase()
		{
			ConfigurationSectionName = new TConfigHandler().ConfigurationSectionName;
		}

		/// <summary>
		/// Provides ability to override default <see cref="System.Configuration.ConfigurationManager"/> with custom implementation.
		/// Can be set to null if all configuration is specified by code.
		/// </summary>
		public static ConfigurationProviderBase<TConfig> Current
		{
			get => _current ?? (_current = new StaticConfigurationManagerProvider());
			set => _current = value ?? new NullConfigurationProvider();
		}

		/// <summary>
		/// Directly supply the configuration to be used to the configuration provider.
		/// </summary>
		/// <param name="configuration">The configuration, or <see langword="null" /> for resetting the provider to its
		/// default behavior.</param>
		public static void SetConfiguration(Configuration configuration)
		{
			_current = configuration == null ? null : new SystemConfigurationProvider(configuration);
		}

		private class StaticConfigurationManagerProvider : ConfigurationProviderBase<TConfig, TConfigHandler>
		{
			/// <inheritdoc />
			public override TConfig GetConfiguration()
			{
				// 6.0 TODO: Throw if not null and not CacheConfig
				return ConfigurationManager.GetSection(ConfigurationSectionName) as TConfig;
			}
		}

		private class SystemConfigurationProvider : ConfigurationProviderBase<TConfig, TConfigHandler>
		{
			private readonly Configuration _configuration;

			public SystemConfigurationProvider(Configuration configuration)
			{
				_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			}

			public override TConfig GetConfiguration()
			{
				var configurationSection = _configuration.GetSection(ConfigurationSectionName);
				var xml = configurationSection?.SectionInformation.GetRawXml();
				if (xml == null)
					return null;

				var xmlDoc = new XmlDocument();
				xmlDoc.LoadXml(xml);
				if (xmlDoc.DocumentElement == null)
					return null;

				var section = new TConfigHandler();
				return (TConfig) section.Create(null, null, xmlDoc.DocumentElement);
			}
		}

		private class NullConfigurationProvider : ConfigurationProviderBase<TConfig>
		{
			/// <inheritdoc />
			public override TConfig GetConfiguration()
			{
				return null;
			}
		}
	}
}
