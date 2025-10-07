using System.Linq;
using System;
using System.Reflection;

namespace Connected.Entities;

internal static class QueryableMethods
{
	static QueryableMethods()
	{
		var queryableMethodGroups = typeof(Queryable)
			.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
			.GroupBy(mi => mi.Name)
			.ToDictionary(e => e.Key, l => l.ToList());

		SingleWithoutPredicate = GetMethod(nameof(Queryable.Single), 1, types => new[] { typeof(IQueryable<>).MakeGenericType(types[0]) });
		SingleOrDefaultWithoutPredicate = GetMethod(nameof(Queryable.SingleOrDefault), 1, types => new[] { typeof(IQueryable<>).MakeGenericType(types[0]) });

		MethodInfo GetMethod(string name, int genericParameterCount, Func<Type[], Type[]> parameterGenerator)
		{
			return queryableMethodGroups[name].Single(mi => ((genericParameterCount == 0 && !mi.IsGenericMethod)
							|| (mi.IsGenericMethod && mi.GetGenericArguments().Length == genericParameterCount))
							&& mi.GetParameters().Select(e => e.ParameterType).SequenceEqual(parameterGenerator(mi.IsGenericMethod ? mi.GetGenericArguments() : Array.Empty<Type>())));
		}
	}

	public static MethodInfo SingleWithoutPredicate { get; }
	public static MethodInfo SingleOrDefaultWithoutPredicate { get; }
}
