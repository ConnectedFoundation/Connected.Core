using Connected.Entities;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Configuration.Settings;

internal sealed class Update(ISettingCache cache, IStorageProvider storage, IEventService events, ISettingService service)
	: ServiceAction<IUpdateSettingDto>
{
	protected override async Task OnInvoke()
	{
		if (SetState(await service.Select(Dto.AsDto<INameDto>())) is not Setting existing)
		{
			var entity = await storage.Open<Setting>().Update(Dto.AsEntity<Setting>(State.New)) ?? throw new NullReferenceException();

			await cache.Refresh(entity.Id);
			await events.Inserted(this, service, entity.Id);

			return;
		}

		await storage.Open<Setting>().Update(existing, Dto, async () =>
		{
			await cache.Refresh(existing.Id);

			return SetState(await service.Select(Dto.AsDto<INameDto>())) as Setting;
		}, Caller);

		await cache.Refresh(existing.Id);
		await events.Updated(this, service, existing.Id);
	}
}
