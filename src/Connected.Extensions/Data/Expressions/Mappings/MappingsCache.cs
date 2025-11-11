using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Mappings;

internal static class MappingsCache
{
	static MappingsCache()
	{
		Items = new();
	}

	private static ConcurrentDictionary<string, EntityMapping> Items { get; }

	public static EntityMapping Get(Type entityType)
	{
		if (entityType.FullName is not string key)
			throw new ArgumentNullException(entityType.Name);

		if (Items.TryGetValue(key, out EntityMapping? existing))
			return existing;

		Items.TryAdd(key, new EntityMapping(entityType));

		if (!Items.TryGetValue(key, out EntityMapping? result))
			throw new NullReferenceException(nameof(EntityMapping));

		return result;
	}

	public static bool CanEvaluateLocally(Expression expression)
	{
		if (expression is ConstantExpression cex)
		{
			if (cex.Value is IQueryable query && query.Provider.GetType() == typeof(IStorage<>))
				return false;
		}

		if (expression is MethodCallExpression mc && (mc.Method.DeclaringType == typeof(Enumerable) || mc.Method.DeclaringType == typeof(Queryable)))
			return false;

		if (expression.NodeType == ExpressionType.Convert && expression.Type == typeof(object))
			return true;

		return expression.NodeType != ExpressionType.Parameter && expression.NodeType != ExpressionType.Lambda;
	}
}
