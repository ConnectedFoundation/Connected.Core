namespace Connected.Authorization;
/// <summary>
/// Represents authorization tied (bound) to a specific entity identity.
/// </summary>
public interface IBoundAuthorization
	: IAuthorization
{
	/// <summary>
	/// Gets the logical entity name this authorization applies to.
	/// </summary>
	string Entity { get; }
	/// <summary>
	/// Gets the identifier of the entity instance this authorization applies to.
	/// </summary>
	string EntityId { get; }
	/*
	 * Bound authorizations require entity context to make accurate access decisions.
	 */
}
