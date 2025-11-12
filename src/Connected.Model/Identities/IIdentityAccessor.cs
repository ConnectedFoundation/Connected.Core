namespace Connected.Identities;

/// <summary>
/// Provides access to the current identity in the execution context.
/// </summary>
/// <remarks>
/// This interface serves as an accessor pattern for retrieving the currently authenticated
/// identity within the application context. The identity may be null if no authentication
/// has occurred or if the context is anonymous.
/// </remarks>
public interface IIdentityAccessor
{
	/// <summary>
	/// Gets the current identity in the execution context.
	/// </summary>
	IIdentity? Identity { get; }
}
