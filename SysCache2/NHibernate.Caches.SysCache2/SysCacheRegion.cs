using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using NHibernate.Cache;
using NHibernate.Util;
using Environment = NHibernate.Cfg.Environment;

namespace NHibernate.Caches.SysCache2
{
	// 6.0 TODO: replace that class by its base
	/// <summary>
	/// Pluggable cache implementation using the System.Web.Caching classes and handling SQL dependencies.
	/// </summary>
	public class SysCacheRegion : SysCacheRegionBase,
#pragma warning disable 618
		ICache
#pragma warning restore 618
	{
		/// <summary>
		/// Default constructor.
		/// </summary>
		public SysCacheRegion()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SysCacheRegion"/> class with the default configuration
		/// properties.
		/// </summary>
		/// <param name="name">The name of the region.</param>
		/// <param name="additionalProperties">Additional NHibernate configuration properties.</param>
		public SysCacheRegion(string name, IDictionary<string, string> additionalProperties)
			: base(name, additionalProperties)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SysCacheRegion"/> class.
		/// </summary>
		/// <param name="name">The name of the region.</param>
		/// <param name="settings">The configuration settings for the cache region.</param>
		/// <param name="additionalProperties">Additional NHibernate configuration properties.</param>
		public SysCacheRegion(string name, CacheRegionElement settings,
			IDictionary<string, string> additionalProperties)
			: base(name, settings, additionalProperties)
		{
		}

		/// <inheritdoc />
		public new Task<object> GetAsync(object key, CancellationToken cancellationToken)
			=> base.GetAsync(key, cancellationToken);

		/// <inheritdoc />
		public new Task PutAsync(object key, object value, CancellationToken cancellationToken)
			=> base.PutAsync(key, value, cancellationToken);

		/// <inheritdoc />
		public new Task RemoveAsync(object key, CancellationToken cancellationToken)
			=> base.RemoveAsync(key, cancellationToken);

		/// <inheritdoc />
		public new Task ClearAsync(CancellationToken cancellationToken)
			=> base.ClearAsync(cancellationToken);

		/// <inheritdoc />
		public new Task LockAsync(object key, CancellationToken cancellationToken)
			=> base.LockAsync(key, cancellationToken);

		/// <inheritdoc />
		public Task UnlockAsync(object key, CancellationToken cancellationToken)
			=> base.UnlockAsync(key, null, cancellationToken);

		/// <inheritdoc />
		public new string RegionName => base.RegionName;

		/// <inheritdoc />
		public new object Get(object key)
			=> base.Get(key);

		/// <inheritdoc />
		public new void Put(object key, object value)
			=> base.Put(key, value);

		/// <inheritdoc />
		public new void Remove(object key)
			=> base.Remove(key);

		/// <inheritdoc />
		public new void Clear()
			=> base.Clear();

		/// <inheritdoc />
		public new void Destroy()
			=> base.Destroy();

		/// <inheritdoc />
		public new void Lock(object key)
			=> base.Lock(key);

		/// <inheritdoc />
		public void Unlock(object key)
			=> base.Unlock(key, null);

		/// <inheritdoc />
		public new long NextTimestamp()
			=> base.NextTimestamp();

		/// <inheritdoc />
		public new int Timeout => base.Timeout;
	}

	/// <summary>
	/// Pluggable cache implementation using the System.Web.Caching classes and handling SQL dependencies.
	/// </summary>
	public abstract class SysCacheRegionBase : CacheBase
	{
		/// <summary>The name of the cache prefix to differentiate the nhibernate cache elements from
		/// other items in the cache.</summary>
		private const string _cacheKeyPrefix = "NHibernate-Cache:";

		/// <summary>The default expiration to use if one is not specified.</summary>
		private static readonly TimeSpan DefaultRelativeExpiration = TimeSpan.FromSeconds(300);
		private const bool _defaultUseSlidingExpiration = false;

		/// <summary>Log4net logger for the class.</summary>
		private static readonly INHibernateLogger Log = NHibernateLogger.For(typeof(SysCacheRegion));

		/// <summary>
		/// List of dependencies that need to be enlisted before being hooked to a cache item.
		/// </summary>
		private readonly List<ICacheDependencyEnlister> _dependencyEnlisters = new List<ICacheDependencyEnlister>();

