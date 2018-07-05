using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NHibernate.Caches.Common.Tests
{
	[TestFixture]
	public class BinaryCacheSerializerFixture : CacheSerializerFixture
	{
		protected override Func<ICacheSerializer> SerializerProvider => () => new BinaryCacheSerializer();
	}
}
