using System;
using System.Collections.Generic;
using NHibernate.Cache;
using NUnit.Framework;

namespace NHibernate.Caches.Common.Tests
{
	[TestFixture]
	public abstract class Fixture
	{
		protected virtual bool SupportsDefaultExpiration => true;
		protected virtual bool DisposeCacheProvidersPerTest => false;

		protected ICacheProvider DefaultProvider { get; private set; }

		protected abstract Func<ICacheProvider> ProviderBuilder { get; }

		private readonly Dictionary<string, string> _defaultProperties = new Dictionary<string, string>();
		private readonly List<ICacheProvider> _providers = new List<ICacheProvider>();

		/// <summary>
		/// Yield a copy of default properties for avoiding having tests altering it.
		/// </summary>
		/// <returns>Default properties.</returns>
		protected IDictionary<string, string> GetDefaultProperties() => new Dictionary<string, string>(_defaultProperties);

		protected const string DefaultRegion = "nunit";
		protected const string DefaultExpirationSetting = "expiration";

		[OneTimeSetUp]
		public void FixtureSetup()
		{
			Configure(_defaultProperties);
			if (!DisposeCacheProvidersPerTest)
			{
				DefaultProvider = GetNewProvider();
			}
		}

		protected virtual void Configure(Dictionary<string, string> defaultProperties)
		{
			if (SupportsDefaultExpiration)
			{
				defaultProperties.Add(DefaultExpirationSetting, 120.ToString());
			}
		}

		[OneTimeTearDown]
		public void FixtureTearDown()
		{
			if (DisposeCacheProvidersPerTest)
			{
				return;
			}

			StopProviders();
			OnOneTimeTearDown();
		}

		protected virtual void OnOneTimeTearDown()
		{}

		[SetUp]
		public void TestSetup()
		{
			if (!DisposeCacheProvidersPerTest)
			{
				return;
			}

			DefaultProvider = GetNewProvider();
		}

		[TearDown]
		public void TearDown()
		{
			if (!DisposeCacheProvidersPerTest)
			{
				return;
			}

			StopProviders();
		}

		protected ICacheProvider GetNewProvider()
		{
			var provider = ProviderBuilder();
			_providers.Add(provider);
			provider.Start(GetDefaultProperties());
			return provider;
		}

		protected CacheBase GetDefaultCache()
		{
			var cache = (CacheBase) DefaultProvider.BuildCache(DefaultRegion, GetDefaultProperties());
			Assert.That(cache, Is.Not.Null, "No default cache returned");
			return cache;
		}

		private void StopProviders()
		{
			foreach (var provider in _providers)
			{
				provider.Stop();
			}

			_providers.Clear();
		}
	}
}
