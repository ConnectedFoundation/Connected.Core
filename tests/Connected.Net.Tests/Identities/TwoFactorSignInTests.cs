using Connected.Authentication;
using Connected.Configuration;
using Connected.Configuration.Authentication;
using Connected.Identities;
using Connected.Identities.Authentication;
using Connected.Identities.Dtos;
using Connected.Identities.Ops;
using Connected.Identities.TwoFactor;
using Connected.Identities.TwoFactor.Dtos;
using Connected.Identities.TwoFactor.Ops;
using Connected.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Connected.Net.Tests.Identities;

/// <summary>
/// Holds the two-factor gate on every credential sign-in, and the checks of a code against the user's stored secret.
/// </summary>
[TestClass]
public sealed class TwoFactorSignInTests
{
	private const long UserId = 7;

	private static readonly byte[] Key = RandomNumberGenerator.GetBytes(TwoFactorExtensions.KeyLength);
	private static readonly byte[] Secret = RandomNumberGenerator.GetBytes(Totp.SecretLength);

	private readonly IUser _user = new TestUser
	{
		Id = UserId,
		Status = UserStatus.Active,
		Token = "user-7",
		Email = "user@example.com"
	};

	private TwoFactorStatus _status;
	private int _challenges;
	private InMemoryStorage _storage = default!;

	[TestInitialize]
	public void Initialize()
	{
		var services = new ServiceCollection();

		services.AddTransient<IUpdateIdentityDto, Connected.Authentication.Dtos.UpdateIdentityDto>();
		services.AddTransient<ISelectUserDto, TestValidateUserDto>();
		services.AddTransient<IChallengeTwoFactorDto, ChallengeTwoFactorDto>();
		services.AddSingleton(Stub.Create<IAuthenticationService>());
		services.AddSingleton(Users());
		services.AddSingleton(TwoFactorService());
		services.AddSingleton(Configuration(new TestTwoFactorConfiguration { Key = Convert.ToBase64String(Key) }));

		new ServiceExtensionsStartup().ConfigureServices(services);

		_storage = new InMemoryStorage();
		_storage.Add<UserTwoFactor>((f, id, etag) => f with { Id = id, ETag = etag, State = Entities.State.Update }, f => f.User);
		_storage.Add<TwoFactorChallenge>((f, id, etag) => f with { Id = id, ETag = etag, State = Entities.State.Update }, f => f.Hash);
	}

	[TestCleanup]
	public async Task Cleanup()
	{
		await ServiceExtensionsStartup.Reset();
	}

	/// <summary>
	/// A user who set up two-factor sign-in gets no token from a password alone.
	/// </summary>
	[TestMethod]
	public async Task ValidateRefusesAnEnrolledUser()
	{
		_status = TwoFactorStatus.Enabled;

		Assert.IsNull(await Validate(new TestTwoFactorConfiguration { Key = Convert.ToBase64String(Key) }));
	}

	/// <summary>
	/// A user who set up two-factor sign-in gets a challenge, not a token, from a password.
	/// </summary>
	[TestMethod]
	public async Task SignInChallengesAnEnrolledUser()
	{
		_status = TwoFactorStatus.Enabled;

		var result = await SignIn(new TestTwoFactorConfiguration { Key = Convert.ToBase64String(Key) });

		Assert.AreEqual(SignInStage.TwoFactor, result?.Stage);
		Assert.IsNull(result?.Token);
		Assert.AreEqual(1, _challenges);
	}

	/// <summary>
	/// When two-factor sign-in is required, a user who has not set it up yet is sent to set it up.
	/// </summary>
	[TestMethod]
	public async Task RequiredTwoFactorRefusesEveryUser()
	{
		_status = TwoFactorStatus.None;

		var configuration = new TestTwoFactorConfiguration { Key = Convert.ToBase64String(Key), Required = true };

		Assert.IsNull(await Validate(configuration));
		Assert.AreEqual(SignInStage.TwoFactorSetup, (await SignIn(configuration))?.Stage);
	}

	/// <summary>
	/// Removing the key does not let a user who set up two-factor sign-in in with a password alone.
	/// </summary>
	[TestMethod]
	[DataRow(null)]
	[DataRow("")]
	public async Task MissingKeyFailsClosed(string? key)
	{
		_status = TwoFactorStatus.Enabled;

		var configuration = new TestTwoFactorConfiguration { Key = key };

		Assert.IsTrue(await TwoFactorService().IsTwoFactorRequired(configuration, _user));
		Assert.IsNull(await Validate(configuration));
		Assert.AreEqual(SignInStage.TwoFactor, (await SignIn(configuration))?.Stage);
	}

