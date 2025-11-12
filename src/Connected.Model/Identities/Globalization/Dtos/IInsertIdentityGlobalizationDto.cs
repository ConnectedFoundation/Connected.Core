using Connected.Services;

namespace Connected.Identities.Globalization.Dtos;

/// <summary>
/// Represents a data transfer object for inserting identity globalization settings.
/// </summary>
/// <remarks>
/// This interface defines the contract for creating new globalization preferences
/// for an identity, including time zone and language settings. These settings
/// enable localization and cultural customization for user experiences.
/// </remarks>
public interface IInsertIdentityGlobalizationDto
	: IDto
{
	/// <summary>
	/// Gets or sets the identity identifier for which globalization settings are being created.
	/// </summary>
	string Id { get; set; }

	/// <summary>
	/// Gets or sets the time zone identifier for the identity.
	/// </summary>
	string? TimeZone { get; set; }

	/// <summary>
	/// Gets or sets the language identifier for the identity.
	/// </summary>
	int? Language { get; set; }
}
