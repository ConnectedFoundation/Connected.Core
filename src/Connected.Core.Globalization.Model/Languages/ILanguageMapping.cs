using Connected.Entities;

namespace Connected.Globalization.Languages;
public interface ILanguageMapping : IEntity<int>
{
	int Language { get; init; }
	string Mapping { get; init; }
}
