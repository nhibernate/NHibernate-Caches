using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace NHibernate.Caches.StackExchangeRedis
{
	/// <summary>
	/// Provides the lua scripts of the internal region strategies from the embedded resources.
	/// </summary>
	internal static class LuaScriptProvider
	{
		// Dictionary<RegionStrategyName, Dictionary<ScriptName, LuaScript>>
		private static readonly Dictionary<string, Dictionary<string, string>> StrategyLuaScripts =
			new Dictionary<string, Dictionary<string, string>>();

		static LuaScriptProvider()
		{
			var assembly = Assembly.GetExecutingAssembly();
			var regex = new Regex(@"Lua\.([\w]+)\.([\w]+)\.lua");
			foreach (var resourceName in assembly.GetManifestResourceNames())
			{
				var match = regex.Match(resourceName);
				if (!match.Success)
				{
					continue;
				}
				if (!StrategyLuaScripts.TryGetValue(match.Groups[1].Value, out var luaScripts))
				{
					luaScripts = new Dictionary<string, string>();
					StrategyLuaScripts.Add(match.Groups[1].Value, luaScripts);
				}

				using (var stream = assembly.GetManifestResourceStream(resourceName))
				using (var reader = new StreamReader(stream))
				{
					luaScripts.Add(match.Groups[2].Value, reader.ReadToEnd());
				}
			}
		}

		/// <summary>
		/// Get the concatenation of multiple lua scripts for the region strategy.
		/// </summary>
		/// <typeparam name="TRegionStrategy">The region strategy.</typeparam>
		/// <param name="scriptNames">The script names to concatenate.</param>
		/// <returns>The concatenation of multiple lua scripts.</returns>
		public static string GetConcatenatedScript<TRegionStrategy>(params string[] scriptNames) where TRegionStrategy : AbstractRegionStrategy
		{
			var scriptBuilder = new StringBuilder();
			foreach (var scriptName in scriptNames)
			{
				scriptBuilder.Append(GetScript<TRegionStrategy>(scriptName));
			}
			return scriptBuilder.ToString();
		}

		/// <summary>
		/// Get the lua script for the region strategy.
		/// </summary>
		/// <typeparam name="TRegionStrategy">The region strategy.</typeparam>
		/// <param name="scriptName">The script name.</param>
		/// <returns>The lua script.</returns>
		public static string GetScript<TRegionStrategy>(string scriptName) where TRegionStrategy : AbstractRegionStrategy
		{
			return GetScript(scriptName, typeof(TRegionStrategy).Name);
		}

		/// <summary>
		/// Get a common lua script.
		/// </summary>
		/// <param name="scriptName">The script name.</param>
		/// <returns>The lua script.</returns>
		public static string GetScript(string scriptName)
		{
			return GetScript(scriptName, "Common");
		}

		private static string GetScript(string scriptName, string folderName)
		{
			if (!StrategyLuaScripts.TryGetValue(folderName, out var luaScripts))
			{
				throw new KeyNotFoundException(
					$"There are no embedded scripts for region strategy {folderName}.");
			}
			if (!luaScripts.TryGetValue(scriptName, out var script))
			{
				throw new KeyNotFoundException(
					$"There is no embedded script with name {scriptName} for region strategy {folderName}.");
			}

			return script;
		}
	}
}