	/// <summary>
	/// A user who has not set up two-factor sign-in, where it is not required, signs in with a password alone.
	/// </summary>
	[TestMethod]
	public async Task OptionalTwoFactorLetsOtherUsersIn()
	{
		_status = TwoFactorStatus.None;

		Assert.IsFalse(await TwoFactorService().IsTwoFactorRequired(new TestTwoFactorConfiguration(), _user));
		Assert.IsTrue(await BasicAuthenticated());
	}

	/// <summary>
	/// Basic authentication refuses a user who set up two-factor sign-in.
	/// </summary>
	[TestMethod]
	public async Task BasicAuthenticationRefusesAnEnrolledUser()
	{
		_status = TwoFactorStatus.Enabled;

		Assert.IsFalse(await BasicAuthenticated());
	}

	/// <summary>
	/// A code of the step signed in with last is refused, and still counted.
	/// </summary>
	[TestMethod]
	public async Task VerifyRefusesAUsedCode()
	{
		var step = Totp.Step(DateTimeOffset.UtcNow);

		SeedFactor(step);

		var challenge = SeedChallenge();

		Assert.IsNull(await Verify(challenge, Totp.Code(Secret, step)));

		var factor = Factor();

		Assert.AreEqual(step, factor.LastStep);
		Assert.AreEqual(1, factor.Failures);
	}

	/// <summary>
	/// Each wrong code is stored before Verify returns, and the last allowed one locks the user's codes out, the right
	/// code included.
	/// </summary>
	[TestMethod]
	public async Task VerifyLocksOutAfterMaxAttempts()
	{
		SeedFactor(0);

		var challenge = SeedChallenge();
		var wrong = WrongCode();

		for (var i = 1; i < ITwoFactorConfiguration.DefaultMaxAttempts; i++)
		{
			Assert.IsNull(await Verify(challenge, wrong));
			Assert.AreEqual(i, Factor().Failures);
			Assert.IsNull(Factor().BlockedUntil);
		}

		Assert.IsNull(await Verify(challenge, wrong));
		Assert.IsTrue(Factor().BlockedUntil > DateTimeOffset.UtcNow.AddMinutes(ITwoFactorConfiguration.DefaultLockoutDuration - 1));
		Assert.AreEqual(0, Factor().Failures);

		Assert.IsNull(await Verify(challenge, Totp.Code(Secret, Totp.Step(DateTimeOffset.UtcNow))));
		Assert.AreEqual(0, Factor().LastStep);
	}

	/// <summary>
	/// A challenge that another sign-in used while this one waited for the user's row does not complete again, even
	/// with a different valid code.
	/// </summary>
	[TestMethod]
	public async Task VerifyRefusesAChallengeUsedMeanwhile()
	{
		var step = Totp.Step(DateTimeOffset.UtcNow);

		SeedFactor(0);

		var challenge = SeedChallenge();
		var factors = _storage.Table<UserTwoFactor>();
		var challenges = _storage.Table<TwoFactorChallenge>();

		factors.BeforeNextWrite = () =>
		{
			factors.Replace(Factor() with { LastStep = step - 1 });
			challenges.Rows.Clear();
		};

		Assert.IsNull(await Verify(challenge, Totp.Code(Secret, step)));
		Assert.AreEqual(step - 1, Factor().LastStep);
	}

	/// <summary>
	/// A secret removed by a reset while a code was entered ends the check at once, without retrying.
	/// </summary>
	[TestMethod]
	public async Task VerifyStopsWhenTheSecretIsRemoved()
	{
		SeedFactor(0);

		var challenge = SeedChallenge();
		var factors = _storage.Table<UserTwoFactor>();

		factors.BeforeNextWrite = factors.Rows.Clear;

		Assert.IsNull(await Verify(challenge, Totp.Code(Secret, Totp.Step(DateTimeOffset.UtcNow))));
		Assert.AreEqual(1, factors.Writes);
	}

	/// <summary>
	/// Of two setups at the same time for a user without a secret, the one whose insert breaks the unique index gets
	/// null, not an error.
	/// </summary>
	[TestMethod]
	public async Task SetupLosingTheInsertReturnsNull()
	{
		var challenge = SeedChallenge(SignInStage.TwoFactorSetup);
		var factors = _storage.Table<UserTwoFactor>();

		factors.BeforeNextWrite = () => SeedFactor(0, TwoFactorStatus.Pending);

		Assert.IsNull(await Setup(challenge));
		Assert.AreEqual(1, factors.Rows.Count);
	}

	/// <summary>
	/// Of two setups at the same time for a user with an unconfirmed secret, the one whose update finds the row changed
	/// gets null, not an error.
	/// </summary>
	[TestMethod]
	public async Task SetupLosingTheUpdateReturnsNull()
	{
		SeedFactor(0, TwoFactorStatus.Pending);

		var challenge = SeedChallenge(SignInStage.TwoFactorSetup);
		var factors = _storage.Table<UserTwoFactor>();

		factors.BeforeNextWrite = () => factors.Replace(Factor());

		Assert.IsNull(await Setup(challenge));
	}

