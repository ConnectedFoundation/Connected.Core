using Connected.Annotations;
using Connected.Identities.MetaData.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.MetaData;

[Service]
public interface IIdentityMetaDataService
{
	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Put)]
	Task Insert(IInsertIdentityMetaDataDto dto);

	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task Update(IUpdateIdentityMetaDataDto dto);

	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Delete)]
	Task Delete(IPrimaryKeyDto<string> dto);

	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Get)]
	Task<IImmutableList<IIdentityMetaData>> Query(IQueryDto? dto);

	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Get)]
	Task<IIdentityMetaData?> Select(IPrimaryKeyDto<string> dto);
}