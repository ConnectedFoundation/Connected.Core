using Connected.Entities;

namespace Connected.Globalization.Languages;
public interface ILanguage : IEntity<int>
{
	string Name { get; init; }
	int Lcid { get; init; }
	Status Status { get; init; }
	string Culture { get; init; }
}
