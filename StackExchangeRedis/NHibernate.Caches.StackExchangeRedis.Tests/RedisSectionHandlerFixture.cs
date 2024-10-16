#region License

//
//  CoreMemoryCache - A cache provider for NHibernate using Microsoft.Extensions.Caching.Memory.
//
//  This library is free software; you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation; either
//  version 2.1 of the License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

#endregion

using System;
using System.Configuration;
using System.IO;
using System.Xml;
using NHibernate.Caches.Common;
using NUnit.Framework;

namespace NHibernate.Caches.StackExchangeRedis.Tests
{
	[TestFixture]
	public class RedisSectionHandlerFixture
	{
		private static XmlNode GetConfigurationSection(string xml)
		{
			var doc = new XmlDocument();
			doc.LoadXml(xml);
			return doc.DocumentElement;
		}

		[Test]
		public void TestGetConfigNullSection()
		{
			var handler = new RedisSectionHandler();
			var section = new XmlDocument();
			var result = handler.Create(null, null, section);
			Assert.That(result, Is.Not.Null, "result");
			Assert.That(result, Is.InstanceOf<CacheConfig>());
			var config = (CacheConfig) result;
			Assert.That(config.Configuration, Is.Null, "Configuration");
			Assert.That(config.Regions, Is.Not.Null, "Regions");
			Assert.That(config.Regions.Length, Is.EqualTo(0));
		}

		[Test]
		public void TestGetConfigFromFile()
		{
			const string xmlSimple =
				"<redis configuration=\"127.0.0.1\"><cache region=\"foo\" expiration=\"500\" sliding=\"true\" /></redis>";

			var handler = new RedisSectionHandler();
			var section = GetConfigurationSection(xmlSimple);
			var result = handler.Create(null, null, section);
			Assert.That(result, Is.Not.Null);
			Assert.That(result, Is.InstanceOf<CacheConfig>());
			var config = (CacheConfig) result;
			Assert.That(config.Configuration, Is.EqualTo("127.0.0.1"), "Configuration");

			Assert.That(config.Regions, Is.Not.Null, "Regions");
			Assert.That(config.Regions.Length, Is.EqualTo(1), "Regions count");
			Assert.That(config.Regions[0].Region, Is.EqualTo("foo"));
			Assert.That(config.Regions[0].UseSlidingExpiration, Is.EqualTo(true));
			Assert.That(config.Regions[0].Expiration, Is.EqualTo(TimeSpan.FromSeconds(500)));
		}

		[Test]
		public void TestGetConfigFromProvidedConfiguration()
		{
			var assemblyPath =
				Path.Combine(TestContext.CurrentContext.TestDirectory, Path.GetFileName(GetType().Assembly.Location));
			ConfigurationProvider.SetConfiguration(ConfigurationManager.OpenExeConfiguration(assemblyPath));
			var config = ConfigurationProvider.Current.GetConfiguration();

			Assert.That(config, Is.Not.Null, "config");
			Assert.That(config.Configuration, Is.EqualTo("127.0.0.1"), "Configuration");

			Assert.That(config.Regions, Is.Not.Null, "Regions");
			Assert.That(config.Regions.Length, Is.GreaterThan(1), "Regions count");
			Assert.That(config.Regions[0].Region, Is.EqualTo("foo"));
			Assert.That(config.Regions[0].UseSlidingExpiration, Is.EqualTo(true));
			Assert.That(config.Regions[0].Expiration, Is.EqualTo(TimeSpan.FromSeconds(500)));
		}

		private ConfigurationProviderBase<CacheConfig> _configurationProviderBackup;

		[SetUp]
		public void OnSetup()
		{
			_configurationProviderBackup = ConfigurationProvider.Current;
		}

		[TearDown]
		public void OnTearDown()
		{
			ConfigurationProvider.Current = _configurationProviderBackup;
		}
	}
}
