using System.Net.Http;

namespace Connected.Net;

/// <summary>
/// Provides factory services for creating HTTP client instances.
/// </summary>
/// <remarks>
/// This service abstracts HTTP client creation, enabling centralized configuration
/// of HTTP clients with consistent settings such as timeouts, default headers, and
/// connection pooling. By centralizing client creation, the service ensures that
/// HTTP clients are properly configured and managed according to best practices,
/// preventing resource exhaustion and socket depletion issues. Implementations
/// typically integrate with dependency injection and HTTP client factory patterns
/// to provide properly configured and lifecycle-managed HTTP client instances.
/// </remarks>
public interface IHttpService
{
	/// <summary>
	/// Creates a new HTTP client instance.
	/// </summary>
	/// <returns>
	/// A configured <see cref="HttpClient"/> instance ready for making HTTP requests.
	/// </returns>
	/// <remarks>
	/// This method returns a properly configured HTTP client that can be used to
	/// make HTTP requests to external services. The returned client should be used
	/// for the duration of a logical operation and may be configured with default
	/// settings such as base addresses, authentication headers, timeout values,
	/// and message handlers. Implementations should leverage HTTP client factory
	/// patterns to ensure proper socket lifecycle management and avoid socket
	/// exhaustion issues.
	/// </remarks>
	HttpClient CreateClient();
}
