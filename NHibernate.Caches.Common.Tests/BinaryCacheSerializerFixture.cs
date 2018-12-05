using System;
using NUnit.Framework;

namespace NHibernate.Caches.Common.Tests
{
	[TestFixture]
	public class BinaryCacheSerializerFixture : CacheSerializerFixture
	{
		protected override Func<CacheSerializerBase> SerializerProvider => () => new BinaryCacheSerializer();
	}
}
