using Connected.Entities;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Identities.Globalization;

internal sealed class Update(IIdentityGlobalizationCache cache, IStorageProvider storage, IEventService events, IIdentityGlobalizationService globalization)
	: ServiceAction<IUpdateIdentityGlobalizationDto>
{
	protected override async Task OnInvoke()
	{
		if (SetState(await globalization.Select(new PrimaryKeyDto<string> { Id = Dto.Id })) is not IdentityGlobalization existing)
			return;

		await storage.Open<IdentityGlobalization>().Update(Dto.AsEntity<IdentityGlobalization>(State.Default), Dto, async () =>
		{
			await cache.Refresh(Dto.Id);

			return SetState(await globalization.Select(new PrimaryKeyDto<string> { Id = Dto.Id })) as IdentityGlobalization;
		}, Caller);

		await cache.Refresh(Dto.Id);
		await events.Updated(this, globalization, Dto.Id);
	}
}