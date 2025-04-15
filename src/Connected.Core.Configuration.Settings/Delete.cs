using Connected.Entities;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Configuration.Settings;

internal sealed class Delete(ISettingCache cache, IStorageProvider storage, IEventService events, ISettingService service)
	: ServiceAction<IPrimaryKeyDto<int>>
{
	protected override async Task OnInvoke()
	{
		var existing = SetState(await Select()) ?? throw new NullReferenceException(nameof(ISetting));

		await storage.Open<Setting>().Update(Dto.AsEntity<Setting>(State.Delete));
	}

	private async Task<Setting?> Select()
	{
		return await service.Select(Dto.AsDto<INameDto>()) as Setting;
	}

	protected override async Task OnCommitted()
	{
		await cache.Refresh(Dto.Id);
		await events.Deleted(this, service, Dto.Id);
	}
}