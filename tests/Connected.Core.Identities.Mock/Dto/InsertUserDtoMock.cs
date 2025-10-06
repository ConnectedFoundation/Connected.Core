using Connected.Identities.Dtos;

namespace Connected.Core.Identities.Mock.Dto;
public class InsertUserDtoMock
	: UserDtoMock, IInsertUserDto
{
	public string? Password { get; set; }
}
