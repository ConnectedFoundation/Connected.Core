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
   /*
    * Data providers hydrate and refresh cache contents on demand.
    */
}