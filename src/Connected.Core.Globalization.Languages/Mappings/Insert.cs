using Connected.Entities;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Globalization.Languages.Mappings;
internal sealed class Insert(IStorageProvider storage, ILanguageMappingCache cache, IEventService events, ILanguageMappingService mappings)
	: ServiceFunction<IInsertLanguageMappingDto, int>
{
	protected override async Task<int> OnInvoke()
	{
		if (await storage.Open<LanguageMapping>().Update(Dto.AsEntity<LanguageMapping>(State.Add)) is not LanguageMapping entity)
			throw new NullReferenceException(Strings.ErrEntityExpected);

		await cache.Refresh(entity.Id);
		await events.Inserted(this, mappings, entity.Id);

		return entity.Id;
	}
}
