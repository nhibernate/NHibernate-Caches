using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;
using System.Xml;
using NHibernate.Cache;
using NHibernate.Cache.Entry;
using NHibernate.Engine;
using NHibernate.Intercept;
using NHibernate.Properties;
using NHibernate.Type;
using NSubstitute;
using NUnit.Framework;

namespace NHibernate.Caches.Common.Tests
{
	[TestFixture]
	public abstract class CacheSerializerFixture
	{
		protected abstract Func<CacheSerializerBase> SerializerProvider { get; }

		protected CacheSerializerBase DefaultSerializer { get; private set; }

		[OneTimeSetUp]
		public void FixtureSetup()
		{
			DefaultSerializer = SerializerProvider();
		}

		[Test]
		public void TestInteger()
		{
			var original = 15;
			var data = DefaultSerializer.Serialize(original);
			var copy = DefaultSerializer.Deserialize(data);

			Assert.That(copy, Is.TypeOf<int>());
			Assert.That(copy, Is.EqualTo(original));
		}

		[Test]
		public void TestObjectArray()
		{
			var original = GetAllNHibernateTypeValues();
			var data = DefaultSerializer.Serialize(original);
			var copy = (object[]) DefaultSerializer.Deserialize(data);
			AssertEqual(original, copy);
		}

		[Test]
		public void TestListOfObjects()
		{
			var original = CreateListOfObjects();
			var data = DefaultSerializer.Serialize(original);
			var copy = (List<object>) DefaultSerializer.Deserialize(data);
			AssertEqual(original, copy);
		}

		[Test]
		public void TestHashtableIntegerKey()
		{
			var original = CreateHashtable(i => i);
			var data = DefaultSerializer.Serialize(original);
			var copy = (Hashtable) DefaultSerializer.Deserialize(data);
			AssertEqual(original, copy);
		}

		[Test]
		public void TestHashtableGuidKey()
		{
			var original = CreateHashtable(i => Guid.NewGuid());
			var data = DefaultSerializer.Serialize(original);
			var copy = (Hashtable) DefaultSerializer.Deserialize(data);
			AssertEqual(original, copy);
		}

		[Test]
		public void TestHashtableStringKey()
		{
			var original = CreateHashtable(i => i.ToString());
			var data = DefaultSerializer.Serialize(original);
			var copy = (Hashtable) DefaultSerializer.Deserialize(data);
			AssertEqual(original, copy);
		}

		[Test]
		public void TestHashtableCharKey()
		{
			var original = CreateHashtable(i => (char) (64 + i));
			var data = DefaultSerializer.Serialize(original);
			var copy = (Hashtable) DefaultSerializer.Deserialize(data);
			AssertEqual(original, copy);
		}

		[Test]
		public void TestHashtableDateTime()
		{
			var original = CreateHashtable(i => DateTime.Now.AddDays(i));
			var data = DefaultSerializer.Serialize(original);
			var copy = (Hashtable) DefaultSerializer.Deserialize(data);
			AssertEqual(original, copy);
		}

		[Test]
		public void TestCustomObject()
		{
			var original = new CustomEntity {Id = 10};
			var data = DefaultSerializer.Serialize(original);
			var copy = (CustomEntity) DefaultSerializer.Deserialize(data);

			Assert.That(copy.Id, Is.EqualTo(original.Id));
		}

		[Test]
		public void TestNullableInt32Type()
		{
			var serializer = DefaultSerializer;
			var original = new object[] {NullableInt32.Default, new NullableInt32(32)};
			var data = serializer.Serialize(original);
			var copy = (object[]) serializer.Deserialize(data);
			AssertEqual(original, copy);
		}

		protected void AssertEqual(Hashtable original, Hashtable copy)
		{
			Assert.That(copy, Has.Count.EqualTo(original.Count));
			foreach (DictionaryEntry entry in original)
			{
				Assert.That(copy.ContainsKey(entry.Key), Is.True, $"Key {entry.Key} for value {entry.Value} was not found.");
				AssertEqual(entry.Value, copy[entry.Key]);
			}
		}

		protected void AssertEqual(object[] original, object[] copy)
		{
			Assert.That(copy, Has.Length.EqualTo(original.Length));
			for (var i = 0; i < original.Length; i++)
			{
				AssertEqual(original[i], copy[i]);
			}
		}

		protected void AssertEqual(List<object> original, List<object> copy)
		{
			Assert.That(copy, Has.Count.EqualTo(original.Count));
			for (var i = 0; i < original.Count; i++)
			{
				AssertEqual(original[i], copy[i]);
			}
		}

		protected void AssertEqual(object original, object copy)
		{
			if (original == null)
			{
				Assert.That(copy, Is.Null);
				return;
			}
			Assert.That(copy, Is.TypeOf(original.GetType()));
			Assert.That(copy, Is.EqualTo(original));
		}

