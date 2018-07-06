using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NHibernate.Cache;
using NHibernate.Cache.Entry;
using NHibernate.Caches.Common;
using NHibernate.Intercept;
using NHibernate.Properties;
using NHibernate.Type;
using NHibernate.UserTypes;
using Serializer = Newtonsoft.Json.JsonSerializer;

namespace NHibernate.Caches.Util.JsonSerializer
{
	/// <summary>
	/// A serializer that uses Json.Net to serialize the data that will be stored in a distributed cache.
	/// If the cached <see cref="IUserType"/> have a <see cref="IUserType.Disassemble"/> method which does
	/// not yield only basic value types or array of them, their return type has to be registered explicitly
	/// by <see cref="RegisterType(System.Type, string)"/> method.
	/// </summary>
	public class JsonCacheSerializer : CacheSerializerBase
	{
		private const string TypeMetadataName = "$type";
		private const string ShortTypeMetadataName = "$t";
		private const string ValueMetadataName = "$value";
		private const string ShortValueMetadataName = "$v";
		private const string ValuesMetadataName = "$values";
		private const string ShortValuesMetadataName = "$vs";

		/// <summary>
		/// Types that are serialized as number or string and need the type metadata property to be correctly deserialized.
		/// </summary>
		/// <remarks>
		/// When deserializing an array of objects all numbers will be deserialized as double or long by default.
		/// In order to prevent that we have to add the type metadata so that Json.Net will correctly deserialize
		/// the number to the original type.
		/// </remarks>
		private static readonly Dictionary<System.Type, string> ExplicitTypes = new Dictionary<System.Type, string>
		{
			// Serialized as number
			{typeof(short), "s"},
			{typeof(ushort), "us"},
			{typeof(int), "i"},
			{typeof(uint), "ui"},
			{typeof(ulong), "ul"},
			{typeof(sbyte), "sb"},
			{typeof(byte), "b"},
			{typeof(decimal), "d"},
			{typeof(float), "f"},
			// Serialized as string
			{typeof(Guid), "g"},
			{typeof(char), "c"},
			{typeof(TimeSpan), "ts"},
			{typeof(DateTimeOffset), "do"},
			{typeof(byte[]), "ba"}
		};

		// The types that are allowed to be serialized and deserialized in order to prevent
		// exposing any security vulnerability as we are not using TypeNameHandling.None
		private static readonly Dictionary<System.Type, string> TypeAliases =
			new Dictionary<System.Type, string>(ExplicitTypes)
			{
				// Added for completeness, they will never be requested by Json.NET
				{typeof(long), "l"},
				{typeof(double), "db"},
				{typeof(DateTime), "dt"},

				// Used by NHibernate
				{typeof(object[]), "oa"},
				{typeof(List<object>), "lo"},
				{typeof(Hashtable), "ht"},
				{typeof(CacheEntry), "ce"},
				{typeof(CacheLock), "cl"},
				{typeof(CachedItem), "ci"},
				{typeof(CollectionCacheEntry), "cc"},
				{typeof(AnyType.ObjectTypeCacheEntry), "at"},
				{typeof(UnfetchedLazyProperty), "up"},
				{typeof(UnknownBackrefProperty), "ub"}
			};

		private readonly Serializer _serializer;
		private readonly ExplicitSerializationBinder _serializationBinder = new ExplicitSerializationBinder();
		private readonly CustomDefaultContractResolver _contractResolver = new CustomDefaultContractResolver();

		/// <inheritdoc />
		public JsonCacheSerializer()
		{
			var settings = new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.Auto,
				DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
				Formatting = Formatting.None,
				SerializationBinder = _serializationBinder,
				ContractResolver = _contractResolver
			};
			settings.Converters.Add(new ExplicitTypesConverter());
			// Setup the Hashtable serialization
			var contract = settings.ContractResolver.ResolveContract(typeof(Hashtable));
			contract.Converter = new HashtableConverter();
			contract.OnDeserializedCallbacks.Add((o, context) => HashtableConverter.OnDeserialized(o, _serializer));

