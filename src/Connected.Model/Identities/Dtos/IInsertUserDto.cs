namespace Connected.Identities.Dtos;

public interface IInsertUserDto : IUserDto
{
	string? Password { get; set; }
}