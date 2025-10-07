using Connected.Entities;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Globalization.Languages;
internal sealed class Delete(IStorageProvider storage, ILanguageCache cache, IEventService events, ILanguageService languages)
	: ServiceAction<IPrimaryKeyDto<int>>
{
	protected override async Task OnInvoke()
	{
		SetState(await languages.Select(Dto));

		await storage.Open<Language>().Update(Dto.AsEntity<Language>(State.Delete));
		await cache.Remove(Dto.Id);
		await events.Deleted(this, languages, Dto.Id);
	}
}
