using Connected.Services;

namespace Connected.Membership.Dtos;

public interface IQueryUserDto
	: IDto
{
	string Role { get; set; }
}
