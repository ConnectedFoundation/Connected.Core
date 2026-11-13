using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;

namespace Connected.Identities.TwoFactor;
/// <summary>
/// A user's two-factor secret.
/// </summary>
/// <remarks>
/// The entity is not exposed: the secret leaves the service only while it is set up.
/// </remarks>
[Table(Schema = SchemaAttribute.CoreSchema)]
internal sealed record UserTwoFactor : ConsistentEntity<long>
{
	/// <summary>
	/// Gets the id of the user.
	/// </summary>
	[Ordinal(0), Index(true, "idx_userTwoFactor_user")]
	public long User { get; init; }

	/// <summary>
	/// Gets the secret, encrypted with <see cref="TwoFactorSecrets.Protect"/>.
	/// </summary>
	[Ordinal(1), Length(256)]
	public required string Secret { get; init; }

	/// <summary>
	/// Gets whether the secret is confirmed with a code.
	/// </summary>
	[Ordinal(2)]
	public TwoFactorStatus Status { get; init; }

	/// <summary>
	/// Gets the time step of the last code the user signed in with, so the code cannot be used again.
	/// </summary>
	[Ordinal(3)]
	public long LastStep { get; init; }

	/// <summary>
	/// Gets the time the secret was created, in UTC.
	/// </summary>
	[Ordinal(4), Date(DateKind.DateTime2, 7)]
	public DateTimeOffset Created { get; init; }
}
