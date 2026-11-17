using Connected.Authentication;
using Connected.Authorization;
using Connected.Authorization.Services;
using Connected.Configuration;
using Connected.Configuration.Authentication;
using Connected.Entities;
using Connected.Identities;
using Connected.Identities.Authentication;
using Connected.Identities.Authentication.Dtos;
using Connected.Identities.Authorization;
using Connected.Identities.Ops;
using Connected.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using System.Text;

namespace Connected.Net.Tests.Identities;

/// <summary>
/// Holds that Ping, Reset, TokenInfo and SignOut act only on the session the request is authenticated with, and that
/// Ping and SignOut work for such a caller.
/// </summary>
[TestClass]
public sealed class OwnTokenAuthorizationTests
{
	private const string OwnToken = "own-session";
	private const string OtherSession = "other-session";
	private const string Identity = "identity-7";

	private readonly IUser _user = new TestUser { Id = 7, Status = UserStatus.Active, Token = Identity };
	private List<TestToken> _tokens = [];
	private int _updates;

	private sealed class AuthorizationDto
		: Dto, IServiceOperationAuthorizationDto<IValueDto<string>>
	{
		public ICallerContext Caller { get; set; } = default!;
		public IValueDto<string> Dto { get; set; } = default!;
	}

	private sealed record TestDescriptor
		: Entity, IIdentityDescriptor
	{
		public string Name { get; init; } = default!;
		public string Token { get; init; } = default!;
	}

	[TestInitialize]
	public void Initialize()
	{
		_tokens =
		[
			Token(1, OwnToken, Identity, IdentityAuthenticationTokens.AuthenticationToken),
			Token(2, OtherSession, Identity, IdentityAuthenticationTokens.AuthenticationToken),
			// A session renewed by Reset is stored again under a key of its own
			Token(3, "renewed-session", Identity, Guid.NewGuid().ToString("N")),
			Token(4, "someone-else", "identity-8", IdentityAuthenticationTokens.AuthenticationToken)
		];

		var services = new ServiceCollection();

		services.AddTransient<IUpdateIdentityDto, Connected.Authentication.Dtos.UpdateIdentityDto>();
		services.AddTransient<IQueryIdentityAuthenticationTokensDto, Connected.Identities.Authentication.Dtos.QueryIdentityAuthenticationTokensDto>();
		services.AddTransient<IUpdateIdentityAuthenticationTokenDto, Connected.Identities.Authentication.Dtos.UpdateIdentityAuthenticationTokenDto>();
		services.AddTransient(typeof(IPrimaryKeyDto<>), typeof(PrimaryKeyDto<>));
		services.AddTransient(typeof(IValueDto<>), typeof(ValueDto<>));
		services.AddScoped(_ => Stub.Create<IAuthenticationService>());
		services.AddSingleton(Tokens());
		services.AddSingleton(Stub.Create<IIdentityExtensions>((method, args) =>
			method.Name == nameof(IIdentityExtensions.Select) && args[0] is IValueDto<string> value
				? Task.FromResult<IIdentityDescriptor?>(new TestDescriptor { Name = "User", Token = value.Value })
				: null));

		new ServiceExtensionsStartup().ConfigureServices(services);
	}

	[TestCleanup]
	public async Task Cleanup()
	{
		await ServiceExtensionsStartup.Reset();
	}

	/// <summary>
	/// The token the request is authenticated with may be acted on.
	/// </summary>
	[TestMethod]
	public async Task AllowsTheSessionOfTheRequest()
	{
		Assert.AreEqual(AuthorizationResult.Skip, await Authorize(Request(OwnToken), OwnToken));
	}

	/// <summary>
	/// Another session of the same identity, another identity's token, an empty or unknown value are refused.
	/// </summary>
	[TestMethod]
	[DataRow(OtherSession)]
	[DataRow("renewed-session")]
	[DataRow("someone-else")]
	[DataRow("")]
	public async Task RefusesAnyOtherToken(string token)
	{
		Assert.AreEqual(AuthorizationResult.Fail, await Authorize(Request(OwnToken), token));
	}

