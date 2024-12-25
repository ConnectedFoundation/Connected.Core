using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Authentication;
/// <summary>
/// A Dto used when performing authentication.
/// </summary>
public class AuthenticateDto : Dto, IAuthenticateDto
{
	/// <summary>
	/// An authentication schema that has been passed to the scope.
	/// </summary>
	[MaxLength(1024)]
	public string? Schema { get; set; }
	/// <summary>
	/// An authentication token that has been passed to the scope.
	/// </summary>
	[MaxLength(1024)]
	public string? Token { get; set; }
}