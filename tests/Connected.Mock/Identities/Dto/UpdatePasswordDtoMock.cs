using Connected.Core.Services.Mock;
using Connected.Identities.Dtos;

namespace Connected.Core.Identities.Mock.Dto;
public class UpdatePasswordDtoMock
	: DtoMock, IUpdatePasswordDto
{
	public string? Password { get; set; }
	public long Id { get; set; }
}
