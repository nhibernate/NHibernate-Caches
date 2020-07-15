using System.Configuration;

namespace NHibernate.Caches.Common
{
	/// <inheritdoc />
	public interface ICacheConfigurationSectionHandler : IConfigurationSectionHandler
	{
		/// <summary>
		/// The configuration section name.
		/// </summary>
		string ConfigurationSectionName { get; }
	}
}
