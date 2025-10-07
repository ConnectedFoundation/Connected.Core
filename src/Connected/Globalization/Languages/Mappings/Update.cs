using Connected.Entities;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Globalization.Languages.Mappings;
internal sealed class Update(IStorageProvider storage, IEventService events, ILanguageMappingCache cache, ILanguageMappingService mappings)
	: ServiceAction<IUpdateLanguageMappingDto>
{
	protected override async Task OnInvoke()
	{
		if (SetState(await mappings.Select(Dto.AsDto<IPrimaryKeyDto<int>>())) is not LanguageMapping mapping)
			throw new NullReferenceException(Strings.ErrEntityExpected);

		await storage.Open<LanguageMapping>().Update(mapping.Merge(Dto, State.Update), Dto, async () =>
		{
			await cache.Refresh(Dto.Id);

			return SetState(await mappings.Select(Dto.AsDto<IPrimaryKeyDto<int>>()) as LanguageMapping);
		}, Caller);

		await cache.Refresh(Dto.Id);
		await events.Updated(this, mappings, Dto.Id);
	}
}
