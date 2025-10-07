using Connected.Services;

namespace Connected.Authorization.Dtos;

public interface IAuthorizationDecorationHandlerDto : IDto
{
	IAuthorization Middleware { get; set; }
}
