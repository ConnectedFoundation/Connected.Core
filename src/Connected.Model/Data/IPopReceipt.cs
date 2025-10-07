using System;

namespace Connected.Data;
/// <summary>
/// Represents an entity with a conditional visibillity.
/// </summary>
/// <remarks>
/// Some entities require a singleton access which protects
/// them from being processed by multiple clients at a time. This
/// entity serves for such purpose. One example is queue message which
/// must be processed only by a single client. But, on the other hand,
/// a client has only a limited available time to process it successfully.
/// If it's not processed in time, other client gets opportunity to process
/// the message. The isolation is achieved through the PopReceipt property
/// which is updated everytime client dequeues the message. This means other
/// clients can't successfully update (or delete) the message once other
/// clients was granted the access.
/// </remarks>
public interface IPopReceipt
{
	/// <summary>
	/// The id of the current scope. The id is available only upon the expiration
	/// (NextVisible).
	/// </summary>
	Guid? PopReceipt { get; init; }
	/// <summary>
	/// The date and time the current PopReceipt expires and the access is granted to
	/// other client.
	/// </summary>
	DateTimeOffset NextVisible { get; init; }
}