		protected Hashtable CreateHashtable(Func<int, object> keyProvider)
		{
			var hashtable = new Hashtable();
			var values = GetAllNHibernateTypeValues();
			for (var i = 0; i < values.Length; i++)
			{
				hashtable.Add(keyProvider(i), values[i]);
			}

			return hashtable;
		}

		protected List<object> CreateListOfObjects()
		{
			return GetAllNHibernateTypeValues().ToList();
		}

		// TODO: make tests after upgraded to NHiberante 5.2
		protected CacheEntry CreateCacheEntry()
		{
			var types = GetNHibernateTypes();
			return new CacheEntry(types.Values.ToArray(), null, false, null, null, null);
		}

		// TODO: make tests after upgraded to NHiberante 5.2
		protected CollectionCacheEntry CreateCollectionCacheEntry()
		{
			return new CollectionCacheEntry(null, null);
		}

		// TODO: make tests after upgraded to NHiberante 5.2
		protected CacheLock CreateCacheLock()
		{
			return new CacheLock(1234, 1, 5);
		}

		// TODO: make tests after upgraded to NHiberante 5.2
		protected CachedItem CreateCachedItem(object data)
		{
			return new CachedItem(data, 111, 5);
		}

		// TODO: make tests after upgraded to NHiberante 5.2
		protected AnyType.ObjectTypeCacheEntry CreateObjectTypeCacheEntry()
		{
			return null;
		}

		[Serializable]
		public class CustomEntity
		{
			public int Id { get; set; }
		}

		protected Dictionary<IType, object> GetNHibernateTypes()
		{
			var entityName = nameof(CustomEntity);
			var xmlDoc = new XmlDocument();
			xmlDoc.LoadXml("<Root>XmlDoc</Root>");
			return new Dictionary<IType, object>
			{
				{NHibernateUtil.AnsiString, "test"},
				{NHibernateUtil.Binary, new byte[] {1, 2, 3, 4}},
				{NHibernateUtil.BinaryBlob, new byte[] {1, 2, 3, 4}},
				{NHibernateUtil.Boolean, true},
				{NHibernateUtil.Byte, (byte) 1},
				{NHibernateUtil.Character, 'a'},
				// TODO: enable after upgraded to NHiberante 5.2
				//{NHibernateUtil.CultureInfo, CultureInfo.CurrentCulture},
				{NHibernateUtil.DateTime, DateTime.Now},
				{NHibernateUtil.DateTimeNoMs, DateTime.Now},
				{NHibernateUtil.LocalDateTime, DateTime.Now},
				{NHibernateUtil.UtcDateTime, DateTime.UtcNow},
				{NHibernateUtil.LocalDateTimeNoMs, DateTime.Now},
				{NHibernateUtil.UtcDateTimeNoMs, DateTime.UtcNow},
				{NHibernateUtil.DateTimeOffset, DateTimeOffset.Now},
				{NHibernateUtil.Date, DateTime.Today},
				{NHibernateUtil.Decimal, 2.5m},
				{NHibernateUtil.Double, 2.5d},
				{NHibernateUtil.Currency, 2.5m},
				{NHibernateUtil.Guid, Guid.NewGuid()},
				{NHibernateUtil.Int16, (short) 1},
				{NHibernateUtil.Int32, 3},
				{NHibernateUtil.Int64, 3L},
				{NHibernateUtil.SByte, (sbyte) 1},
				{NHibernateUtil.UInt16, (ushort) 1},
				{NHibernateUtil.UInt32, (uint) 1},
				{NHibernateUtil.UInt64, (ulong) 1},
				{NHibernateUtil.Single, 1.1f},
				{NHibernateUtil.String, "test"},
				{NHibernateUtil.StringClob, "test"},
				{NHibernateUtil.Time, DateTime.Now},
				{NHibernateUtil.Ticks, DateTime.Now},
				{NHibernateUtil.TimeAsTimeSpan, TimeSpan.FromMilliseconds(15)},
				{NHibernateUtil.TimeSpan, TimeSpan.FromMilliseconds(1234)},
				{NHibernateUtil.DbTimestamp, DateTime.Now},
				{NHibernateUtil.TrueFalse, false},
				{NHibernateUtil.YesNo, true},
				// TODO: enable after upgraded to NHiberante 5.2
				//{NHibernateUtil.Class, typeof(IType)},
				{NHibernateUtil.ClassMetaType, entityName},
				{NHibernateUtil.Serializable, new CustomEntity {Id = 1}},
				// TODO: enable after upgraded to NHiberante 5.2
				//{NHibernateUtil.Object, new CustomEntity {Id = 10}},
				{NHibernateUtil.AnsiChar, 'a'},
				// TODO: enable after upgraded to NHiberante 5.2
				//{NHibernateUtil.XmlDoc, xmlDoc},
				//{NHibernateUtil.XDoc, XDocument.Parse("<Root>XDoc</Root>")},
				//{NHibernateUtil.Uri, new Uri("http://test.com")}
			};
		}

