using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace NHibernate.Caches.StackExRedis
{
	/// <summary>
	/// A region strategy that have very simple read/write operations but does not support
	/// <see cref="Clear"/> operation.
	/// </summary>
	public partial class FastRegionStrategy : AbstractRegionStrategy
	{
		private const string SlidingGetLuaScript = @"
	local value = redis.call('get', KEYS[1])
	if value ~= nil then
		redis.call('pexpire', KEYS[1], ARGV[2])
	end
	return value";

		private const string SlidingGetManyLuaScript = @"
	local expirationMs = ARGV[2]
	local values = redis.call('MGET', unpack(KEYS));
	for i=1,#KEYS do
		if values[i] ~= nil then
			redis.call('pexpire', KEYS[i], expirationMs)
		end
	end
	return values";

		private const string ExpirationPutManyLuaScript = @"
	local expirationMs = ARGV[#ARGV]
	for i=1,#KEYS do
		redis.call('set', KEYS[i], ARGV[i], 'px', expirationMs)
	end";

		private const string LockManyLuaScript = @"
	local lockValue = ARGV[#ARGV-1]
	local expirationMs = ARGV[#ARGV]
	local lockedKeys = {}
	local lockedKeyIndex = 1
	local locked = true
	for i=1,#KEYS do
		if redis.call('set', KEYS[i], lockValue, 'nx', 'px', expirationMs) == false then
			locked = false
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

		private const string UnlockLuaScript = @"
	if redis.call('get', KEYS[1]) == ARGV[1] then
		return redis.call('del', KEYS[1])
	else 
		return 0
	end";

		private const string UnlockManyLuaScript = @"
	local lockValue = ARGV[1]
	local removedKeys = 0
	for i=1,#KEYS do
		if redis.call('get', KEYS[i]) == lockValue then
			removedKeys = removedKeys + redis.call('del', KEYS[i])
		end
	end
	return removedKeys";


		/// <summary>
		/// Default constructor.
		/// </summary>
		public FastRegionStrategy(ConnectionMultiplexer connectionMultiplexer,
			RedisCacheRegionConfiguration configuration, IDictionary<string, string> properties)
			: base(connectionMultiplexer, configuration, properties)
		{
			if (ExpirationEnabled)
			{
				PutManyScript = ExpirationPutManyLuaScript;
				if (UseSlidingExpiration)
				{
					GetScript = SlidingGetLuaScript;
					GetManyScript = SlidingGetManyLuaScript;
				}
			}
		}

		/// <inheritdoc />
		protected override string GetScript { get; }

		/// <inheritdoc />
		protected override string GetManyScript { get; }

		/// <inheritdoc />
		protected override string PutManyScript { get; }

		/// <inheritdoc />
		protected override string LockManyScript => LockManyLuaScript;

		/// <inheritdoc />
		protected override string UnlockScript => UnlockLuaScript;

		/// <inheritdoc />
		protected override string UnlockManyScript => UnlockManyLuaScript;

		/// <inheritdoc />
		public override void Clear()
		{
			throw new NotSupportedException(
				$"{nameof(Clear)} operation is not supported, if it cannot be avoided use {nameof(DefaultRegionStrategy)}.");
		}

		/// <inheritdoc />
		public override void Validate()
		{
		}
	}
}
