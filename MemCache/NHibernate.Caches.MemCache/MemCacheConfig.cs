#region License

//
//  MemCache - A cache provider for NHibernate using the .NET client
//  (http://sourceforge.net/projects/memcacheddotnet) for memcached,
//  which is located at http://www.danga.com/memcached/.
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
// CLOVER:OFF
//

#endregion

using System;

namespace NHibernate.Caches.MemCache
{
	/// <summary>
	/// A Memcached server configuration.
	/// </summary>
	public class MemCacheConfig
	{
		/// <summary>
		/// Constructor with a default cache weigth of one.
		/// </summary>
		/// <param name="host">The cache server host name.</param>
		/// <param name="port">The cache server port.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="host"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="port"/> is less or equal to zero.</exception>
		public MemCacheConfig(string host, int port) : this(host, port, 1) {}

		/// <summary>
		/// Full constructor.
		/// </summary>
		/// <param name="host">The cache server host name.</param>
		/// <param name="port">The cache server port.</param>
		/// <param name="weight">The cache server weight.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="host"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="port"/> is less or equal to zero.</exception>
		public MemCacheConfig(string host, int port, int weight)
		{
			if (string.IsNullOrEmpty(host))
			{
				throw new ArgumentNullException("host");
			}
			if (port <= 0)
			{
				throw new ArgumentOutOfRangeException("port", "port must be greater than 0.");
			}
			Host = host;
			Port = port;
			Weight = weight;
		}

		/// <summary>
		/// The cache server host name.
		/// </summary>
		public string Host { get; private set; }

		/// <summary>
		/// The cache server port.
		/// </summary>
		public int Port { get; private set; }

		/// <summary>
		/// The cache server weight.
		/// </summary>
		public int Weight { get; private set; }
	}
}
