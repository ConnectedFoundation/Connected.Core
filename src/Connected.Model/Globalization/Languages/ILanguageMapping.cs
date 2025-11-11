using Connected.Entities;

namespace Connected.Globalization.Languages;

/// <summary>
/// Represents a mapping between a language and an external identifier or key.
/// </summary>
public interface ILanguageMapping
	: IEntity<int>
{
	/// <summary>
	/// Gets the language identifier (primary key of the associated language).
	/// </summary>
	int Language { get; init; }
	/// <summary>
	/// Gets the external mapping value or key.
	/// </summary>
	string Mapping { get; init; }
}
