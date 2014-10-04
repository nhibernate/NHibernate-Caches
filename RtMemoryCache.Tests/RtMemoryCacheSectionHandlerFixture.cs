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

using System.Collections.Generic;
using System.Runtime.Caching;
using System.Xml;
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

		public RtMemoryCache[] CreateCache(CacheConfig[] configuration)
		{
			var result = new List<RtMemoryCache>(configuration.Length);
			foreach (var config in configuration)
			{
				result.Add(new RtMemoryCache(config.Region, config.Properties));
			}
			return result.ToArray();
		}

		[Test]
		public void TestGetConfigNullSection()
		{
			var handler = new RtMemoryCacheSectionHandler();
			var section = new XmlDocument();
			object result = handler.Create(null, null, section);
			Assert.IsNotNull(result);
			Assert.IsTrue(result is CacheConfig[]);
			var caches = result as CacheConfig[];
			Assert.AreEqual(0, caches.Length);
		}

		[Test]
		public void TestGetConfigFromFile()
		{
            const string xmlSimple = "<rtmemorycache><cache region=\"foo\" expiration=\"500\" priority=\"4\" /></rtmemorycache>";

			var handler = new RtMemoryCacheSectionHandler();
			XmlNode section = GetConfigurationSection(xmlSimple);
			object result = handler.Create(null, null, section);
			Assert.IsNotNull(result);
			Assert.IsTrue(result is CacheConfig[]);
			var caches = result as CacheConfig[];
			Assert.AreEqual(1, caches.Length);
		}

		[Test]
		public void RecognizeMnemonicPriorityValues()
		{
            const string xmlSimple = "<rtmemorycache><cache region='foo' expiration='500' priority='NotRemovable' /></rtmemorycache>";

			var handler = new RtMemoryCacheSectionHandler();
			XmlNode section = GetConfigurationSection(xmlSimple);
			RtMemoryCache[] cache = CreateCache((CacheConfig[]) handler.Create(null, null, section));
			Assert.That(cache[0].Priority, Is.EqualTo(CacheItemPriority.NotRemovable));
		}

		[Test]
		public void RecognizeNumericPriorityValues()
		{
            const string xmlSimple = "<rtmemorycache><cache region='foo' expiration='500' priority='1' /></rtmemorycache>";

			var handler = new RtMemoryCacheSectionHandler();
			XmlNode section = GetConfigurationSection(xmlSimple);
			RtMemoryCache[] cache = CreateCache((CacheConfig[])handler.Create(null, null, section));
            Assert.That(cache[0].Priority, Is.EqualTo(CacheItemPriority.NotRemovable));
		}
	}
}