using Connected.Identities;
using Connected.Identities.Dtos;

namespace Connected.Core.Identities.Mock.Dto;
public class UpdateUserDtoMock
	: UserDtoMock, IUpdateUserDto
{
	public UserStatus Status { get; set; }
	public long Id { get; set; }
}
