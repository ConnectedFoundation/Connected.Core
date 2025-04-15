using Connected.Entities;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Globalization.Languages;
internal sealed class Update(IStorageProvider storage, IEventService events, ILanguageCache cache, ILanguageService languages)
	: ServiceAction<IUpdateLanguageDto>
{
	protected override async Task OnInvoke()
	{
		if (SetState(await languages.Select(Dto.AsDto<IPrimaryKeyDto<int>>())) is not Language language)
			throw new NullReferenceException(Strings.ErrEntityExpected);

		await storage.Open<Language>().Update(language.Merge(Dto, State.Update), Dto, async () =>
		{
			await cache.Refresh(Dto.Id);

			return SetState(await languages.Select(Dto.AsDto<IPrimaryKeyDto<int>>()) as Language);
		}, Caller);

		await cache.Refresh(Dto.Id);
		await events.Updated(this, languages, Dto.Id);
	}
}
