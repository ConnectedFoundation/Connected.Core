using Connected.Services;

namespace Connected.Authentication;
public interface IAuthenticateDto : IDto
{
	string? Schema { get; set; }
	string? Token { get; set; }
}
