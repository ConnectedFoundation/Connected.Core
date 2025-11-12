using Connected.Services;

namespace Connected.Identities.Globalization.Dtos;

/// <summary>
/// Represents a data transfer object for updating identity globalization settings.
/// </summary>
/// <remarks>
/// This interface defines the contract for modifying existing globalization preferences
/// for an identity, allowing changes to time zone and language settings. These updates
/// enable dynamic adaptation of localization and cultural customization.
/// </remarks>
public interface IUpdateIdentityGlobalizationDto
	: IDto
{
	/// <summary>
	/// Gets or sets the identity identifier for which globalization settings are being updated.
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
