using Connected.Identities.Dtos;
using Connected.Services;

namespace Connected.Identities;

public interface IInsertUserAmbient : IAmbientProvider<IInsertUserDto>
{
	string Token { get; set; }
	UserStatus Status { get; set; }
}
