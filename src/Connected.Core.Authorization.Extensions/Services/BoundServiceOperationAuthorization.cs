using Connected.Services;

namespace Connected.Authorization.Services;
public abstract class BoundServiceOperationAuthorization<TDto> : ServiceOperationAuthorization<TDto>, IBoundAuthorization
	where TDto : IDto
{
	public abstract string Entity { get; }

	public abstract string EntityId { get; }

}
