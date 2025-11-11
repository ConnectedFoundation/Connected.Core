using Connected.Services;

namespace Connected.Net.Dtos;

/// <summary>
/// Represents a message acknowledgement data transfer object.
/// </summary>
/// <remarks>
/// This interface defines the basic structure for acknowledging message receipt or processing.
/// Message acknowledgements are essential in reliable messaging systems to confirm that messages
/// have been successfully received and processed by clients. The acknowledgement includes a
/// message identifier that references the specific message being acknowledged. This enables
/// message brokers and messaging systems to track message delivery status, implement retry
/// mechanisms, and ensure at-least-once or exactly-once delivery semantics.
/// </remarks>
public interface IMessageAcknowledgeDto
	: IDto
{
	/// <summary>
	/// Gets or sets the unique identifier of the message being acknowledged.
	/// </summary>
	/// <value>
	/// An unsigned 64-bit integer representing the message identifier.
	/// </value>
	/// <remarks>
	/// This identifier uniquely references the message that is being acknowledged,
	/// allowing the messaging system to correlate acknowledgements with specific
	/// messages and update delivery tracking accordingly.
	/// </remarks>
	ulong Id { get; set; }
}
