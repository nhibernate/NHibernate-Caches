#region License

//
//  SysCache - A cache provider for NHibernate using System.Web.Caching.Cache.
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
using System.Web.Caching;
using System.Xml;
using NUnit.Framework;

namespace NHibernate.Caches.SysCache.Tests
{
	[TestFixture]
	public class SysCacheSectionHandlerFixture
	{
		public XmlNode GetConfigurationSection(string xml)
		{
			var doc = new XmlDocument();
			doc.LoadXml(xml);
			return doc.DocumentElement;
		}

		public SysCache[] CreateCache(CacheConfig[] configuration)
		{
			var result = new List<SysCache>(configuration.Length);
			foreach (var config in configuration)
			{
				result.Add(new SysCache(config.Region, config.Properties));
			}
			return result.ToArray();
		}

		[Test]
		public void TestGetConfigNullSection()
		{
			var handler = new SysCacheSectionHandler();
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
			const string xmlSimple = "<syscache><cache region=\"foo\" expiration=\"500\" priority=\"4\" /></syscache>";

			var handler = new SysCacheSectionHandler();
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
			const string xmlSimple = "<syscache><cache region='foo' expiration='500' priority='AboveNormal' /></syscache>";

			var handler = new SysCacheSectionHandler();
			XmlNode section = GetConfigurationSection(xmlSimple);
			SysCache[] cache = CreateCache((CacheConfig[]) handler.Create(null, null, section));
			Assert.That(cache[0].Priority, Is.EqualTo(CacheItemPriority.AboveNormal));
		}

		[Test]
		public void RecognizeNumericPriorityValues()
		{
			const string xmlSimple = "<syscache><cache region='foo' expiration='500' priority='4' /></syscache>";

			var handler = new SysCacheSectionHandler();
			XmlNode section = GetConfigurationSection(xmlSimple);
			SysCache[] cache = CreateCache((CacheConfig[])handler.Create(null, null, section));
			Assert.That(cache[0].Priority, Is.EqualTo(CacheItemPriority.AboveNormal));
		}
	}
}