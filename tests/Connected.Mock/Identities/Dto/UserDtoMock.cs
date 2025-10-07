using Connected.Core.Services.Mock;
using Connected.Identities.Dtos;

namespace Connected.Core.Identities.Mock.Dto;
public class UserDtoMock : DtoMock, IUserDto
{
	public string? FirstName { get; set; }
	public string? LastName { get; set; }
	public string? Email { get; set; }
}
