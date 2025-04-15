﻿using Connected.Annotations;
using System.Collections.Immutable;

namespace Connected;

[Service]
public interface IMiddlewareService
{
	Task<TMiddleware?> First<TMiddleware>() where TMiddleware : IMiddleware;
	Task<IImmutableList<TMiddleware>> Query<TMiddleware>() where TMiddleware : IMiddleware;
	Task<IImmutableList<TMiddleware>> Query<TMiddleware>(ICallerContext? caller) where TMiddleware : IMiddleware;

	Task<IMiddleware?> First(Type type);
	Task<IImmutableList<IMiddleware>> Query(Type type);
	Task<IImmutableList<IMiddleware>> Query(Type type, ICallerContext? caller);
}