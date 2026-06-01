using Connected.Caching;
using Connected.Entities;
using Connected.Storage;
using Microsoft.Extensions.Configuration;
using System.Collections.Immutable;

namespace Connected.Globalization.Languages;

internal sealed class LanguageCache(ICachingService cache, IStorageProvider storage, IConfiguration configuration)
	: EntityCache<ILanguage, Language, int>(cache, storage, MetaData.LanguageKey)
{
	protected override async Task<IImmutableList<ILanguage>?> OnInitializingEntities()
	{
		var items = configuration.GetSection("globalization:languages").GetChildren();
		var result = new List<ILanguage>();

		foreach (var item in items)
		{
			var name = item["name"];
			var culture = item["culture"];

			if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(culture))
				continue;

			if (!int.TryParse(item["id"], out var id))
				continue;

			if (!int.TryParse(item["lcid"], out var lcid))
				continue;

			var status = Status.Enabled;

			if (!string.IsNullOrWhiteSpace(item["status"]))
				Enum.TryParse(item["status"], true, out status);

			result.Add(new Language
			{
				Id = id,
				Name = name,
				Lcid = lcid,
				Status = status,
				Culture = culture
			});
		}

		return await Task.FromResult(result.ToImmutableList());
	}
}
