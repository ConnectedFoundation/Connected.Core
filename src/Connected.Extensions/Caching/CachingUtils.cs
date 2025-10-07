using Connected.Annotations;
using Connected.Reflection;
using System.Reflection;

namespace Connected.Caching;

public static class CachingUtils
{
	public static PropertyInfo GetCacheKeyProperty(object instance)
	{
		return Properties.GetPropertyAttribute<CacheKeyAttribute>(instance) ?? throw new NullReferenceException(SR.ErrCacheKeyNotSet);
	}
}