			_serializer = Serializer.Create(settings);
		}

		/// <summary>
		/// Register a type that is allowed to be serialized with an alias.
		/// </summary>
		/// <param name="type">The type allowed to be serialized.</param>
		/// <param name="alias">The shorten name of the type.</param>
		public void RegisterType(System.Type type, string alias)
		{
			RegisterType(type, alias, null, null);
		}

		/// <summary>
		/// Register a type that is allowed to be serialized with an alias.
		/// </summary>
		/// <param name="type">The type allowed to be serialized.</param>
		/// <param name="alias">The shorten name of the type.</param>
		/// <param name="setupContractAction">The action to setup the <see cref="JsonContract"/> of the type.</param>
		public void RegisterType(System.Type type, string alias, Action<JsonContract> setupContractAction)
		{
			RegisterType(type, alias, setupContractAction, null);
		}

		/// <summary>
		/// Register a type that is allowed to be serialized with an alias.
		/// </summary>
		/// <param name="type">The type allowed to be serialized.</param>
		/// <param name="alias">The shorten name of the type.</param>
		/// <param name="setupContractAction">The action to setup the <see cref="JsonContract"/> of the type.</param>
		/// <param name="contractType">The concrete type of a <see cref="JsonContract"/> to use for the type.</param>
		public void RegisterType(System.Type type, string alias, Action<JsonContract> setupContractAction, System.Type contractType)
		{
			if (type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}
			_serializationBinder.RegisterType(type, alias);
			if (contractType != null)
			{
				_contractResolver.SetTypeContract(type, contractType);
			}
			setupContractAction?.Invoke(_serializer.ContractResolver.ResolveContract(type));
		}

		/// <inheritdoc />
		public override object Deserialize(byte[] value)
		{
			using (var reader = new CustomJsonTextReader(new StringReader(Encoding.UTF8.GetString(value))))
			{
				return _serializer.Deserialize(reader);
			}
		}

		/// <inheritdoc />
		public override byte[] Serialize(object value)
		{
			using (var stringWriter = new StringWriter(new StringBuilder(256), CultureInfo.InvariantCulture))
			using (var writer = new CustomJsonTextWriter(stringWriter))
			{
				writer.Formatting = _serializer.Formatting;
				_serializer.Serialize(writer, value, typeof(object));
				return Encoding.UTF8.GetBytes(stringWriter.ToString());
			}
		}

		/// <summary>
		/// Allows to override the default <see cref="JsonContract"/> that will be created for a given type.
		/// </summary>
		private class CustomDefaultContractResolver : DefaultContractResolver
		{
			private static readonly Dictionary<System.Type, Func<CustomDefaultContractResolver, System.Type, JsonContract>>
				ContractCreators = new Dictionary<System.Type, Func<CustomDefaultContractResolver, System.Type, JsonContract>>
				{
					{typeof(JsonObjectContract), (r, t) => r.CreateObjectContract(t)},
					{typeof(JsonArrayContract), (r, t) => r.CreateArrayContract(t)},
					{typeof(JsonDictionaryContract), (r, t) => r.CreateDictionaryContract(t)},
					{typeof(JsonDynamicContract), (r, t) => r.CreateDynamicContract(t)},
					{typeof(JsonISerializableContract), (r, t) => r.CreateISerializableContract(t)},
					{typeof(JsonLinqContract), (r, t) => r.CreateLinqContract(t)},
					{typeof(JsonPrimitiveContract), (r, t) => r.CreatePrimitiveContract(t)},
					{typeof(JsonStringContract), (r, t) => r.CreateStringContract(t)}
				};

			private readonly Dictionary<System.Type, System.Type> _typeContracts = new Dictionary<System.Type, System.Type>();

