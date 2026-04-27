using Connected.Annotations;
using Connected.Reflection;
using System.Collections.Concurrent;
using System.Reflection;
namespace Connected.Caching;
public static class CachingUtils
{
	private static ConcurrentDictionary<Type, PropertyInfo> Index { get; } = new();

	public static PropertyInfo GetCacheKeyProperty(object instance)
	{
		var key = instance.GetType();

		if (Index.TryGetValue(key, out var property) && property.CanRead)
			return property;

		var result = Properties.GetPropertyAttribute<CacheKeyAttribute>(instance) ?? throw new NullReferenceException(SR.ErrCacheKeyNotSet);

		Index[key] = result;

		return result;
	}
}