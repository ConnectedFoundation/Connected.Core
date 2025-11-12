namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Describes an index to be created on a database table.
/// </summary>
/// <remarks>
/// This class encapsulates the definition of a database index including its uniqueness constraint,
/// column composition, and optional grouping name. Index descriptors are used during schema
/// synchronization to determine what indexes need to be created or modified. The group property
/// allows multiple columns to be logically grouped under a single named index. The class provides
/// a string representation that returns either the group name or the first column name for
/// identification purposes.
/// </remarks>
internal class IndexDescriptor
{
	private List<string>? _columns;

	/// <summary>
	/// Gets or sets a value indicating whether this is a unique index.
	/// </summary>
	/// <value>
	/// <c>true</c> if the index enforces uniqueness; otherwise, <c>false</c>.
	/// </value>
	public bool Unique { get; set; }

	/// <summary>
	/// Gets or sets the group name for multi-column indexes.
	/// </summary>
	/// <value>
	/// The name identifying a group of columns in a composite index, or <c>null</c> for single-column indexes.
	/// </value>
	public string? Group { get; set; }

	/// <summary>
	/// Gets the list of column names included in this index.
	/// </summary>
	/// <value>
	/// A list of column names that participate in the index.
	/// </value>
	public List<string> Columns => _columns ??= [];

	/// <summary>
	/// Returns a string representation of the index descriptor.
	/// </summary>
	/// <returns>
	/// The group name if specified; otherwise, the first column name.
	/// </returns>
	public override string ToString()
	{
		return string.IsNullOrWhiteSpace(Group) ? Columns[0] : Group;
	}
}
