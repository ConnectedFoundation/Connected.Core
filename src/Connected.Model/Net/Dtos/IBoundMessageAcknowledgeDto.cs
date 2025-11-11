namespace Connected.Net.Dtos;

/// <summary>
/// Represents a message acknowledgement that is bound to a specific connection.
/// </summary>
/// <remarks>
/// This interface extends the base message acknowledgement functionality by associating
/// the acknowledgement with a specific connection identifier. This is useful in scenarios
/// where message delivery confirmation needs to be tracked per connection, such as in
/// distributed messaging systems or when managing multiple client connections. The connection
/// identifier enables precise tracking of which connection acknowledged the message.
/// </remarks>
public interface IBoundMessageAcknowledgeDto
	: IMessageAcknowledgeDto
{
	/// <summary>
	/// Gets or sets the connection identifier associated with this acknowledgement.
	/// </summary>
	/// <value>
	/// A string representing the unique connection identifier.
	/// </value>
	/// <remarks>
	/// The connection identifier helps track which specific connection acknowledged the message,
	/// enabling connection-specific message delivery tracking and acknowledgement management.
	/// </remarks>
	string Connection { get; set; }
}
