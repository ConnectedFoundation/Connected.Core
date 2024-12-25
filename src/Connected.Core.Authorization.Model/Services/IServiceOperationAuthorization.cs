using Connected.Services;

namespace Connected.Authorization.Services;

public interface IServiceOperationAuthorization<TDto> : IAuthorization
		where TDto : IDto
{
	Task Invoke(IServiceOperationAuthorizationDto<TDto> dto);
}