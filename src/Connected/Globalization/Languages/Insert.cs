using Connected.Entities;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Globalization.Languages;
internal sealed class Insert(IStorageProvider storage, ILanguageCache cache, IEventService events, ILanguageService languages)
	: ServiceFunction<IInsertLanguageDto, int>
{
	protected override async Task<int> OnInvoke()
	{
		if (await storage.Open<Language>().Update(Dto.AsEntity<Language>(State.Add)) is not Language entity)
			throw new NullReferenceException(Strings.ErrEntityExpected);

		await cache.Refresh(entity.Id);
		await events.Inserted(this, languages, entity.Id);

		return entity.Id;
	}
}
