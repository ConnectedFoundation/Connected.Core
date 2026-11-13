using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;

namespace Connected.Identities.TwoFactor;
/// <summary>
/// The second step of a user's sign-in, waiting for a code.
/// </summary>
[Table(Schema = SchemaAttribute.CoreSchema)]
internal sealed record TwoFactorChallenge : ConsistentEntity<long>
{
	/// <summary>
	/// Gets the id of the user.
	/// </summary>
	[Ordinal(0), Index(false, "idx_twoFactorChallenge_user")]
	public long User { get; init; }

	/// <summary>
	/// Gets the hash of the challenge, from <see cref="TwoFactorSecrets.Hash"/>.
	/// </summary>
	[Ordinal(1), Length(64), Index(true, "idx_twoFactorChallenge_hash")]
	public required string Hash { get; init; }

	/// <summary>
	/// Gets whether the challenge verifies a code or sets up a secret.
	/// </summary>
	[Ordinal(2)]
	public SignInStage Stage { get; init; }

	/// <summary>
	/// Gets the time the challenge expires, in UTC.
	/// </summary>
	[Ordinal(3), Date(DateKind.DateTime2, 7)]
	public DateTimeOffset Expire { get; init; }

	/// <summary>
	/// Gets whether the authentication token issued at the end of the sign-in never expires.
	/// </summary>
	[Ordinal(4)]
	public bool Permanent { get; init; }
}
