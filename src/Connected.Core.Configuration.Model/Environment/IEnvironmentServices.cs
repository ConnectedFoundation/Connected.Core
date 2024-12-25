using System;
using System.Collections.Immutable;

namespace Connected.Configuration.Environment;

public interface IEnvironmentServices
{
	ImmutableList<Type> Services { get; }
	ImmutableList<Type> ServiceMethods { get; }
	ImmutableList<Type> Caches { get; }
	ImmutableList<Type> Middlewares { get; }
	ImmutableList<Type> Dispatchers { get; }
	ImmutableList<Type> DispatcherJobs { get; }
	ImmutableList<Type> Models { get; }
	ImmutableList<Type> Dtos { get; }
}