			public void SetTypeContract(System.Type type, System.Type contractType)
			{
				if (!ContractCreators.ContainsKey(contractType))
				{
					throw new InvalidOperationException(
						$"Invalid json contract type {contractType}. List of valid json contract types: {string.Join(", ", ContractCreators.Keys)}");
				}

				if (_typeContracts.ContainsKey(type))
				{
					throw new InvalidOperationException($"A json contract type was already set for type {type}");
				}
				_typeContracts.Add(type, contractType);
			}

			protected override JsonContract CreateContract(System.Type objectType)
			{
				return !_typeContracts.TryGetValue(objectType, out var contractType)
					? base.CreateContract(objectType)
					: ContractCreators[contractType](this, objectType);
			}
		}

		/// <summary>
		/// A serialization binder that allows only registered types to be serialized.
		/// </summary>
		private class ExplicitSerializationBinder : ISerializationBinder
		{
			private readonly Dictionary<System.Type, string> _typeAliases;
			private readonly Dictionary<string, System.Type> _aliasTypes;

			public ExplicitSerializationBinder()
			{
				_typeAliases = new Dictionary<System.Type, string>(TypeAliases);
				_aliasTypes = _typeAliases.ToDictionary(o => o.Value, o => o.Key);
			}

			public void RegisterType(System.Type type, string alias)
			{
				if (string.IsNullOrEmpty(alias))
				{
					alias = type.AssemblyQualifiedName;
				}

				if (alias == null)
				{
					throw new ArgumentNullException(nameof(alias));
				}

				if (_typeAliases.ContainsKey(type))
				{
					throw new InvalidOperationException($"Type {type} is already registered.");
				}

				if (_aliasTypes.ContainsKey(alias))
				{
					throw new InvalidOperationException($"Alias {alias} is already registered.");
				}

				_typeAliases.Add(type, alias);
				_aliasTypes.Add(alias, type);
			}

			public void BindToName(System.Type serializedType, out string assemblyName, out string typeName)
			{
				if (!_typeAliases.TryGetValue(serializedType, out typeName))
				{
					throw new InvalidOperationException(
						$"Unknown type '{serializedType.AssemblyQualifiedName}', use JsonCacheSerializer.RegisterType method to register it.");
				}

				assemblyName = null;
			}

			public System.Type BindToType(string assemblyName, string typeName)
			{
				if (!_aliasTypes.TryGetValue(typeName, out var type))
				{
					throw new InvalidOperationException(
						$"Unknown type '{typeName}, {assemblyName}', use JsonCacheSerializer.RegisterType method to register it.");
				}

				return type;
			}
		}

		/// <summary>
		/// A <see cref="Hashtable"/> converter that preserves the key type. <see cref="ReadJson"/> is not
		/// implemented because it is not called by the serializer when the <see cref="Hashtable"/> is located on
		/// a property of type <see cref="object"/>, which can happen when wrapping the value in <see cref="CachedItem"/>.
		/// Instead, the <see cref="OnDeserialized"/> method should be appended to the <see cref="JsonContract.OnDeserializedCallbacks"/>
		/// of the <see cref="Hashtable"/> <see cref="JsonContract"/>.
		/// </summary>
		private class HashtableConverter : JsonConverter<Hashtable>
		{
			/// <inheritdoc />
			public override void WriteJson(JsonWriter writer, Hashtable value, Serializer serializer)
			{
				writer.WriteStartObject();
				writer.WritePropertyName(ShortTypeMetadataName);
				writer.WriteValue(TypeAliases[typeof(Hashtable)]);

				foreach (DictionaryEntry entry in value)
				{
					var type = entry.Key.GetType();
					if (type == typeof(string))
					{
						writer.WritePropertyName(entry.Key.ToString());
						serializer.Serialize(writer, entry.Value, typeof(object));
					}
					else
					{
						serializer.SerializationBinder.BindToName(type, out var assemblyName, out var typeName);
						writer.WritePropertyName(string.IsNullOrEmpty(assemblyName)
							? $"{typeName}:{JsonConvert.ToString(entry.Key)}"
							: $"{typeName};{assemblyName}:{JsonConvert.ToString(entry.Key)}");
						serializer.Serialize(writer, entry.Value, typeof(object));
					}
				}
				writer.WriteEndObject();
			}

