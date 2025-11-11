namespace Connected.Entities;
/// <summary>
/// Represents the logical availability of a record in the domain model.
/// </summary>
/// <remarks>
/// The <see cref="Status"/> enum is used throughout the runtime to indicate
/// whether a record may be selected or used for mutation operations. Typical
/// UI and business logic should treat <see cref="Enabled"/> values as active
/// and usable, while <see cref="Disabled"/> values are retained for historical
/// or display-only scenarios.
/// 
/// The enum uses <c>byte</c> as the underlying storage type to keep persisted
/// representations compact when stored in databases or transmitted over the
/// wire. Consumers should not rely on numeric values directly; prefer the
/// named members to express intent.
/// </remarks>
public enum Status : byte
{
	/// <summary>
	/// The record is enabled and considered active. It can be used for reads,
	/// updates and for creating relationships with other entities.
	/// </summary>
	Enabled = 1,

	/// <summary>
	/// The record is disabled and should not be used for new business actions.
	/// It is typically preserved for auditing, historical display or soft-delete
	/// scenarios. Disabled records may still be visible in read-only views.
	/// </summary>
	Disabled = 2
}