	/// <summary>
	/// A setup that runs alone returns the secret it stored.
	/// </summary>
	[TestMethod]
	public async Task SetupStoresTheSecret()
	{
		var challenge = SeedChallenge(SignInStage.TwoFactorSetup);
		var setup = await Setup(challenge);

		Assert.IsNotNull(setup);
		Assert.AreEqual(TwoFactorStatus.Pending, Factor().Status);
	}

	private async Task<string?> Validate(ITwoFactorConfiguration twoFactor)
	{
		var operation = new Validate(Users(), Stub.Create<IIdentityAuthenticationTokenService>(), TwoFactorService(), Stub.Create<IAuthenticationService>(),
			Configuration(twoFactor));

		return await operation.Invoke(new TestValidateUserDto { User = "user", Password = "password" });
	}

	private async Task<SignInResult?> SignIn(ITwoFactorConfiguration twoFactor)
	{
		var operation = new SignIn(Users(), Stub.Create<IIdentityAuthenticationTokenService>(), TwoFactorService(), Stub.Create<IAuthenticationService>(),
			Configuration(twoFactor));

		return await operation.Invoke(new TestValidateUserDto { User = "user", Password = "password" });
	}

	private async Task<bool> BasicAuthenticated()
	{
		var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
		var provider = new BasicAuthentication(http);

		await provider.Invoke(new AuthenticateDto
		{
			Schema = "basic",
			Token = Convert.ToBase64String(Encoding.UTF8.GetBytes("user:password"))
		});

		return http.HttpContext.User.Identity?.IsAuthenticated == true;
	}

	private async Task<string?> Verify(string challenge, string code)
	{
		var operation = new Verify(_storage, Users(), Stub.Create<IIdentityAuthenticationTokenService>(), Configuration(Settings()), Stub.Create<IAuthenticationService>());

		return await operation.Invoke(new VerifyTwoFactorDto { Challenge = challenge, Code = code });
	}

	private async Task<TwoFactorSetup?> Setup(string challenge)
	{
		var operation = new Setup(_storage, Users(), Configuration(Settings()), Stub.Create<IAuthenticationService>());

		return await operation.Invoke(new ValueDto<string>(challenge));
	}

	private static TestTwoFactorConfiguration Settings() => new() { Key = Convert.ToBase64String(Key) };

	private UserTwoFactor Factor() => _storage.Table<UserTwoFactor>().Rows.Single();

	private UserTwoFactor SeedFactor(long lastStep, TwoFactorStatus status = TwoFactorStatus.Enabled)
	{
		return _storage.Table<UserTwoFactor>().Seed(new UserTwoFactor
		{
			User = UserId,
			Secret = TwoFactorSecrets.Protect(Key, UserId, Secret),
			Status = status,
			LastStep = lastStep,
			Created = DateTimeOffset.UtcNow
		});
	}

	private string SeedChallenge(SignInStage stage = SignInStage.TwoFactor)
	{
		var value = TwoFactorSecrets.Challenge();

		_storage.Table<TwoFactorChallenge>().Seed(new TwoFactorChallenge
		{
			User = UserId,
			Hash = TwoFactorSecrets.Hash(value),
			Stage = stage,
			Expire = DateTimeOffset.UtcNow.AddMinutes(10)
		});

		return value;
	}

	/// <summary>
	/// Returns a code that matches none of the steps a code is accepted for now.
	/// </summary>
	private static string WrongCode()
	{
		var step = Totp.Step(DateTimeOffset.UtcNow);
		var valid = new[] { step - 1, step, step + 1, step + 2 }.Select(f => Totp.Code(Secret, f)).ToHashSet();

		return Enumerable.Range(0, 10).Select(f => new string((char)('0' + f), 6)).First(f => !valid.Contains(f));
	}

	private IUserService Users()
	{
		return Stub.Create<IUserService>((method, _) => method.Name == nameof(IUserService.Select) ? Stub.Result(method, _user) : null);
	}

	private ITwoFactorService TwoFactorService()
	{
		return Stub.Create<ITwoFactorService>((method, args) => method.Name switch
		{
			nameof(ITwoFactorService.Status) => Task.FromResult(_status),
			nameof(ITwoFactorService.Challenge) => Challenge(),
			_ => null
		});

		Task<SignInResult> Challenge()
		{
			_challenges++;

			return Task.FromResult(new SignInResult
			{
				Stage = _status == TwoFactorStatus.Enabled ? SignInStage.TwoFactor : SignInStage.TwoFactorSetup,
				Challenge = "challenge"
			});
		}
	}

	private static IConfigurationService Configuration(ITwoFactorConfiguration twoFactor)
	{
		var authentication = new TestAuthenticationConfiguration(twoFactor);

		return Stub.Create<IConfigurationService>((method, _) => method.Name == "get_Authentication" ? authentication : null);
	}
}