	/// <summary>
	/// A caller that is anonymous, signed in without a token, or outside an HTTP request is refused.
	/// </summary>
	[TestMethod]
	public async Task RefusesCallersWithoutASessionToken()
	{
		Assert.AreEqual(AuthorizationResult.Fail, await Authorize(new HttpContextAccessor { HttpContext = new DefaultHttpContext() }, OwnToken));
		Assert.AreEqual(AuthorizationResult.Fail, await Authorize(Request(null), OwnToken));
		Assert.AreEqual(AuthorizationResult.Fail, await Authorize(new HttpContextAccessor(), OwnToken));
	}

	/// <summary>
	/// Token authentication keeps the token the request was authenticated with on its identity.
	/// </summary>
	[TestMethod]
	public async Task TokenAuthenticationKeepsTheRequestsToken()
	{
		var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

		await new TokenAuthentication(http).Invoke(new AuthenticateDto
		{
			Schema = "bearer",
			Token = Convert.ToBase64String(Encoding.UTF8.GetBytes(OwnToken))
		});

		var identity = http.HttpContext.User.Identity as HttpIdentity;

		Assert.IsTrue(identity?.IsAuthenticated);
		Assert.AreEqual(OwnToken, identity?.AuthenticationToken);
		Assert.AreEqual(Identity, identity?.Identity?.Token);
	}

	/// <summary>
	/// Signing out revokes every token of the identity, a session renewed under its own key included, and no other
	/// identity's.
	/// </summary>
	[TestMethod]
	public async Task SignOutRevokesEveryTokenOfTheIdentity()
	{
		var tokens = Tokens();

		await new SignOut(tokens, Stub.Create<IAuthenticationService>()).Invoke(new ValueDto<string>(OwnToken));

		CollectionAssert.AreEqual(new[] { "someone-else" }, _tokens.Select(f => f.Token).ToArray());
	}

	/// <summary>
	/// Ping extends an expiring session.
	/// </summary>
	[TestMethod]
	public async Task PingExtendsTheSession()
	{
		_tokens[0] = _tokens[0] with { Expire = DateTimeOffset.UtcNow.AddMinutes(5) };

		var configuration = Stub.Create<IConfigurationService>((method, _) => method.Name == "get_Authentication" ? new TestAuthenticationConfiguration(new TestTwoFactorConfiguration()) : null);

		await new Ping(Tokens(), Stub.Create<IAuthenticationService>(), configuration).Invoke(new ValueDto<string>(OwnToken));

		Assert.AreEqual(1, _updates);
	}

	private static HttpContextAccessor Request(string? token)
	{
		var context = new DefaultHttpContext
		{
			User = new DefaultPrincipal(new HttpIdentity(new TestDescriptor { Name = "User", Token = Identity })
			{
				IsAuthenticated = true,
				AuthenticationToken = token
			})
		};

		return new HttpContextAccessor { HttpContext = context };
	}

	private static async Task<AuthorizationResult> Authorize(IHttpContextAccessor http, string token)
	{
		return await new TokenInfoAuthorization(http).Invoke(new AuthorizationDto
		{
			Dto = new ValueDto<string>(token)
		});
	}

	private static TestToken Token(long id, string token, string identity, string key) => new()
	{
		Id = id,
		Key = key,
		Token = token,
		Identity = identity,
		Status = AuthenticationTokenStatus.Enabled,
		Created = DateTimeOffset.UtcNow
	};

	private IIdentityAuthenticationTokenService Tokens()
	{
		return Stub.Create<IIdentityAuthenticationTokenService>((method, args) =>
		{
			switch (method.Name)
			{
				case nameof(IIdentityAuthenticationTokenService.Select) when args[0] is IValueDto<string> value:
					return Task.FromResult<IIdentityAuthenticationToken?>(_tokens.FirstOrDefault(f => f.Token == value.Value));
				case nameof(IIdentityAuthenticationTokenService.Query):
					var query = (IQueryIdentityAuthenticationTokensDto)args[0]!;

					return Task.FromResult<IImmutableList<IIdentityAuthenticationToken>>(_tokens
						.Where(f => f.Identity == query.Identity && (query.Key is null || f.Key == query.Key))
						.ToImmutableList<IIdentityAuthenticationToken>());
				case nameof(IIdentityAuthenticationTokenService.Delete):
					_tokens.RemoveAll(f => f.Id == ((IPrimaryKeyDto<long>)args[0]!).Id);

					return Task.CompletedTask;
				case nameof(IIdentityAuthenticationTokenService.Update):
					_updates++;

					return Task.CompletedTask;
				default:
					return null;
			}
		});
	}
}
