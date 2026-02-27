using System;
using System.Collections.Immutable;

namespace Connected.Services.Middlewares;

public interface ICollectionProviderMiddleware<TCollectionItem, TDto> : IMiddleware
{
	Task<ImmutableList<TCollectionItem>> Invoke(TDto dto);
}
