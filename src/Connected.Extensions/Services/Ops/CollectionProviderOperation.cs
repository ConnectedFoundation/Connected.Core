using Connected.Services.Middlewares;
using System.Collections.Immutable;

namespace Connected.Services.Ops;

public class CollectionProviderOperation<TDto, TCollectionItem, TMiddleware>(IMiddlewareService middlewares) : ServiceFunction<TDto, IImmutableList<TCollectionItem>> where TDto : IDto
																																												 where TMiddleware : ICollectionProviderMiddleware<TCollectionItem, TDto>
{
	protected override async Task<IImmutableList<TCollectionItem>> OnInvoke()
	{
		var valueProviders = await middlewares.Query<TMiddleware>();

		var tasks = valueProviders.Select(e => e.Invoke(Dto));
		var results = await Task.WhenAll(tasks);

		return [.. results.SelectMany(e => e)];
	}
}
