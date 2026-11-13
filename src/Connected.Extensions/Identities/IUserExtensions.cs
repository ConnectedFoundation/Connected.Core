using Connected.Annotations;
using Connected.Identities.Dtos;
using Connected.Identities.TwoFactor;

namespace Connected.Identities;

[Service, ServiceUrl(IdentitiesUrls.Users)]
public interface IUserExtensions
{
	/// <summary>
	/// Signs a user in with credentials alone.
	/// </summary>
	/// <param name="dto">The credentials.</param>
	/// <returns>
	/// The authentication token, or <see langword="null"/> when the credentials are not valid or the user must sign in
	/// with a code through <see cref="SignIn"/>.
	/// </returns>
	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task<string?> Validate(IValidateUserDto dto);

	/// <summary>
	/// Signs a user in, asking for a code when two-factor sign-in applies to the user.
	/// </summary>
	/// <param name="dto">The credentials.</param>
	/// <returns>
	/// The authentication token, or a challenge to complete with <see cref="ITwoFactorService"/>; <see langword="null"/>
	/// when the credentials are not valid.
	/// </returns>
	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task<SignInResult?> SignIn(IValidateUserDto dto);
}