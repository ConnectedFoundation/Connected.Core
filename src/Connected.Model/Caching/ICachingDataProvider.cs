namespace Connected.Caching;
/// <summary>
/// Specifies a provider for the data in the cache.
/// </summary>
public interface ICachingDataProvider
{
   /// <summary>
   /// Initializes the cache which usually means loading data into the container.
   /// </summary>
   Task Initialize();
   /// <summary>
   /// Invalidates the entry in the container. This usually means reloading it from the storage.
   /// </summary>
   Task Invalidate(object id);
   /// <summary>
   /// Atomically drops all entries from the container and marks it as not initialized, so the
   /// next access hydrates it again from the storage.
   /// </summary>
   /// <remarks>
   /// Unlike <see cref="Invalidate(object)"/>, which refreshes a single entry, this discards the
   /// entire container. It is meant for cases where the backing store was replaced out of band,
   /// for example by an offline synchronization tool.
   /// </remarks>
   Task Reset();
   /*
    * Data providers hydrate and refresh cache contents on demand.
    */
}