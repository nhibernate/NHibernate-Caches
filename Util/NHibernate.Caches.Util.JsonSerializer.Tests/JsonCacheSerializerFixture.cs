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
		protected override Func<CacheSerializerBase> SerializerProvider => CreateDefaultSerializer;

		[Test]
		public void TestStrictSerialization()
		{
			var serializer = new JsonCacheSerializer();
			Assert.Throws<InvalidOperationException>(() => serializer.Serialize(new CustomEntity {Id = 10}),
				"Non standard types should be registered explicitly");
		}

		private CacheSerializerBase CreateDefaultSerializer()
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
