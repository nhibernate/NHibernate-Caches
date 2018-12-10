namespace NHibernate.Caches.StackExchangeRedis
{
	/// <summary>
	/// Defines a method to get a unique value that will be used as a value when locking keys in
	/// order to identify which instance locked the key.
	/// </summary>
	public interface ICacheLockValueProvider
	{
		/// <summary>
		/// Gets a unique value that will be used for locking keys.
		/// </summary>
		/// <returns>A unique value.</returns>
		string GetValue();
	}
}
