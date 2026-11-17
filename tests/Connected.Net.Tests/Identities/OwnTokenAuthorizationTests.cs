using Connected.Authentication;
using Connected.Authorization;
using Connected.Authorization.Services;
using Connected.Identities;
using Connected.Identities.Authentication;
using Connected.Identities.Authorization;
using Connected.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Net.Tests.Identities;

/// <summary>
/// Holds that Ping, Reset, TokenInfo and SignOut act only on a token of the calling identity.
/// </summary>
[TestClass]
public sealed class OwnTokenAuthorizationTests
{
	private const string OwnToken = "own-session";
	private const string OtherToken = "other-session";

	private readonly IUser _user = new TestUser { Id = 7, Status = UserStatus.Active, Token = "identity-7" };

	private readonly List<TestToken> _tokens =
	[
		new() { Id = 1, Key = IdentityAuthenticationTokens.AuthenticationToken, Token = OwnToken, Identity = "identity-7", Status = AuthenticationTokenStatus.Enabled },
		new() { Id = 2, Key = IdentityAuthenticationTokens.AuthenticationToken, Token = OtherToken, Identity = "identity-8", Status = AuthenticationTokenStatus.Enabled }
	];

	private sealed class AuthorizationDto
		: Dto, IServiceOperationAuthorizationDto<IValueDto<string>>
	{
		public ICallerContext Caller { get; set; } = default!;
		public IValueDto<string> Dto { get; set; } = default!;
	}

	[TestInitialize]
	public void Initialize()
	{
		var services = new ServiceCollection();

		services.AddTransient<IUpdateIdentityDto, Connected.Authentication.Dtos.UpdateIdentityDto>();
		services.AddScoped(_ => Stub.Create<IAuthenticationService>());
		services.AddSingleton(Stub.Create<IIdentityAuthenticationTokenService>((method, args) =>
			method.Name == nameof(IIdentityAuthenticationTokenService.Select) && args[0] is IValueDto<string> value
				? Task.FromResult<IIdentityAuthenticationToken?>(_tokens.FirstOrDefault(f => f.Token == value.Value))
				: null));

		new ServiceExtensionsStartup().ConfigureServices(services);
	}

	[TestCleanup]
	public async Task Cleanup()
	{
		await ServiceExtensionsStartup.Reset();
	}

	/// <summary>
	/// A signed-in caller may act on a token of their own identity, although it differs from the identity's own token.
	/// </summary>
	[TestMethod]
	public async Task AllowsTheCallersOwnToken()
	{
		Assert.AreEqual(AuthorizationResult.Skip, await Authorize(_user, OwnToken));
	}

	/// <summary>
	/// A token of another identity is refused.
	/// </summary>
	[TestMethod]
	public async Task RefusesAnotherIdentitysToken()
	{
		Assert.AreEqual(AuthorizationResult.Fail, await Authorize(_user, OtherToken));
	}

	/// <summary>
	/// An unknown token, a missing token and an anonymous caller are refused.
	/// </summary>
	[TestMethod]
	[DataRow("unknown")]
	[DataRow("")]
	public async Task RefusesUnknownTokens(string token)
	{
		Assert.AreEqual(AuthorizationResult.Fail, await Authorize(_user, token));
	}

	[TestMethod]
	public async Task RefusesAnonymousCallers()
	{
		Assert.AreEqual(AuthorizationResult.Fail, await Authorize(null, OwnToken));
	}

	private static async Task<AuthorizationResult> Authorize(IIdentity? caller, string token)
	{
		var authentication = Stub.Create<IAuthenticationService>((method, _) =>
			method.Name == nameof(IAuthenticationService.SelectIdentity) ? Task.FromResult(caller) : null);

		return await new TokenInfoAuthorization(authentication).Invoke(new AuthorizationDto
		{
			Dto = new ValueDto<string>(token)
		});
	}
}
