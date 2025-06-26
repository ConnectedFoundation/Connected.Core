using Connected.Services;

namespace Connected.Identities.Dtos;

public interface IUserDto : IDto
{
	string? FirstName { get; set; }
	string? LastName { get; set; }
	string? Email { get; set; }
}
