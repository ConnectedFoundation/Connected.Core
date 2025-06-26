using Connected.Services;

namespace Connected.Identities.Dtos;

public interface IUpdateUserDto : IUserDto, IPrimaryKeyDto<long>
{
	UserStatus Status { get; set; }
}