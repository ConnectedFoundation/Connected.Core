using Connected.Globalization.Languages;
using Connected.Services;

namespace Connected.Globalization;
public static class LanguageExtensions
{
	public static async Task<ILanguage?> Match(this ILanguageService service, ILanguageMappingService mappings, string qualifier)
	{
		var items = await mappings.Query(new QueryLanguageMappingsDto());

		foreach (var item in items)
		{
			if (string.Equals(item.Mapping, qualifier, StringComparison.OrdinalIgnoreCase))
				return await service.Select(Dto.Factory.CreatePrimaryKey(item.Language));
		}

		return null;
	}
}
