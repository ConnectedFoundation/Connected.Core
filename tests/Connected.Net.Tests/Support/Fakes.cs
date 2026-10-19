using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Reflection;

namespace Connected.Net.Tests;

/// <summary>
/// The middleware service, answering the one query the hooks make: the middlewares of a given interface.
/// </summary>
public class MiddlewareServiceProxy
	: DispatchProxy
{
	internal List<object> Middlewares { get; } = [];

	/// <summary>
	/// What the query fails with, or <c>null</c> for one that answers.
	/// </summary>
	internal Exception? QueryFails { get; set; }

	protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
	{
		if (targetMethod is not { Name: "Query", IsGenericMethod: true })
			throw new NotSupportedException(targetMethod?.Name);

		if (QueryFails is not null)
			throw QueryFails;

		var type = targetMethod.GetGenericArguments()[0];
		var matching = Middlewares.Where(type.IsInstanceOfType).ToArray();
		var typed = Array.CreateInstance(type, matching.Length);

		Array.Copy(matching, typed, matching.Length);

		var immutable = typeof(ImmutableList)
			.GetMethods()
			.First(f => f.Name == nameof(ImmutableList.CreateRange) && f.GetParameters().Length == 1)
			.MakeGenericMethod(type)
			.Invoke(null, [typed]);

		return typeof(Task)
			.GetMethod(nameof(Task.FromResult))!
			.MakeGenericMethod(typeof(IImmutableList<>).MakeGenericType(type))
			.Invoke(null, [immutable]);
	}

	/// <summary>
	/// Creates a middleware service that answers with the given middlewares.
	/// </summary>
	internal static (IMiddlewareService Service, MiddlewareServiceProxy Proxy) Create(params object[] middlewares)
	{
		var service = DispatchProxy.Create<IMiddlewareService, MiddlewareServiceProxy>();
		var proxy = (MiddlewareServiceProxy)(object)service;

		proxy.Middlewares.AddRange(middlewares);

		return (service, proxy);
	}
}

/// <summary>
/// A logger that keeps what it was asked to write.
/// </summary>
public sealed class CapturingLogger<T>
	: ILogger<T>
{
	public List<(LogLevel Level, Exception? Exception, string Message)> Entries { get; } = [];

	public IDisposable? BeginScope<TState>(TState state)
		where TState : notnull => null;

	public bool IsEnabled(LogLevel logLevel) => true;

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
		Entries.Add((logLevel, exception, formatter(state, exception)));
}
