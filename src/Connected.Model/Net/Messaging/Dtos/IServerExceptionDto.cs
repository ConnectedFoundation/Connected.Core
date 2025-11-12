using Connected.Services;

namespace Connected.Net.Messaging.Dtos;

/// <summary>
/// Represents a data transfer object for server-side exception information.
/// </summary>
/// <remarks>
/// This interface encapsulates exception details that occur on the server side during
/// message processing or service operations. It enables structured error communication
/// between server and client, allowing clients to receive and handle server errors
/// gracefully. The DTO typically contains an error message that describes what went wrong,
/// which clients can use for error handling, logging, or user notification purposes.
/// This is essential for building robust distributed applications where errors need to
/// be communicated across process boundaries in a structured manner.
/// </remarks>
public interface IServerExceptionDto
	: IDto
{
	/// <summary>
	/// Gets or sets the exception message.
	/// </summary>
	/// <value>
	/// A string containing the error message, or null if no message is available.
	/// </value>
	/// <remarks>
	/// The message provides information about the server-side error that occurred,
	/// helping clients understand what went wrong and potentially take corrective action.
	/// This should contain user-friendly or developer-friendly error information depending
	/// on the application's error handling strategy.
	/// </remarks>
	string? Message { get; set; }
}
