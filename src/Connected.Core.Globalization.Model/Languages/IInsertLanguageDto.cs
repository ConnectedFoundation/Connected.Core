using Connected.Entities;
using Connected.Services;

namespace Connected.Globalization.Languages;
public interface IInsertLanguageDto : IDto
{
	string Name { get; set; }
	string Culture { get; set; }
	Status Status { get; set; }
	int Lcid { get; set; }
}
