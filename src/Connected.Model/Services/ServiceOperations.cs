namespace Connected.Services;

/// <summary>
/// Provides standard service operation names used across the platform.
/// </summary>
/// <remarks>
/// This class centralizes common operation names for consistency across services.
/// These operation names are typically used in service URL routing to identify
/// specific operation endpoints beyond standard CRUD operations.
/// </remarks>
public static class ServiceOperations
{
	/// <summary>
	/// The operation name for looking up entities by identifiers.
	/// </summary>
	public const string Lookup = "lookup";

	/// <summary>
	/// The operation name for looking up entities by tokens.
	/// </summary>
	public const string LookupByTokens = "lookup-by-tokens";

	/// <summary>
	/// The operation name for selecting an entity by its code.
	/// </summary>
	public const string SelectByCode = "select-by-code";

	/// <summary>
	/// The operation name for selecting an entity by its token.
	/// </summary>
	public const string SelectByToken = "select-by-token";

	/// <summary>
	/// The operation name for selecting an entity by its name.
	/// </summary>
	public const string SelectByName = "select-by-name";

	/// <summary>
	/// The operation name for selecting an entity by its head.
	/// </summary>
	public const string SelectByHead = "select-by-head";

	/// <summary>
	/// The operation name for querying entities by parent identifier.
	/// </summary>
	public const string QueryByParent = "query-by-parent";

	/// <summary>
	/// The operation name for querying entities by tags.
	/// </summary>
	public const string QueryByTags = "query-by-tags";

	/// <summary>
	/// The operation name for querying entities by head identifier.
	/// </summary>
	public const string QueryByHead = "query-by-head";

	/// <summary>
	/// The operation name for querying entities by identity.
	/// </summary>
	public const string QueryByIdentity = "query-by-identity";
}
