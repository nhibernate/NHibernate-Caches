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
		private static readonly string SlidingGetLuaScript;
		private static readonly string SlidingGetManyLuaScript;
		private static readonly string ExpirationPutManyLuaScript;
		private static readonly string LockManyLuaScript;
		private static readonly string UnlockLuaScript;
		private static readonly string UnlockManyLuaScript;

		static FastRegionStrategy()
		{
			SlidingGetLuaScript = LuaScriptProvider.GetScript<FastRegionStrategy>("SlidingGet");
			SlidingGetManyLuaScript = LuaScriptProvider.GetScript<FastRegionStrategy>("SlidingGetMany");
			ExpirationPutManyLuaScript = LuaScriptProvider.GetScript<FastRegionStrategy>("ExpirationPutMany");
			LockManyLuaScript = LuaScriptProvider.GetScript<FastRegionStrategy>(nameof(LockMany));
			UnlockLuaScript = LuaScriptProvider.GetScript<FastRegionStrategy>(nameof(Unlock));
			UnlockManyLuaScript = LuaScriptProvider.GetScript<FastRegionStrategy>(nameof(UnlockMany));
		}


		/// <summary>
		/// Default constructor.
		/// </summary>
		public FastRegionStrategy(IConnectionMultiplexer connectionMultiplexer,
			RedisCacheRegionConfiguration configuration, IDictionary<string, string> properties)
			: base(connectionMultiplexer, configuration, properties)
		{
			if (!ExpirationEnabled)
			{
				return;
			}
			PutManyScript = ExpirationPutManyLuaScript;
			if (UseSlidingExpiration)
			{
				GetScript = SlidingGetLuaScript;
				GetManyScript = SlidingGetManyLuaScript;
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