			/// <inheritdoc />
			public override Hashtable ReadJson(JsonReader reader, System.Type objectType, Hashtable existingValue, bool hasExistingValue,
				Serializer serializer)
			{
				throw new NotSupportedException();
			}

			public static void OnDeserialized(object o, Serializer serializer)
			{
				var hashtable = (Hashtable) o;
				var keys = hashtable.Keys.Cast<string>().ToList();
				foreach (var key in keys)
				{
					var index = key.IndexOf(':');
					if (index < 0)
					{
						// Key is a string
						continue;
					}

					var typeAssembly = key.Substring(0, index).Split(';');
					var type = serializer.SerializationBinder.BindToType(
						typeAssembly.Length > 1 ? typeAssembly[1] : null,
						typeAssembly[typeAssembly.Length - 1]);
					var keyString = key.Substring(index + 1);
					var keyValue = serializer.Deserialize(new StringReader(keyString), type);

					hashtable.Add(keyValue, hashtable[key]);
					hashtable.Remove(key);
				}
			}

			/// <inheritdoc />
			public override bool CanRead => false;
		}

		/// <summary>
		/// A json converter that adds the type metadata for <see cref="ExplicitTypes"/>.
		/// </summary>
		private class ExplicitTypesConverter : JsonConverter
		{
			public override bool CanConvert(System.Type objectType)
			{
				return ExplicitTypes.ContainsKey(objectType);
			}

			public override bool CanRead => false;

			public override object ReadJson(JsonReader reader, System.Type objectType, object existingValue, Serializer serializer)
			{
				throw new NotSupportedException();
			}

			public override void WriteJson(JsonWriter writer, object value, Serializer serializer)
			{
				writer.WriteStartObject();
				writer.WritePropertyName(ShortTypeMetadataName);
				var typeName = ExplicitTypes[value.GetType()];
				writer.WriteValue(typeName);
				writer.WritePropertyName(ShortValueMetadataName);
				writer.WriteValue(value);
				writer.WriteEndObject();
			}
		}

		#region Reader & Writer

		/// <summary>
		/// Restores the metadata property names modified by <see cref="CustomJsonTextWriter"/>.
		/// </summary>
		private class CustomJsonTextReader : JsonTextReader
		{
			public CustomJsonTextReader(TextReader reader) : base(reader)
			{
			}

			public override bool Read()
			{
				var hasToken = base.Read();
				if (!hasToken || TokenType != JsonToken.PropertyName || !(Value is string str))
				{
					return hasToken;
				}

				switch (str)
				{
					case ShortTypeMetadataName:
						SetToken(JsonToken.PropertyName, TypeMetadataName);
						break;
					case ShortValueMetadataName:
						SetToken(JsonToken.PropertyName, ValueMetadataName);
						break;
					case ShortValuesMetadataName:
						SetToken(JsonToken.PropertyName, ValuesMetadataName);
						break;
				}

				return true;
			}
		}

		/// <summary>
		/// Reduces the json size by shortening the metadata properties names.
		/// </summary>
		private class CustomJsonTextWriter : JsonTextWriter
		{
			public CustomJsonTextWriter(TextWriter textWriter) : base(textWriter)
			{
			}

			public override void WritePropertyName(string name, bool escape)
			{
				switch (name)
				{
					case TypeMetadataName:
						name = ShortTypeMetadataName;
						break;
					case ValueMetadataName:
						name = ShortValueMetadataName;
						break;
					case ValuesMetadataName:
						name = ShortValuesMetadataName;
						break;
				}

				base.WritePropertyName(name, escape);
			}
		}

		#endregion

	}
}