		protected object[] GetAllNHibernateTypeValues()
		{
			var types = GetNHibernateTypes();
			var sessionImpl = Substitute.For<ISessionImplementor>();
			sessionImpl.BestGuessEntityName(Arg.Any<object>()).Returns(o => o[0].GetType().Name);
			sessionImpl.GetContextEntityIdentifier(Arg.Is<object>(o => o is CustomEntity)).Returns(o => ((CustomEntity) o[0]).Id);
			return TypeHelper.Disassemble(
					types.Values.ToArray(),
					types.Keys.Cast<ICacheAssembler>().ToArray(),
					null,
					sessionImpl,
					null)
				.Concat(
					new[]
					{
						LazyPropertyInitializer.UnfetchedProperty,
						BackrefPropertyAccessor.Unknown,
						null
					})
				.ToArray();
		}


		[Serializable, TypeConverter(typeof(NullableInt32Converter))]
		public struct NullableInt32 : IComparable
		{
			public static readonly NullableInt32 Default = new NullableInt32();

			private readonly int _value;

			public NullableInt32(int value)
			{
				_value = value;
				HasValue = true;
			}

			public int CompareTo(object obj)
			{
				if (!(obj is NullableInt32 value))
				{
					throw new ArgumentException("NullableInt32 can only compare to another NullableInt32 or a System.Int32");
				}

				if (value.HasValue == HasValue)
				{
					return HasValue ? Value.CompareTo(value.Value) : 0;
				}
				return HasValue ? 1 : -1;
			}

			public bool HasValue { get; }

			public int Value
			{
				get
				{
					if (HasValue)
						return _value;
					throw new InvalidOperationException("Nullable type must have a value.");
				}
			}

			public static explicit operator int(NullableInt32 nullable)
			{
				if (!nullable.HasValue)
					throw new NullReferenceException();

				return nullable.Value;
			}

			public static implicit operator NullableInt32(int value)
			{
				return new NullableInt32(value);
			}

			public override string ToString()
			{
				return HasValue ? Value.ToString() : string.Empty;
			}

			public override int GetHashCode()
			{
				return HasValue ? Value.GetHashCode() : 0;
			}

			public override bool Equals(object obj)
			{
				if (obj is NullableInt32 int32)
				{
					return Equals(int32);
				}
				return false;
			}

			public bool Equals(NullableInt32 x)
			{
				return Equals(this, x);
			}

			public static bool Equals(NullableInt32 x, NullableInt32 y)
			{
				if (x.HasValue != y.HasValue)
					return false;
				if (x.HasValue)
					return x.Value == y.Value;
				return true;
			}
		}

		public class NullableInt32Converter : TypeConverter
		{
			public override bool CanConvertFrom(ITypeDescriptorContext context, System.Type sourceType)
			{
				return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
			}

			public override bool CanConvertTo(ITypeDescriptorContext context, System.Type destinationType)
			{
				return destinationType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destinationType);
			}

			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				switch (value)
				{
					case null:
						return NullableInt32.Default;
					case string _:
						var stringValue = ((string) value).Trim();

						if (stringValue == string.Empty)
							return NullableInt32.Default;

						//get underlying types converter
						var converter = TypeDescriptor.GetConverter(typeof(int));
						var newValue = (int) converter.ConvertFromString(context, culture, stringValue);
						return new NullableInt32(newValue);
					default:
						return base.ConvertFrom(context, culture, value);
				}
			}

			public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, System.Type destinationType)
			{
				if (destinationType != typeof(InstanceDescriptor) || !(value is NullableInt32))
				{
					return base.ConvertTo(context, culture, value, destinationType);
				}

				var nullable = (NullableInt32) value;

				var constructorArgTypes = new[] {typeof(int)};
				var constructor = typeof(NullableInt32).GetConstructor(constructorArgTypes);

				if (constructor == null)
				{
					return base.ConvertTo(context, culture, value, destinationType);
				}

				var constructorArgValues = new object[] {nullable.Value};
				return new InstanceDescriptor(constructor, constructorArgValues);
			}

			public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
			{
				return new NullableInt32((int) propertyValues["Value"]);
			}

			public override bool GetCreateInstanceSupported(ITypeDescriptorContext context)
			{
				return true;
			}

			public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value,
																	   Attribute[] attributes)
			{
				return TypeDescriptor.GetProperties(typeof(NullableInt32), attributes);
			}

			public override bool GetPropertiesSupported(ITypeDescriptorContext context)
			{
				return true;
			}
		}
	}
}
