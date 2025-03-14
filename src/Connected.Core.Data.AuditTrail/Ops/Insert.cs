using Connected.Data.AuditTrail;
using Connected.Data.AuditTrail.Dtos;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Data.Ops;

internal sealed class Insert(IStorageProvider storage, IAuditTrailService auditTrail, IEventService events, IInsertAuditTrailAmbient ambient)
	: ServiceFunction<IInsertAuditTrailDto, long>
{
	protected override async Task<long> OnInvoke()
	{
		var result = await storage.Open<AuditTrailEntry>().Update(Dto.AsEntity<AuditTrailEntry>(Entities.State.New, ambient)) ?? throw new NullReferenceException(Strings.ErrEntityExpected);

		await events.Inserted(this, auditTrail, result.Id);

		return result.Id;
	}
}
