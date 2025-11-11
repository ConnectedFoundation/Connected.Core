using Connected.Annotations;

namespace Connected.Services;

/// <summary>
/// Provides access to cancellation tokens and cancellation control for service operations.
/// </summary>
/// <remarks>
/// This service interface enables service operations to check for cancellation requests
/// and to trigger cancellation of ongoing operations. It provides a centralized mechanism
/// for managing cancellation across asynchronous workflows.
/// </remarks>
[Service]
public interface ICancellationContext
{
	/// <summary>
	/// Gets the cancellation token that can be monitored for cancellation requests.
	/// </summary>
	CancellationToken CancellationToken { get; }

	/// <summary>
	/// Cancels the current operation by signaling the cancellation token.
	/// </summary>
	void Cancel();
}