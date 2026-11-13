using Connected.Configuration.Authentication;
using Connected.Identities.TwoFactor;
using System.Security.Cryptography;
using System.Text;

namespace Connected.Net.Tests.Identities;

/// <summary>
/// Holds the time-based one-time passwords to RFC 6238 and the encryption of two-factor secrets.
/// </summary>
[TestClass]
public sealed class TwoFactorTests
{
	private sealed class Configuration(string? key)
		: ITwoFactorConfiguration
	{
		public string? Key => key;
		public bool Required => false;
		public string? Issuer => null;
		public int ChallengeLifetime => ITwoFactorConfiguration.DefaultChallengeLifetime;
		public int MaxAttempts => ITwoFactorConfiguration.DefaultMaxAttempts;
	}

	/// <summary>
	/// The SHA-1 secret of the RFC 6238 test vectors.
	/// </summary>
	private static readonly byte[] RfcSecret = Encoding.ASCII.GetBytes("12345678901234567890");

	/// <summary>
	/// The codes match the SHA-1 test vectors of RFC 6238, appendix B, with eight and with six digits.
	/// </summary>
	[TestMethod]
	[DataRow(59L, "94287082")]
	[DataRow(1111111109L, "07081804")]
	[DataRow(1111111111L, "14050471")]
	[DataRow(1234567890L, "89005924")]
	[DataRow(2000000000L, "69279037")]
	[DataRow(20000000000L, "65353130")]
	public void MatchesTheRfcVectors(long seconds, string expected)
	{
		var step = Totp.Step(DateTimeOffset.FromUnixTimeSeconds(seconds));

		Assert.AreEqual(expected, Totp.Code(RfcSecret, step, 8));
		Assert.AreEqual(expected[2..], Totp.Code(RfcSecret, step));
	}

	/// <summary>
	/// A code is accepted one step before and after the current one, and not further.
	/// </summary>
	[TestMethod]
	public void AcceptsNeighbouringSteps()
	{
		var now = DateTimeOffset.FromUnixTimeSeconds(1111111111);
		var current = Totp.Step(now);

		Assert.AreEqual(current, Totp.Match(RfcSecret, Totp.Code(RfcSecret, current), now, 0));
		Assert.AreEqual(current - 1, Totp.Match(RfcSecret, Totp.Code(RfcSecret, current - 1), now, 0));
		Assert.AreEqual(current + 1, Totp.Match(RfcSecret, Totp.Code(RfcSecret, current + 1), now, 0));
		Assert.IsNull(Totp.Match(RfcSecret, Totp.Code(RfcSecret, current - 2), now, 0));
		Assert.IsNull(Totp.Match(RfcSecret, Totp.Code(RfcSecret, current + 2), now, 0));
	}

	/// <summary>
	/// A code of the last step signed in with, or of an earlier one, is refused, so a code works once.
	/// </summary>
	[TestMethod]
	public void RefusesUsedSteps()
	{
		var now = DateTimeOffset.FromUnixTimeSeconds(1111111111);
		var current = Totp.Step(now);

		Assert.IsNull(Totp.Match(RfcSecret, Totp.Code(RfcSecret, current), now, current));
		Assert.IsNull(Totp.Match(RfcSecret, Totp.Code(RfcSecret, current - 1), now, current - 1));
		Assert.AreEqual(current + 1, Totp.Match(RfcSecret, Totp.Code(RfcSecret, current + 1), now, current));
	}

	/// <summary>
	/// Codes that are not six digits are refused without being compared.
	/// </summary>
	[TestMethod]
	[DataRow(null)]
	[DataRow("")]
	[DataRow("12345")]
	[DataRow("1234567")]
	[DataRow("12345a")]
	[DataRow("１２３４５６")]
	public void RefusesMalformedCodes(string? code)
	{
		Assert.IsNull(Totp.Match(RfcSecret, code, DateTimeOffset.FromUnixTimeSeconds(1111111111), 0));
	}

	/// <summary>
	/// Secrets are encoded in Base32 without padding (RFC 4648, section 10).
	/// </summary>
	[TestMethod]
	[DataRow("", "")]
	[DataRow("f", "MY")]
	[DataRow("fo", "MZXQ")]
	[DataRow("foo", "MZXW6")]
	[DataRow("foob", "MZXW6YQ")]
	[DataRow("fooba", "MZXW6YTB")]
	[DataRow("foobar", "MZXW6YTBOI")]
	public void EncodesBase32(string data, string expected)
	{
		Assert.AreEqual(expected, Totp.Base32(Encoding.ASCII.GetBytes(data)));
	}

	/// <summary>
	/// The URI names the issuer and the account, escaped, and the parameters of the codes.
	/// </summary>
	[TestMethod]
	public void BuildsTheOtpauthUri()
	{
		Assert.AreEqual("otpauth://totp/Example%20Co:ana%40example.test?secret=MZXW6YTBOI&issuer=Example%20Co&algorithm=SHA1&digits=6&period=30",
			Totp.Uri("Example Co", "ana@example.test", "MZXW6YTBOI"));
	}

	/// <summary>
	/// A secret decrypts with the key and user it was encrypted for, and with no other.
	/// </summary>
	[TestMethod]
	public void EncryptsSecretsForOneUser()
	{
		var key = TwoFactorSecrets.Key(new Configuration(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))));
		var otherKey = RandomNumberGenerator.GetBytes(32);
		var secret = RandomNumberGenerator.GetBytes(Totp.SecretLength);
		var encrypted = TwoFactorSecrets.Protect(key, 7, secret);

		CollectionAssert.AreEqual(secret, TwoFactorSecrets.Unprotect(key, 7, encrypted));
		Assert.AreNotEqual(encrypted, TwoFactorSecrets.Protect(key, 7, secret));
		Assert.IsNull(TwoFactorSecrets.Unprotect(key, 8, encrypted));
		Assert.IsNull(TwoFactorSecrets.Unprotect(otherKey, 7, encrypted));
		Assert.IsNull(TwoFactorSecrets.Unprotect(key, 7, "not base64"));
		Assert.IsNull(TwoFactorSecrets.Unprotect(key, 7, Convert.ToBase64String(new byte[8])));
	}

	/// <summary>
	/// Only a 256-bit key encoded in Base64 is accepted.
	/// </summary>
	[TestMethod]
	public void RequiresA256BitKey()
	{
		Assert.ThrowsException<InvalidOperationException>(() => TwoFactorSecrets.Key(new Configuration(null)));
		Assert.ThrowsException<InvalidOperationException>(() => TwoFactorSecrets.Key(new Configuration("not base64")));
		Assert.ThrowsException<InvalidOperationException>(() => TwoFactorSecrets.Key(new Configuration(Convert.ToBase64String(new byte[16]))));
		Assert.ThrowsException<InvalidOperationException>(() => TwoFactorSecrets.Key(new Configuration(Convert.ToBase64String(new byte[33]))));
		Assert.AreEqual(32, TwoFactorSecrets.Key(new Configuration(Convert.ToBase64String(new byte[32]))).Length);
	}

	/// <summary>
	/// Challenges are random and only their SHA-256 hash is stored.
	/// </summary>
	[TestMethod]
	public void HashesChallenges()
	{
		var challenge = TwoFactorSecrets.Challenge();

		Assert.AreNotEqual(challenge, TwoFactorSecrets.Challenge());
		Assert.AreEqual(43, challenge.Length);
		Assert.AreEqual("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", TwoFactorSecrets.Hash("abc"));
	}
}
