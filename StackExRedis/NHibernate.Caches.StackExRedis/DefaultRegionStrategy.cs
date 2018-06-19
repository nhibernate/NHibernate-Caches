using System.Collections.Generic;
using NHibernate.Cache;
using StackExchange.Redis;
using static NHibernate.Caches.StackExRedis.ConfigurationHelper;

namespace NHibernate.Caches.StackExRedis
{
	/// <summary>
	/// The default region strategy. This strategy uses a special key that contains the region current version number which is appended
	/// after the region prefix. Each time a clear operation is performed the version number is increased and an event is send to all
	/// clients so that they can update thier local versions. Even if the event was not sent to all clients, each operation has a
	/// version check in order to prevent working with stale data.
	/// </summary>
	public partial class DefaultRegionStrategy : AbstractRegionStrategy
	{
		private const string InvalidVersionMessage = "Invalid version";

		private static readonly string CheckVersionCode = $@"
	local version = redis.call('get', KEYS[#KEYS])
	if version ~= ARGV[#ARGV] then
		return redis.error_reply('{InvalidVersionMessage}')
	end";

		private const string UpdateVersionLuaScript = @"
	local version = redis.call('incr', KEYS[1])
	if version > tonumber(ARGV[1]) then
		version = 1
		redis.call('set', KEYS[1], version)
	end
	return version";

		private const string InitializeVersionLuaScript = @"
	if redis.call('exists', KEYS[1]) == 1 then
		return redis.call('get', KEYS[1])
	else
		redis.call('set', KEYS[1], 1)
		return 1
	end";

		private static readonly string GetLuaScript = $@"
	{CheckVersionCode}
	local value = redis.call('get', KEYS[1])
	if value ~= nil and ARGV[1] == '1' then
		redis.call('pexpire', KEYS[1], ARGV[2])
	end
	return value";

		private static readonly string GetManyLuaScript = $@"
	{CheckVersionCode}
	local values = {{}}
	local sliding = ARGV[#ARGV-2]
	local expirationMs = ARGV[#ARGV-1]
	for i=1,#KEYS-1 do
		local value = redis.call('get', KEYS[i])
		if value ~= nil and sliding == '1' then
			redis.call('pexpire', KEYS[i], expirationMs)
		end
		values[i] = value
	end
	return values";

		private static readonly string PutLuaScript = $@"
	{CheckVersionCode}
	return redis.call('set', KEYS[1], ARGV[1], 'px', ARGV[3])";

		private static readonly string PutManyLuaScript = $@"
	{CheckVersionCode}
	local expirationMs = ARGV[#ARGV-1]
	for i=1,#KEYS-1 do
		redis.call('set', KEYS[i], ARGV[i], 'px', expirationMs)
	end";

		private static readonly string RemoveLuaScript = $@"
	{CheckVersionCode}
	return redis.call('del', KEYS[1])";

		private static readonly string RemoveManyLuaScript = $@"
	{CheckVersionCode}
	local removedKeys = 0
	for i=1,#KEYS-1 do
		removedKeys = removedKeys + redis.call('del', KEYS[i])
	end
	return removedKeys";

		private static readonly string LockLuaScript = $@"
	{CheckVersionCode}
	if redis.call('set', KEYS[1], ARGV[1], 'nx', 'px', ARGV[2]) == false then
		return 0
	else 
		return 1
	end";

		private static readonly string LockManyLuaScript = $@"
	{CheckVersionCode}
	local lockValue = ARGV[#ARGV-2]
	local expirationMs = ARGV[#ARGV-1]
	local lockedKeys = {{}}
	local lockedKeyIndex = 1
	local locked = true
	for i=1,#KEYS-1 do
		if redis.call('set', KEYS[i], lockValue, 'nx', 'px', expirationMs) == false then
			locked = 0
			break
		else
			lockedKeys[lockedKeyIndex] = KEYS[i]
			lockedKeyIndex = lockedKeyIndex + 1
		end
	end
	if locked == true then
		return 1
	else
		for i=1,#lockedKeys do
			redis.call('del', lockedKeys[i])
		end
		return 0
	end";

		private static readonly string UnlockLuaScript = $@"
	{CheckVersionCode}
	if redis.call('get', KEYS[1]) == ARGV[1] then
		return redis.call('del', KEYS[1])
	else 
		return 0
	end";

		private static readonly string UnlockManyLuaScript = $@"
	{CheckVersionCode}
	local lockValue = ARGV[1]
	local removedKeys = 0
	for i=1,#KEYS-1 do
		if redis.call('get', KEYS[i]) == lockValue then
			removedKeys = removedKeys + redis.call('del', KEYS[i])
		end
	end
	return removedKeys";

		private readonly RedisKey[] _regionKeyArray;
		private readonly RedisValue[] _maxVersionNumber;
		private RedisValue _currentVersion;
		private RedisValue[] _currentVersionArray;
		private readonly bool _usePubSub;

		/// <summary>
		/// Default constructor.
		/// </summary>
		public DefaultRegionStrategy(ConnectionMultiplexer connectionMultiplexer,
			RedisCacheRegionConfiguration configuration, IDictionary<string, string> properties)
			: base(connectionMultiplexer, configuration, properties)
		{
			var maxVersion = GetInteger("cache.region_strategy.default.max_allowed_version", properties, 1000);
			Log.Debug("Max allowed version for region {0}: {1}", RegionName, maxVersion);

			_usePubSub = GetBoolean("cache.region_strategy.default.use_pubsub", properties, true);
			Log.Debug("Use pubsub for region {0}: {1}", RegionName, _usePubSub);

			_regionKeyArray = new RedisKey[] { RegionKey };
			_maxVersionNumber = new RedisValue[] { maxVersion };
			InitializeVersion();

			if (_usePubSub)
			{
				ConnectionMultiplexer.GetSubscriber().SubscribeAsync(RegionKey, (channel, value) =>
				{
					UpdateVersion(value);
				});
			}
		}

		/// <summary>
		/// The version number that is currently used to retrieve/store keys.
		/// </summary>
		public long CurrentVersion => (long) _currentVersion;

		/// <inheritdoc />
		protected override string GetScript => GetLuaScript;

		/// <inheritdoc />
		protected override string GetManyScript => GetManyLuaScript;

		/// <inheritdoc />
		protected override string PutScript => PutLuaScript;

		/// <inheritdoc />
		protected override string PutManyScript => PutManyLuaScript;

		/// <inheritdoc />
		protected override string RemoveScript => RemoveLuaScript;

		/// <inheritdoc />
		protected override string RemoveManyScript => RemoveManyLuaScript;

		/// <inheritdoc />
		protected override string LockScript => LockLuaScript;

		/// <inheritdoc />
		protected override string LockManyScript => LockManyLuaScript;

		/// <inheritdoc />
		protected override string UnlockScript => UnlockLuaScript;

		/// <inheritdoc />
		protected override string UnlockManyScript => UnlockManyLuaScript;

		/// <inheritdoc />
		public override object Get(object key)
		{
			try
			{
				return base.Get(key);
			}
			catch (RedisServerException e) when (e.Message == InvalidVersionMessage)
			{
				InitializeVersion();
				return base.Get(key);
			}
		}

		/// <inheritdoc />
		public override object[] GetMany(object[] keys)
		{
			try
			{
				return base.GetMany(keys);
			}
			catch (RedisServerException e) when (e.Message == InvalidVersionMessage)
			{
				InitializeVersion();
				return base.GetMany(keys);
			}
		}

		/// <inheritdoc />
		public override string Lock(object key)
		{
			try
			{
				return base.Lock(key);
			}
			catch (RedisServerException e) when (e.Message == InvalidVersionMessage)
			{
				InitializeVersion();
				return base.Lock(key);
			}
		}

		/// <inheritdoc />
		public override string LockMany(object[] keys)
		{
			try
			{
				return base.LockMany(keys);
			}
			catch (RedisServerException e) when (e.Message == InvalidVersionMessage)
			{
				InitializeVersion();
				return base.LockMany(keys);
			}
		}

		/// <inheritdoc />
		public override void Put(object key, object value)
		{
			try
			{
				base.Put(key, value);
			}
			catch (RedisServerException e) when (e.Message == InvalidVersionMessage)
			{
				InitializeVersion();
				// Here we don't know if the operation was executed after as successful lock, so
				// the easiest solution is to skip the operation
			}
		}

		/// <inheritdoc />
		public override void PutMany(object[] keys, object[] values)
		{
			try
			{
				base.PutMany(keys, values);
			}
			catch (RedisServerException e) when (e.Message == InvalidVersionMessage)
			{
				InitializeVersion();
				// Here we don't know if the operation was executed after as successful lock, so
				// the easiest solution is to skip the operation
			}
		}

		/// <inheritdoc />
		public override bool Remove(object key)
		{
			try
			{
				return base.Remove(key);
			}
			catch (RedisServerException e) when (e.Message == InvalidVersionMessage)
			{
				InitializeVersion();
				// There is no point removing the key in the new version.
				return false;
			}
		}

		/// <inheritdoc />
		public override long RemoveMany(object[] keys)
		{
			try
			{
				return base.RemoveMany(keys);
			}
			catch (RedisServerException e) when (e.Message == InvalidVersionMessage)
			{
				InitializeVersion();
				// There is no point removing the keys in the new version.
				return 0L;
			}
		}

		/// <inheritdoc />
		public override bool Unlock(object key)
		{
			try
			{
				return base.Unlock(key);
			}
			catch (RedisServerException e) when (e.Message == InvalidVersionMessage)
			{
				InitializeVersion();
				// If the lock was acquired in the old version we are unable to unlock the key.
				return false;
			}
		}

		/// <inheritdoc />
		public override int UnlockMany(object[] keys, string lockValue)
		{
			try
			{
				return base.UnlockMany(keys, lockValue);
			}
			catch (RedisServerException e) when (e.Message == InvalidVersionMessage)
			{
				InitializeVersion();
				// If the lock was acquired in the old version we are unable to unlock the keys.
				return 0;
			}
		}

		/// <inheritdoc />
		public override void Clear()
		{
			var results = (RedisValue[]) Database.ScriptEvaluate(UpdateVersionLuaScript,
				_regionKeyArray, _maxVersionNumber);
			var version = results[0];
			UpdateVersion(version);
			if (_usePubSub)
			{
				ConnectionMultiplexer.GetSubscriber().Publish(RegionKey, version);
			}
		}

		/// <inheritdoc />
		public override void Validate()
		{
			if (!ExpirationEnabled)
			{
				throw new CacheException($"Expiration must be greater than zero for cache region: '{RegionName}'");
			}
		}

		/// <inheritdoc />
		protected override RedisKey[] GetAdditionalKeys()
		{
			return _regionKeyArray;
		}

		/// <inheritdoc />
		protected override RedisValue[] GetAdditionalValues()
		{
			return _currentVersionArray;
		}

		/// <inheritdoc />
		protected override string GetCacheKey(object value)
		{
			return string.Concat("{", RegionKey, "}-", _currentVersion, ":", value.ToString(), "@", value.GetHashCode());
		}

		private void InitializeVersion()
		{
			var results = (RedisValue[]) Database.ScriptEvaluate(InitializeVersionLuaScript, _regionKeyArray);
			var version = results[0];
			UpdateVersion(version);
		}

		private void UpdateVersion(RedisValue version)
		{
			_currentVersion = version;
			_currentVersionArray = new[] {version};
		}
	}
}
