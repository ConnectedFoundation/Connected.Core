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
	[Obsolete("use query instead")]
	/// <summary>
	/// The operation name for looking up entities by identifiers.
	/// </summary>
	public const string Lookup = "lookup";
	[Obsolete("use query instead")]

	/// <summary>
	/// The operation name for looking up entities by tokens.
	/// </summary>
	public const string LookupByTokens = "lookup-by-tokens";
	[Obsolete("use query instead")]

	/// <summary>
	/// The operation name for looking up entities by codes.
	/// </summary>
	public const string LookupByCodes = "lookup-by-codes";
	[Obsolete("use query instead")]

	/// <summary>
	/// The operation name for looking up entities by heads.
	/// </summary>
	public const string LookupByHeads = "lookup-by-heads";
	[Obsolete("use query instead")]

	/// <summary>
	/// The operation name for selecting an entity by its code.
	/// </summary>
	public const string SelectByCode = "select-by-code";
	[Obsolete("use query instead")]

	/// <summary>
	/// The operation name for selecting an entity by its token.
	/// </summary>
	public const string SelectByToken = "select-by-token";
	[Obsolete("use query instead")]

	/// <summary>
	/// The operation name for selecting an entity by its name.
	/// </summary>
	public const string SelectByName = "select-by-name";
	[Obsolete("use query instead")]

	/// <summary>
	/// The operation name for selecting an entity by its head.
	/// </summary>
	public const string SelectByHead = "select-by-head";
	[Obsolete("use query instead")]

	/// <summary>
	/// The operation name for querying entities by parent identifier.
	/// </summary>
	public const string QueryByParent = "query-by-parent";
	[Obsolete("use query instead")]
	/// <summary>
	/// The operation name for querying entities by tags.
	/// </summary>
	public const string QueryByTags = "query-by-tags";
	[Obsolete("use query instead")]
	/// <summary>
	/// The operation name for querying entities by head identifier.
	/// </summary>
	public const string QueryByHead = "query-by-head";
	[Obsolete("use query instead")]
	/// <summary>
	/// The operation name for querying entities by identity.
	/// </summary>
	public const string QueryByIdentity = "query-by-identity";
	[Obsolete("use query instead")]
	/// <summary>
	/// The operation name for selecting an entity by key.
	/// </summary>
	public const string SelectByKey = "select-by-key";
	[Obsolete("use query instead")]
	/// <summary>
	/// The operation name for updating multiple entities at the same time. This
	/// operation is typically used in extension services.
	/// </summary>
	public const string UpdateBatch = "update-batch";
	[Obsolete("use query instead")]
	/// <summary>
	/// The operation name for searching entities.
	/// </summary>
	public const string Search = "search";
}