		/// <summary>The name of the cache region.</summary>
		private readonly string _name;

		/// <summary>The name of the cache key for the region.</summary>
		private readonly string _rootCacheKey;

		/// <summary>The cache for the web application.</summary>
		private readonly System.Web.Caching.Cache _webCache;

		/// <summary>Indicates if the root cache item has been stored or not.</summary>
		private bool _isRootItemCached;

		/// <summary>The priority of the cache item.</summary>
		private CacheItemPriority _priority;

		/// <summary>Relative expiration for the cache items.</summary>
		private TimeSpan? _relativeExpiration;
		private bool _useSlidingExpiration;

		/// <summary>Time of day expiration for the cache items.</summary>
		private TimeSpan? _timeOfDayExpiration;

		/// <summary>
		/// Initializes a new instance of the <see cref="SysCacheRegion"/> class with
		/// the default region name and configuration properties.
		/// </summary>
		public SysCacheRegionBase() : this(null, null, null) {}

		/// <summary>
		/// Initializes a new instance of the <see cref="SysCacheRegion"/> class with the default configuration
		/// properties.
		/// </summary>
		/// <param name="name">The name of the region.</param>
		/// <param name="additionalProperties">Additional NHibernate configuration properties.</param>
		public SysCacheRegionBase(string name, IDictionary<string, string> additionalProperties)
			: this(name, null, additionalProperties) {}

		/// <summary>
		/// Initializes a new instance of the <see cref="SysCacheRegion"/> class.
		/// </summary>
		/// <param name="name">The name of the region.</param>
		/// <param name="settings">The configuration settings for the cache region.</param>
		/// <param name="additionalProperties">Additional NHibernate configuration properties.</param>
		public SysCacheRegionBase(string name, CacheRegionElement settings, IDictionary<string, string> additionalProperties)
		{
			//validate the params
			if (string.IsNullOrEmpty(name))
			{
				Log.Info("No region name specified for cache region. Using default name of 'nhibernate'");
				name = "nhibernate";
			}

			_webCache = HttpRuntime.Cache;
			_name = name;

			//configure the cache region based on the configured settings and any relevant nhibernate settings
			Configure(settings, additionalProperties);

			//creaet the cache key that will be used for the root cache item which all other
			//cache items are dependent on
			_rootCacheKey = GenerateRootCacheKey();
		}

		#region CacheBase Members

		/// <inheritdoc />
		public override void Clear()
		{
			//remove the root cache item, this will cause all of the individual items to be removed from the cache
			_webCache.Remove(_rootCacheKey);
			_isRootItemCached = false;

			Log.Debug("All items cleared from the cache.");
		}

		/// <inheritdoc />
		public override void Destroy()
		{
			Clear();
		}

		/// <inheritdoc />
		public override object Get(object key)
		{
			if (key == null || _isRootItemCached == false)
			{
				return null;
			}

			//get the full key to use to locate the item in the cache
			var cacheKey = GetCacheKey(key);

			Log.Debug("Fetching object '{0}' from the cache.", cacheKey);

			var cachedObject = _webCache.Get(cacheKey);
			if (cachedObject == null)
			{
				return null;
			}

			//casting the object to a dictionary entry so we can verify that the item for the correct key was retrieved
			var entry = (DictionaryEntry) cachedObject;
			return key.Equals(entry.Key) ? entry.Value : null;
		}

		/// <inheritdoc />
		public override object Lock(object key)
		{
			//nothing to do here
			return null;
		}

		/// <inheritdoc />
		public override long NextTimestamp()
		{
			return Timestamper.Next();
		}

