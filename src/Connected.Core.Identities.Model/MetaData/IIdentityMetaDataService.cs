using Connected.Annotations;
using Connected.Identities.MetaData.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.MetaData;

[Service]
public interface IIdentityMetaDataService
{
	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task Insert(IInsertIdentityMetaDataDto dto);

	[ServiceOperation(ServiceOperationVerbs.Put)]
	Task Update(IUpdateIdentityMetaDataDto dto);

	[ServiceOperation(ServiceOperationVerbs.Delete)]
	Task Delete(IPrimaryKeyDto<string> dto);

	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IImmutableList<IIdentityMetaData>> Query(IQueryDto? dto);

	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IImmutableList<IIdentityMetaData>> Query(IPrimaryKeyListDto<string> dto);

	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IIdentityMetaData?> Select(IPrimaryKeyDto<string> dto);
}