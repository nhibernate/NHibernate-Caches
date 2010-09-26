#region License

//Microsoft Public License (Ms-PL)
//
//This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.
//
//1. Definitions
//
//The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under U.S. copyright law.
//
//A "contribution" is the original software, or any additions or changes to the software.
//
//A "contributor" is any person that distributes its contribution under this license.
//
//"Licensed patents" are a contributor's patent claims that read directly on its contribution.
//
//2. Grant of Rights
//
//(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
//
//(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
//
//3. Conditions and Limitations
//
//(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
//
//(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
//
//(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
//
//(D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
//
//(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.

#endregion

using System.Collections.Generic;
using System.Configuration;
using System.Text;
using NHibernate.Cache;

namespace NHibernate.Caches.Velocity
{
	/// <summary>
	/// Velocity - A cache provider for NHibernate using the Microsoft project code named “Velocity”
	///  (http://code.msdn.microsoft.com/velocity/)
	/// </summary>
	public class VelocityProvider : ICacheProvider
	{
		private static readonly IInternalLogger log;

		static VelocityProvider()
		{
			log = LoggerProvider.LoggerFor((typeof(VelocityProvider)));
			var configs = ConfigurationManager.GetSection("velocity") as VelocityConfig[];
		}

		#region ICacheProvider Members

		/// <summary>
		/// Configure the cache
		/// </summary>
		/// <param name="regionName">the name of the cache region</param>
		/// <param name="properties">configuration settings</param>
		/// <returns></returns>
		public ICache BuildCache(string regionName, IDictionary<string, string> properties)
		{
			if (regionName == null)
			{
				regionName = "";
			}
			if (properties == null)
			{
				properties = new Dictionary<string, string>();
			}
			if (log.IsDebugEnabled)
			{
				var sb = new StringBuilder();
				foreach (var de in properties)
				{
					sb.Append("name=");
					sb.Append(de.Key);
					sb.Append("&value=");
					sb.Append(de.Value);
					sb.Append(";");
				}
				log.Debug("building cache with region: " + regionName + ", properties: " + sb);
			}
			return new VelocityClient(regionName, properties);
		}

		/// <summary>
		/// generate a timestamp
		/// </summary>
		/// <returns></returns>
		public long NextTimestamp()
		{
			return Timestamper.Next();
		}

		/// <summary>
		/// Callback to perform any necessary initialization of the underlying cache implementation
		/// during ISessionFactory construction.
		/// </summary>
		/// <param name="properties">current configuration settings</param>
		public void Start(IDictionary<string, string> properties) {}

		/// <summary>
		/// Callback to perform any necessary cleanup of the underlying cache implementation
		/// during <see cref="M:NHibernate.ISessionFactory.Close"/>.
		/// </summary>
		public void Stop() {}

		#endregion
	}
}