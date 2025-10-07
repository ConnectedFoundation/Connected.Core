using Connected.Core.Services.Mock;
using Connected.Identities.Dtos;

namespace Connected.Core.Identities.Mock.Dto;
public class SelectUserDtoMock
	: DtoMock, ISelectUserDto
{
	public string? Password { get; set; }
	public required string User { get; set; }
}
