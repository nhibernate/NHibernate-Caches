using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
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

		[Test]
		public void TestCacheEntry()
		{
			var original = CreateCacheEntry();
			var data = DefaultSerializer.Serialize(original);
			var copy = (CacheEntry) DefaultSerializer.Deserialize(data);
			AssertEqual(original, copy);
		}

		[Test]
		public void TestCollectionCacheEntry()
		{
			var original = CreateCollectionCacheEntry();
			var data = DefaultSerializer.Serialize(original);
			var copy = (CollectionCacheEntry) DefaultSerializer.Deserialize(data);
			AssertEqual(original, copy);
		}

		[Test]
		public void TestCacheLock()
		{
			var original = CreateCacheLock();
			var data = DefaultSerializer.Serialize(original);
			var copy = (CacheLock) DefaultSerializer.Deserialize(data);
			AssertEqual(original, copy);
		}

		[Test]
		public void TestCachedItem()
		{
			// CacheEntry
			var original = CreateCachedItem(CreateCacheEntry());
			var data = DefaultSerializer.Serialize(original);
			var copy = (CachedItem) DefaultSerializer.Deserialize(data);
			AssertEqual(original, copy);

			// CollectionCacheEntry
			original = CreateCachedItem(CreateCollectionCacheEntry());
			data = DefaultSerializer.Serialize(original);
			copy = (CachedItem) DefaultSerializer.Deserialize(data);
			AssertEqual(original, copy);
		}

		[Test]
		public void TestAnyTypeObjectTypeCacheEntry()
		{
			var original = CreateObjectTypeCacheEntry();
			var data = DefaultSerializer.Serialize(original);
			var copy = (AnyType.ObjectTypeCacheEntry) DefaultSerializer.Deserialize(data);
			AssertEqual(original, copy);
		}

		protected void AssertEqual(CacheEntry original, CacheEntry copy)
		{
			Assert.That(copy.Version, Is.EqualTo(original.Version));
			Assert.That(copy.Version, Is.TypeOf(original.Version.GetType()));
			Assert.That(copy.Subclass, Is.EqualTo(original.Subclass));
			Assert.That(copy.AreLazyPropertiesUnfetched, Is.EqualTo(original.AreLazyPropertiesUnfetched));
			for (var i = 0; i < copy.DisassembledState.Length; i++)
			{
				if (original.DisassembledState[i] == null)
				{
					Assert.That(copy.DisassembledState[i], Is.Null);
					continue;
				}

				Assert.That(copy.DisassembledState[i], Is.TypeOf(original.DisassembledState[i].GetType()));
				if (original.DisassembledState[i] is AnyType.ObjectTypeCacheEntry originalAnyType)
				{
					var copyAnyType = (AnyType.ObjectTypeCacheEntry) copy.DisassembledState[i];
					AssertEqual(originalAnyType, copyAnyType);
				}
				else
				{
					Assert.That(copy.DisassembledState[i], Is.EqualTo(original.DisassembledState[i]));
				}
			}
		}

		protected void AssertEqual(CachedItem original, CachedItem copy)
		{
			Assert.That(copy.Version, Is.EqualTo(original.Version));
			Assert.That(copy.Version, Is.TypeOf(original.Version.GetType()));
			Assert.That(copy.Value, Is.TypeOf(original.Value.GetType()));
			switch (original.Value)
			{
				case CacheEntry cacheEntry:
					AssertEqual(cacheEntry, (CacheEntry) copy.Value);
					break;
				case CollectionCacheEntry colleectionCacheEntry:
					AssertEqual(colleectionCacheEntry, (CollectionCacheEntry) copy.Value);
					break;
				default:
					Assert.That(copy.Value, Is.EqualTo(original.Value));
					break;
			}
			Assert.That(copy.FreshTimestamp, Is.EqualTo(original.FreshTimestamp));
		}

		protected void AssertEqual(CollectionCacheEntry original, CollectionCacheEntry copy)
		{
			Assert.That(copy.State, Is.TypeOf(original.State.GetType()));

			var originalArray = original.State;
			var copyArray = copy.State;

			for (var i = 0; i < copyArray.Length; i++)
			{
				if (originalArray[i] == null)
				{
					Assert.That(copyArray[i], Is.Null);
					continue;
				}

				Assert.That(copyArray[i], Is.TypeOf(originalArray[i].GetType()));
				if (originalArray[i] is AnyType.ObjectTypeCacheEntry originalAnyType)
				{
					var copyAnyType = (AnyType.ObjectTypeCacheEntry) copyArray[i];
					AssertEqual(originalAnyType, copyAnyType);
				}
				else
				{
					Assert.That(copyArray[i], Is.EqualTo(originalArray[i]));
				}
			}
		}

		protected void AssertEqual(CacheLock original, CacheLock copy)
		{
			Assert.That(copy.Version, Is.EqualTo(original.Version));
			Assert.That(copy.Version, Is.TypeOf(original.Version.GetType()));
			Assert.That(copy.Id, Is.EqualTo(original.Id));
			Assert.That(copy.Multiplicity, Is.EqualTo(original.Multiplicity));
			Assert.That(copy.Timeout, Is.EqualTo(original.Timeout));
			Assert.That(copy.UnlockTimestamp, Is.EqualTo(original.UnlockTimestamp));
			Assert.That(copy.WasLockedConcurrently, Is.EqualTo(original.WasLockedConcurrently));
		}

		protected void AssertEqual(AnyType.ObjectTypeCacheEntry original, AnyType.ObjectTypeCacheEntry copy)
		{
			Assert.That(copy.Id, Is.EqualTo(original.Id));
			Assert.That(copy.EntityName, Is.EqualTo(original.EntityName));
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

			if (original is AnyType.ObjectTypeCacheEntry anyCacheEntry)
			{
				Assert.That(copy, Is.TypeOf<AnyType.ObjectTypeCacheEntry>());
				AssertEqual(anyCacheEntry, (AnyType.ObjectTypeCacheEntry) copy);
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

		protected CacheEntry CreateCacheEntry()
		{
			return new CacheEntry
			{
				DisassembledState = GetAllNHibernateTypeValues(),
				Version = 1,
				Subclass = "TestClass",
				AreLazyPropertiesUnfetched = true
			};
		}

		protected CollectionCacheEntry CreateCollectionCacheEntry()
		{
			return new CollectionCacheEntry
			{
				State = GetAllNHibernateTypeValues()
			};
		}

		protected CacheLock CreateCacheLock()
		{
			return new CacheLock
			{
				Timeout = 1234, Id = 1, Version = 5
			};
		}

		protected CachedItem CreateCachedItem(object data)
		{
			return new CachedItem
			{
				Value = data, FreshTimestamp = 111, Version = 5
			};
		}

		protected AnyType.ObjectTypeCacheEntry CreateObjectTypeCacheEntry()
		{
			return new AnyType.ObjectTypeCacheEntry
			{
				EntityName = "Test",
				Id = 1
			};
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
				{NHibernateUtil.CultureInfo, CultureInfo.CurrentCulture},
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
				{NHibernateUtil.Class, typeof(IType)},
				{NHibernateUtil.MetaType, entityName},
				{NHibernateUtil.Serializable, new CustomEntity {Id = 1}},
				{NHibernateUtil.Object, new CustomEntity {Id = 10}},
				{NHibernateUtil.AnsiChar, 'a'},
				{NHibernateUtil.XmlDoc, xmlDoc},
				{NHibernateUtil.XDoc, XDocument.Parse("<Root>XDoc</Root>")},
				{NHibernateUtil.Uri, new Uri("http://test.com")}
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
