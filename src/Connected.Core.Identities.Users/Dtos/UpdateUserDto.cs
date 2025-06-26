using Connected.Annotations;

namespace Connected.Identities.Dtos;

internal sealed class UpdateUserDto : UserDto, IUpdateUserDto
{
	public UserStatus Status { get; set; } = UserStatus.Disabled;

	[MinValue(1)]
	public long Id { get; set; }
}
