#region License

//
//  CoreDistributedCache - A cache provider for NHibernate using Microsoft.Extensions.Caching.Distributed.IDistributedCache.
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

using System.Xml;
using NUnit.Framework;

namespace NHibernate.Caches.CoreDistributedCache.Tests
{
	[TestFixture]
	public class CoreDistributedCacheSectionHandlerFixture
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
			var handler = new CoreDistributedCacheSectionHandler();
			var section = new XmlDocument();
			var result = handler.Create(null, null, section);
			Assert.That(result, Is.Not.Null, "result");
			Assert.That(result, Is.InstanceOf<CacheConfig>());
			var config = (CacheConfig) result;
			Assert.That(config.Properties, Is.Not.Null, "Properties");
			Assert.That(config.Properties.Count, Is.EqualTo(0), "Properties count");
			Assert.That(config.Regions, Is.Not.Null, "Regions");
			Assert.That(config.Regions.Length, Is.EqualTo(0));
		}

		[Test]
		public void TestGetConfigFromFile()
		{
			const string xmlSimple = "<coredistributedcache factory-class=\"factory1\"><properties><property name=\"prop1\">Value1</property></properties><cache region=\"foo\" expiration=\"500\" sliding=\"true\" /></coredistributedcache>";

			var handler = new CoreDistributedCacheSectionHandler();
			var section = GetConfigurationSection(xmlSimple);
			var result = handler.Create(null, null, section);
			Assert.That(result, Is.Not.Null, "result");
			Assert.That(result, Is.InstanceOf<CacheConfig>());
			var config = (CacheConfig) result;

			Assert.That(config.FactoryClass, Is.EqualTo("factory1"));

			Assert.That(config.Properties, Is.Not.Null, "Properties");
			Assert.That(config.Properties.Count, Is.EqualTo(1), "Properties count");
			Assert.That(config.Properties, Does.ContainKey("prop1"));
			Assert.That(config.Properties["prop1"], Is.EqualTo("Value1"));

			Assert.That(config.Regions, Is.Not.Null, "Regions");
			Assert.That(config.Regions.Length, Is.EqualTo(1), "Regions count");
			Assert.That(config.Regions[0].Region, Is.EqualTo("foo"));
			Assert.That(config.Regions[0].Properties, Does.ContainKey("cache.use_sliding_expiration"));
			Assert.That(config.Regions[0].Properties["cache.use_sliding_expiration"], Is.EqualTo("true"));
			Assert.That(config.Regions[0].Properties, Does.ContainKey("expiration"));
			Assert.That(config.Regions[0].Properties["expiration"], Is.EqualTo("500"));
		}
	}
}
