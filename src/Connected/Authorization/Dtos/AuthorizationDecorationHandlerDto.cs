using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Authorization.Dtos;

internal sealed class AuthorizationDecorationHandlerDto : Dto, IAuthorizationDecorationHandlerDto
{
	[Required]
	public required IAuthorization Middleware { get; set; }
}
