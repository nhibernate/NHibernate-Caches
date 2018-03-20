#region License

//
//  CoreDistributedCache - A cache provider for NHibernate using Microsoft.Extensions.Caching.Distributed.IDistributedCache.
//
//  This library is free software; you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation; either
//  version 2.1 of the License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

#endregion

using System;
using NHibernate.Cache;
using NHibernate.Caches.Common.Tests;
using NUnit.Framework;

namespace NHibernate.Caches.CoreDistributedCache.Tests
{
	[TestFixture]
	public class CoreDistributedCacheFixture : CacheFixture
	{
		protected override bool SupportsSlidingExpiration => true;
		protected override bool SupportsClear => false;

		protected override Func<ICacheProvider> ProviderBuilder =>
			() => new CoreDistributedCacheProvider();
	}
}
