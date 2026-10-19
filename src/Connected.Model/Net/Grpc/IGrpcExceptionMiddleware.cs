using Grpc.Core;

namespace Connected.Net.Grpc;

/// <summary>
/// Defines a middleware that decides what a gRPC call fails with when its handler throws.
/// </summary>
/// <remarks>
/// <para>
/// By default an exception thrown by a handler reaches the caller as a status of <c>Unknown</c> and a generic message,
/// whatever the exception was. A middleware replaces that: it returns the <see cref="RpcException"/> the call fails
/// with, which may carry any status, any detail and any trailers, so what a caller receives is not limited to what
/// the framework knows about.
/// </para>
/// <para>
/// The middlewares are asked in the order of their <see cref="Connected.Annotations.PriorityAttribute"/>, the highest
/// first; one without the attribute counts as 0. Two with the same priority are asked in no defined order, so a
/// middleware that has to be asked before another needs a higher priority.
/// </para>
/// <para>
/// A middleware that throws does not change the outcome for the others: the failure is logged and the next middleware
/// is asked.
/// </para>
/// <para>
/// The first one that returns an exception wins. When none does, the exception propagates as before, so an
/// application that registers no middleware sees no difference. An exception that is already an
/// <see cref="RpcException"/> is not offered to the middlewares: it was meant to be sent as it is.
/// </para>
/// </remarks>
public interface IGrpcExceptionMiddleware
	: IMiddleware
{
	/// <summary>
	/// Translates the exception a handler threw, or declines to.
	/// </summary>
	/// <param name="context">The call and the exception its handler threw.</param>
	/// <returns>The exception the call fails with, or <c>null</c> to leave the exception to the next middleware or to
	/// the default behavior.</returns>
	Task<RpcException?> Translate(IGrpcExceptionContext context);
}

/// <summary>
/// What an <see cref="IGrpcExceptionMiddleware"/> works with.
/// </summary>
public interface IGrpcExceptionContext
{
	/// <summary>
	/// Gets the exception the handler threw.
	/// </summary>
	Exception Exception { get; }

	/// <summary>
	/// Gets the call, for the method, the headers of the request and the identity it was made with.
	/// </summary>
	ServerCallContext CallContext { get; }
}
