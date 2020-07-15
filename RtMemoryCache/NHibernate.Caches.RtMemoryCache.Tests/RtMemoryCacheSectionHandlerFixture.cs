#region License

//
//  RtMemoryCache - A cache provider for NHibernate using System.Runtime.Caching.MemoryCache.
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
// CLOVER:OFF
//

#endregion

using System.Configuration;
using System.IO;
using System.Xml;
using NHibernate.Caches.Common;
using NUnit.Framework;

namespace NHibernate.Caches.RtMemoryCache.Tests
{
	[TestFixture]
	public class RtMemoryCacheSectionHandlerFixture
	{
		public XmlNode GetConfigurationSection(string xml)
		{
			var doc = new XmlDocument();
			doc.LoadXml(xml);
			return doc.DocumentElement;
		}

		[Test]
		public void TestGetConfigNullSection()
		{
			var handler = new RtMemoryCacheSectionHandler();
			var section = new XmlDocument();
			object result = handler.Create(null, null, section);
			Assert.That(result, Is.Not.Null);
			Assert.IsTrue(result is CacheConfig[]);
			var caches = result as CacheConfig[];
			Assert.That(caches.Length, Is.EqualTo(0));
		}

		[Test]
		public void TestGetConfigFromFile()
		{
			const string xmlSimple = "<rtmemorycache><cache region=\"foo\" expiration=\"500\" sliding=\"true\" /></rtmemorycache>";

			var handler = new RtMemoryCacheSectionHandler();
			XmlNode section = GetConfigurationSection(xmlSimple);
			object result = handler.Create(null, null, section);
			Assert.That(result, Is.Not.Null);
			Assert.IsTrue(result is CacheConfig[]);
			var caches = result as CacheConfig[];
			Assert.That(caches.Length, Is.EqualTo(1));
			Assert.That(caches[0].Properties, Does.ContainKey("cache.use_sliding_expiration"));
		}

		[Test]
		public void TestGetConfigFromProvidedConfiguration()
		{
			var assemblyPath =
				Path.Combine(TestContext.CurrentContext.TestDirectory, Path.GetFileName(GetType().Assembly.Location));
			ConfigurationProvider.SetConfiguration(ConfigurationManager.OpenExeConfiguration(assemblyPath));
			var config = ConfigurationProvider.Current.GetConfiguration();

			Assert.That(config, Is.Not.Null, "config");

			Assert.That(config.Length, Is.GreaterThan(1), "Regions count");
			Assert.That(config[0].Region, Is.EqualTo("foo"));
			Assert.That(config[0].Properties, Does.ContainKey("cache.use_sliding_expiration"));
			Assert.That(config[0].Properties["cache.use_sliding_expiration"], Is.EqualTo("true"));
			Assert.That(config[0].Properties, Does.ContainKey("expiration"));
			Assert.That(config[0].Properties["expiration"], Is.EqualTo("500"));
		}

		private ConfigurationProviderBase<CacheConfig[], RtMemoryCacheSectionHandler> _configurationProviderBackup;

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
