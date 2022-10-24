using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bamboo.Prevalence;
using Bamboo.Prevalence.Util;
using NHibernate.Cache;

namespace NHibernate.Caches.Prevalence
{
	/// <summary>
	/// Cache provider using <a href="http://bbooprevalence.sourceforge.net/">Bamboo Prevalence</a>,
	/// a Prevayler implementation in .NET.
	/// </summary>
	public class PrevalenceCacheProvider : ICacheProvider
	{
		private static readonly INHibernateLogger log = NHibernateLogger.For(typeof(PrevalenceCacheProvider));
		private string dataDir;
		private PrevalenceEngine engine;
		private CacheSystem system;
		private SnapshotTaker taker;

		#region ICacheProvider Members

		/// <inheritdoc />
		/// <remarks>There is only one configurable parameter: prevalenceBase. This is
		/// the directory on the file system where the Prevalence engine will save data.
		/// It can be relative to the current directory or a full path. If the directory
		/// doesn't exist, it will be created.</remarks>
#pragma warning disable 618
		public ICache BuildCache(string regionName, IDictionary<string, string> properties)
#pragma warning restore 618
		{
			if (regionName == null)
			{
				regionName = "";
			}
			if (properties == null)
			{
				properties = new Dictionary<string, string>();
			}
			if (log.IsDebugEnabled())
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
				log.Debug("building cache with region: {0}, properties: {1}", regionName, sb);
			}
			dataDir = GetDataDirFromConfig(regionName, properties);
			if (system == null)
			{
				SetupEngine();
			}

			return new PrevalenceCache(regionName, system);
		}

		/// <inheritdoc />
		public long NextTimestamp()
		{
			return Timestamper.Next();
		}

		/// <inheritdoc />
		public void Start(IDictionary<string, string> properties)
		{
			if (string.IsNullOrEmpty(dataDir))
			{
				dataDir = GetDataDirFromConfig("", properties);
			}
			if (system == null)
			{
				SetupEngine();
			}
		}

		/// <inheritdoc />
		public void Stop()
		{
			try
			{
				engine.HandsOffOutputLog();
				taker.Dispose();
				if (Directory.Exists(dataDir))
				{
					Directory.Delete(dataDir, true);
				}
			}
			catch
			{
				// not a big deal, probably a permissions issue
			}
		}

		#endregion

		private void SetupEngine()
		{
			engine = PrevalenceActivator.CreateTransparentEngine(typeof (CacheSystem), dataDir);
			system = engine.PrevalentSystem as CacheSystem;
			taker = new SnapshotTaker(engine, TimeSpan.FromMinutes(5), CleanUpAllFilesPolicy.Default);
		}

		private static string GetDataDirFromConfig(string region, IDictionary<string, string> properties)
		{
			string dataDir = Path.Combine(Environment.CurrentDirectory, region);

			if (properties != null)
			{
				if (properties.ContainsKey("prevalenceBase"))
				{
					string prevalenceBase = properties["prevalenceBase"];
					dataDir = Path.IsPathRooted(prevalenceBase) ? prevalenceBase : Path.Combine(Environment.CurrentDirectory, prevalenceBase);

					if (properties.ContainsKey("regionPrefix"))
					{
						string regionPrefix = properties["regionPrefix"];

						log.Debug("new regionPrefix :{0}", regionPrefix);

						dataDir = Path.Combine(dataDir, regionPrefix);
					}
				}
			}
			if (Directory.Exists(dataDir) == false)
			{
				log.Debug("Data directory {0} doesn't exist: creating it.", dataDir);
				Directory.CreateDirectory(dataDir);
			}
			log.Debug("configuring cache in {0}.", dataDir);
			return dataDir;
		}
	}
}
