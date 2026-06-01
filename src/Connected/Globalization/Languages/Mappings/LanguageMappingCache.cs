using Connected.Caching;
using Connected.Storage;
using Microsoft.Extensions.Configuration;
using System.Collections.Immutable;

namespace Connected.Globalization.Languages.Mappings;

internal sealed class LanguageMappingCache(ICachingService cache, IStorageProvider storage, IConfiguration configuration)
	: EntityCache<ILanguageMapping, LanguageMapping, int>(cache, storage, MetaData.LanguageMappingKey)
{
	protected override async Task<IImmutableList<ILanguageMapping>?> OnInitializingEntities()
	{
		var items = configuration.GetSection("globalization:languages").GetChildren();
		var result = new List<ILanguageMapping>();
		var index = 0;

		foreach (var item in items)
		{
			if (!int.TryParse(item["id"], out var id))
				continue;

			var mappings = item.GetChildren();

			foreach (var mapping in mappings)
			{
				var value = mapping.Value;

				if (string.IsNullOrWhiteSpace(value))
					continue;

				result.Add(new LanguageMapping
				{
					Id = index++,
					Language = id,
					Mapping = value
				});
			}
		}

		return await Task.FromResult(result.ToImmutableList());
	}
}