namespace Connected.Entities;

/// <summary>
/// Specifies the status of the record.
/// </summary>
public enum Status : byte
{
	/// <summary>
	/// The record is enabled and can be used when adding and editing
	/// data.
	/// </summary>
	Enabled = 1,
	/// <summary>
	/// The record is disabled and is used only for display purposes.
	/// </summary>
	Disabled = 2
}
