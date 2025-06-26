using Connected.Services;

namespace Connected.Identities.Dtos;

public interface ISelectUserDto : IDto
{
	string User { get; set; }
	string? Password { get; set; }
}