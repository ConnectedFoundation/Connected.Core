using Connected.Services;

namespace Connected.Identities;

public interface IInsertUserDto : IDto
{
	string? FirstName { get; set; }
	string? LastName { get; set; }
	string? Email { get; set; }
	string? Password { get; set; }
}