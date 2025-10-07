using Connected.Entities;
using Connected.Services;

namespace Connected.Globalization.Languages;
public interface IUpdateLanguageDto : IDto
{
	int Id { get; set; }

	string Name { get; set; }

	Status Status { get; set; }

	int Lcid { get; set; }

	string Culture { get; set; }
}
