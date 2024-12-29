using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Globalization.Languages.Mappings;
internal sealed class Delete(IStorageProvider storage, ILanguageMappingService mappings, ILanguageMappingCache cache, IEventService events)
	: ServiceAction<IPrimaryKeyDto<int>>
{

	protected override async Task OnInvoke()
	{
		if (SetState(await mappings.Select(Dto)) is not LanguageMapping entity)
			throw new NullReferenceException(Strings.ErrEntityExpected);

		await storage.Open<LanguageMapping>().Update(entity.Merge(Dto, Entities.State.Deleted));
		await cache.Refresh(Dto.Id);
		await events.Deleted(this, mappings, Dto.Id);
	}
}