		/// <inheritdoc />
		[SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope")]
		public override void Put(object key, object value)
		{
			//validate the params
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			//If the root cache item is not cached then we should reestablish it now
			if (_isRootItemCached == false)
			{
				Log.Debug("root cache item for region not found.");

				CacheRootItem();
			}

			//get the full key for the cache key
			var cacheKey = GetCacheKey(key);

			if (Log.IsDebugEnabled())
			{
				Log.Debug(
					_webCache[cacheKey] != null
						? "updating value of key '{0}' to '{1}'."
						: "adding new data: key={0} & value={1}",
					cacheKey, value);
			}

			//get the expiration time for the cache item
			var expiration = GetCacheItemExpiration();
			var slidingExpiration = _useSlidingExpiration ? _relativeExpiration : null;

			if (Log.IsDebugEnabled())
			{
				if (expiration.HasValue)
				{
					Log.Debug("item will expire at: {0}", expiration);
				}
				else if (slidingExpiration.HasValue)
				{
					Log.Debug("item will expire in: {0} (sliding)", slidingExpiration);
				}
			}

			_webCache.Insert(
				cacheKey, new DictionaryEntry(key, value), new CacheDependency(null, new[] {_rootCacheKey}),
				expiration ?? System.Web.Caching.Cache.NoAbsoluteExpiration,
				slidingExpiration ?? System.Web.Caching.Cache.NoSlidingExpiration,
				_priority, null);
		}

		/// <inheritdoc />
		public override string RegionName => _name;

		/// <inheritdoc />
		public override void Remove(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			//get the full cache key
			var cacheKey = GetCacheKey(key);

			Log.Debug("removing item with key: {0}", cacheKey);

			_webCache.Remove(cacheKey);
		}

		/// <inheritdoc />
		public override int Timeout => Timestamper.OneMs * 60000;

		/// <inheritdoc />
		public override void Unlock(object key, object lockValue)
		{
			//nothing to do since we arent locking
		}

		#endregion

		/// <summary>
		/// Configures the cache region from configuration values.
		/// </summary>
		/// <param name="settings">Configuration settings for the region.</param>
		/// <param name="additionalProperties">The additional properties supplied by NHibernate engine.</param>
		private void Configure(CacheRegionElement settings, IDictionary<string, string> additionalProperties)
		{
			Log.Debug("Configuring cache region");

			// these are some default connection values that can be later used by the data dependencies
			// if no custom settings are specified
			string connectionName = null;
			string connectionString = null;
			var defaultExpiration = DefaultRelativeExpiration;
			_useSlidingExpiration = _defaultUseSlidingExpiration;

			if (additionalProperties != null)
			{
				// pick up connection settings that might be used later for data dependencis if any are specified
				if (additionalProperties.ContainsKey(Environment.ConnectionStringName))
				{
					connectionName = additionalProperties[Environment.ConnectionStringName];
				}

				if (additionalProperties.ContainsKey(Environment.ConnectionString))
				{
					connectionString = additionalProperties[Environment.ConnectionString];
				}

				if (!additionalProperties.TryGetValue("expiration", out var expirationString))
				{
					additionalProperties.TryGetValue(Environment.CacheDefaultExpiration, out expirationString);
				}

				if (expirationString != null)
				{
					try
					{
						var seconds = Convert.ToInt32(expirationString);
						defaultExpiration = TimeSpan.FromSeconds(seconds);
						Log.Debug("default expiration value: {0}", seconds);
					}
					catch (Exception ex)
					{
						Log.Error("error parsing expiration value '{0}'", expirationString);
						throw new ArgumentException($"could not parse expiration '{expirationString}' as a number of seconds", ex);
					}
				}

				_useSlidingExpiration = PropertiesHelper.GetBoolean("cache.use_sliding_expiration", additionalProperties, _defaultUseSlidingExpiration);
				Log.Debug("default sliding expiration value: {0}", _useSlidingExpiration);
			}

			if (settings != null)
			{
				_priority = settings.Priority;
				_timeOfDayExpiration = settings.TimeOfDayExpiration;
				_relativeExpiration = settings.RelativeExpiration;
				_useSlidingExpiration = settings.UseSlidingExpiration ?? _useSlidingExpiration;

				if (Log.IsDebugEnabled())
				{
					Log.Debug("using priority: {0:g}", settings.Priority);

					if (_relativeExpiration.HasValue)
					{
						Log.Debug("using relative expiration: {0}{1}", _relativeExpiration, _useSlidingExpiration ? " (sliding)" : string.Empty);
					}

					if (_timeOfDayExpiration.HasValue)
					{
						Log.Debug("using time of day expiration: {0}", _timeOfDayExpiration);
					}
				}

				CreateDependencyEnlisters(settings.Dependencies, connectionName, connectionString);
			}
			else
			{
				_priority = CacheItemPriority.Default;

				Log.Debug("no priority specified, using default: {0:g}", _priority);
			}

			//use the default expiration as no expiration was set
			if (_relativeExpiration.HasValue == false && _timeOfDayExpiration.HasValue == false)
			{
				_relativeExpiration = defaultExpiration;

				Log.Debug("no expiration specified, using default: {0}", _relativeExpiration);
			}
		}

		/// <summary>
		/// Creates the dependency enlisters for any dependecies that require notification enlistment.
		/// </summary>
		/// <param name="dependencyConfig">The settings for the dependencies.</param>
		/// <param name="defaultConnectionName">The connection name to use when setting up data dependencies if no connection string provider is specified.</param>
		/// <param name="defaultConnectionString">The default connection string to use for data dependencies if no connection string provider is specified.</param>
		private void CreateDependencyEnlisters(CacheDependenciesElement dependencyConfig, string defaultConnectionName,
		                                       string defaultConnectionString)
		{
			//dont do anything if there is no config
			if (dependencyConfig == null)
			{
				Log.Debug("no data dependencies specified");
				return;
			}

			//build the table dependency enlisters
			if (dependencyConfig.TableDependencies.Count > 0)
			{
				foreach (TableCacheDependencyElement tableConfig in dependencyConfig.TableDependencies)
				{
					Log.Debug("configuring sql table dependency, '{0}' using table, '{1}', and database entry. '{2}'",
					                 tableConfig.Name, tableConfig.TableName, tableConfig.DatabaseEntryName);

					var tableEnlister = new SqlTableCacheDependencyEnlister(tableConfig.TableName, tableConfig.DatabaseEntryName);

					_dependencyEnlisters.Add(tableEnlister);
				}
			}

			//build the command dependency enlisters
			if (dependencyConfig.CommandDependencies.Count <= 0)
				return;

			foreach (CommandCacheDependencyElement commandConfig in dependencyConfig.CommandDependencies)
			{
				//construct the correct connection string provider, we will do are best fallback to a connection string provider
				//that will help us find a connection string even if one isnt specified

				Log.Debug("configuring sql command dependency, '{0}', using command, '{1}'", commandConfig.Name, commandConfig.Command);
				Log.Debug("command configured as stored procedure: {0}", commandConfig.IsStoredProcedure);

				IConnectionStringProvider connectionStringProvider;
				string connectionName = null;

				if (commandConfig.ConnectionStringProviderType != null)
				{
					Log.Debug("Activating configured connection string provider, '{0}'", commandConfig.ConnectionStringProviderType);

					connectionStringProvider =
						Activator.CreateInstance(commandConfig.ConnectionStringProviderType) as IConnectionStringProvider;
					connectionName = commandConfig.ConnectionName;
				}
				else
				{
					//no connection string provider specified so use the appropriate default
					//if a connection string was specified and we dont have a specifi name in the cache regions settings
					//then just use the default connection string
					if (string.IsNullOrEmpty(defaultConnectionName) && string.IsNullOrEmpty(commandConfig.ConnectionName))
					{
						Log.Debug("no connection string provider specified using nhibernate configured connection string");

						connectionStringProvider = new StaticConnectionStringProvider(defaultConnectionString);
					}
					else
					{
						//we dont have any connection strings specified so we must get it from config
						connectionStringProvider = new ConfigConnectionStringProvider();

						//tweak the connection name based on whether the region has one specified or not
						connectionName = string.IsNullOrEmpty(commandConfig.ConnectionName)
							? defaultConnectionName
							: commandConfig.ConnectionName;

						Log.Debug("no connection string provider specified, using connection with name : {0}", connectionName);
					}
				}

				var commandEnlister = new SqlCommandCacheDependencyEnlister(commandConfig.Command, commandConfig.IsStoredProcedure,
					commandConfig.CommandTimeout, connectionName, 
					connectionStringProvider);

				_dependencyEnlisters.Add(commandEnlister);
			}
		}

		/// <summary>
		/// Gets a valid cache key for the element in the cache with <paramref name="identifier"/>.
		/// </summary>
		/// <param name="identifier">The identifier of a cache element.</param>
		/// <returns>The key to use for retrieving an element from the cache.</returns>
		private string GetCacheKey(object identifier)
		{
			return string.Concat(_cacheKeyPrefix, _name, ":", identifier.ToString(), "@", identifier.GetHashCode());
		}

		/// <summary>
		/// Generates the root cache key for the cache region.
		/// </summary>
		/// <returns>The cache key that can be used for the root cache dependency.</returns>
		private string GenerateRootCacheKey()
		{
			return GetCacheKey(Guid.NewGuid());
		}

		/// <summary>
		/// Creates the cache item for the cache region which all other cache items in the region
		/// will be dependent upon.
		/// </summary>
		/// <remarks>
		/// <para>Specified Region dependencies will be associated to the cache item.</para>
		/// </remarks>
		private void CacheRootItem()
		{
			Log.Debug("Creating root cache entry for cache region: {0}", _name);

			//register ant cache dependencies for change notifications
			//and build an aggragate dependency if multiple dependencies exist
			CacheDependency rootCacheDependency = null;

			if (_dependencyEnlisters.Count > 0)
			{
				var dependencies = new List<CacheDependency>(_dependencyEnlisters.Count);

				foreach (var enlister in _dependencyEnlisters)
				{
					Log.Debug("Enlisting cache dependency for change notification");
					dependencies.Add(enlister.Enlist());
				}

				if (dependencies.Count == 1)
				{
					rootCacheDependency = dependencies[0];
				}
				else
				{
					var jointDependency = new AggregateCacheDependency();
					jointDependency.Add(dependencies.ToArray());

					rootCacheDependency = jointDependency;
				}

				Log.Debug("Attaching cache dependencies to root cache entry. Cache entry will be removed when change is detected.");
			}

			_webCache.Add(_rootCacheKey, _rootCacheKey, rootCacheDependency, System.Web.Caching.Cache.NoAbsoluteExpiration,
			              System.Web.Caching.Cache.NoSlidingExpiration, _priority, RootCacheItemRemovedCallback);

			//flag the root cache item as beeing cached
			_isRootItemCached = true;
		}

		/// <summary>
		/// Called when the root cache item has been removed from the cache.
		/// </summary>
		/// <param name="key">The key of the cache item that was removed.</param>
		/// <param name="value">The value of the cache item that was removed.</param>
		/// <param name="reason">The <see cref="CacheItemRemovedReason"/> for the removal of the
		/// item from the cache.</param>
		/// <remarks>
		/// <para>Since all cache items are dependent on the root cache item, if this method is called,
		/// all the cache items for this region have also been removed.</para>
		/// </remarks>
		private void RootCacheItemRemovedCallback(string key, object value, CacheItemRemovedReason reason)
		{
			Log.Debug("Cache items for region '{0}' have been removed from the cache for the following reason : {1:g}",
			          _name, reason);

			//lets us know that we need to reestablish the root cache item for this region
			_isRootItemCached = false;
		}

		/// <summary>
		/// Gets the expiration time for a new item added to the cache.
		/// </summary>
		/// <returns></returns>
		private DateTime? GetCacheItemExpiration()
		{
			//use the relative expiration if one is specified, otherwise use the 
			//time of day expiration if that is specified
			if (_relativeExpiration.HasValue)
			{
				if (_useSlidingExpiration)
					return null;
				return DateTime.UtcNow.Add(_relativeExpiration.Value);
			}

			if (!_timeOfDayExpiration.HasValue)
				return null;

			// Done in local time. Recommendation for supplying expiration is UTC, but that would
			// shift the _timeOfDayExpiration hour.

			//calculate the expiration by starting at 12 am of today
			var timeExpiration = DateTime.Today;

			//add a day to the expiration time if the time of day has already passed,
			//this will cause the item to expire tommorrow
			if (DateTime.Now.TimeOfDay > _timeOfDayExpiration.Value)
			{
				timeExpiration = timeExpiration.AddDays(1);
			}

			//adding the specified time of day expiration to the adjusted base date
			//will provide us with the time of day expiration specified
			timeExpiration = timeExpiration.Add(_timeOfDayExpiration.Value);

			return timeExpiration;
		}
	}
}
