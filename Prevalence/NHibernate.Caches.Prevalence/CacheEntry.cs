using System;

namespace NHibernate.Caches.Prevalence
{
	/// <summary>
	/// An item in the cache
	/// </summary>
	[Serializable]
	internal class CacheEntry
	{
		/// <summary>
		/// the unique identifier
		/// </summary>
		public object Key { get; set; }

		/// <summary>
		/// the value
		/// </summary>
		public object Value { get; set; }

		/// <summary>
		/// the unique timestamp
		/// </summary>
		public DateTime DateCreated { get; set; }
	}
}