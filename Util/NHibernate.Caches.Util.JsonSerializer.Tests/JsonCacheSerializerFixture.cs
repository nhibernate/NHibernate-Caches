using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Serialization;
using NHibernate.Caches.Common;
using NHibernate.Caches.Common.Tests;
using NHibernate.Intercept;
using NHibernate.Properties;
using NUnit.Framework;

namespace NHibernate.Caches.Util.JsonSerializer.Tests
{
	[TestFixture]
	public class JsonCacheSerializerFixture : CacheSerializerFixture
	{
		protected override Func<ICacheSerializer> SerializerProvider => CreateDefaultSerializer;

		[Test]
		public void TestStrictSerialization()
		{
			var serializer = new JsonCacheSerializer();
			Assert.Throws<InvalidOperationException>(() => serializer.Serialize(new CustomEntity {Id = 10}),
				"Non standard types should be registered explicitly");
		}

		[Test]
		public void TestAliasNames()
		{
			var original = new object[]
			{
				(short) 1,
				(ushort) 2,
				3,
				(uint) 4,
				(long) 5,
				(ulong) 6,
				(sbyte) 7,
				(byte) 8,
				9.1m,
				10.2f,
				11.3,
				Guid.Empty,
				'a',
				TimeSpan.FromTicks(1234),
				DateTimeOffset.FromUnixTimeSeconds(100),
				new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
				new byte[] {12},
				new List<object>{13},
				new Hashtable{{14, 14}},
				// TODO: add missing NH types when upgraded to 5.2
				new UnfetchedLazyProperty(),
				new UnknownBackrefProperty()
			};
			var expectedJson =
				"{\"$t\":\"oa\",\"$vs\":[" +
					"{\"$t\":\"s\",\"$v\":1}," +
					"{\"$t\":\"us\",\"$v\":2}," +
					"{\"$t\":\"i\",\"$v\":3}," +
					"{\"$t\":\"ui\",\"$v\":4}," +
					"5," +
					"{\"$t\":\"ul\",\"$v\":6}," +
					"{\"$t\":\"sb\",\"$v\":7}," +
					"{\"$t\":\"b\",\"$v\":8}," +
					"{\"$t\":\"d\",\"$v\":9.1}," +
					"{\"$t\":\"f\",\"$v\":10.2}," +
					"11.3," +
					"{\"$t\":\"g\",\"$v\":\"00000000-0000-0000-0000-000000000000\"}," +
					"{\"$t\":\"c\",\"$v\":\"a\"}," +
					"{\"$t\":\"ts\",\"$v\":\"00:00:00.0001234\"}," +
					"{\"$t\":\"do\",\"$v\":\"1970-01-01T00:01:40+00:00\"}," +
					"\"2000-01-01T00:00:00Z\"," +
					"{\"$t\":\"ba\",\"$v\":\"DA==\"}," +
					"{\"$t\":\"lo\",\"$vs\":[{\"$t\":\"i\",\"$v\":13}]}," +
					"{\"$t\":\"ht\",\"i:14\":{\"$t\":\"i\",\"$v\":14}}," +
					"{\"$t\":\"up\"}," +
					"{\"$t\":\"ub\"}" +
				"]}";
			var data = DefaultSerializer.Serialize(original);
			var json = Encoding.UTF8.GetString(data);
			Assert.That(json, Is.EqualTo(expectedJson));

			var copy = DefaultSerializer.Deserialize(data);

			AssertEqual(original, copy);
		}

		private ICacheSerializer CreateDefaultSerializer()
		{
			var serializer = new JsonCacheSerializer();
			serializer.RegisterType(typeof(CustomEntity), "cue");
			// Because of the type converter attribute, the default resolved json contract for NullableInt32 is JsonStringContract.
			serializer.RegisterType(typeof(NullableInt32), "nint", jsonContract =>
			{
				// Use fields instead of properties
				var contract = (JsonObjectContract) jsonContract;
				contract.Properties.Clear();
				var properties = typeof(NullableInt32).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
					.Select(f => new JsonProperty
					{
						PropertyName = f.Name,
						PropertyType = f.FieldType,
						DeclaringType = f.DeclaringType,
						ValueProvider = new ReflectionValueProvider(f),
						AttributeProvider = new ReflectionAttributeProvider(f),
						Readable = true,
						Writable = true
						
					});
				foreach (var property in properties)
				{
					contract.Properties.AddProperty(property);
				}
			}, typeof(JsonObjectContract));
			return serializer;
		}
	}
}
