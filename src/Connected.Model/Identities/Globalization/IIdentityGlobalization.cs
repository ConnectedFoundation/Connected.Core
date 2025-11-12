using Connected.Entities;

namespace Connected.Identities.Globalization;

/// <summary>
/// Represents an identity globalization entity containing localization preferences.
/// </summary>
/// <remarks>
/// This interface defines the contract for storing globalization settings associated
/// with an identity, including time zone and language preferences. The entity uses
/// a string-based primary key and init-only properties to ensure immutability after
/// construction. These settings enable proper localization and cultural customization
/// for user experiences across different regions and languages.
/// </remarks>
public interface IIdentityGlobalization
	: IEntity<string>
{
	/// <summary>
	/// Gets the time zone identifier for the identity.
	/// </summary>
	string? TimeZone { get; init; }

	/// <summary>
	/// Gets the language identifier for the identity.
	/// </summary>
	int? Language { get; init; }
}