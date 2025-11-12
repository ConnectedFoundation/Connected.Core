namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Specifies the type of database index.
/// </summary>
public enum IndexType
{
	/// <summary>
	/// Regular non-unique index.
	/// </summary>
	Index = 1,

	/// <summary>
	/// Unique index enforcing uniqueness constraint.
	/// </summary>
	Unique = 2,

	/// <summary>
	/// Primary key index uniquely identifying rows.
	/// </summary>
	PrimaryKey = 3
}

/// <summary>
/// Represents index metadata retrieved from SQL Server system catalogs.
/// </summary>
/// <remarks>
/// This class encapsulates index information obtained from the sp_help stored procedure or
/// similar system catalog queries. It provides metadata about index properties including name,
/// description, participating columns, and type classification. The class intelligently parses
/// the raw index description to determine whether the index is a regular index, unique index,
/// or primary key based on keywords in the description. Column membership is extracted from the
/// Keys property which contains a comma-separated list of column names. This information is
/// essential for schema comparison and synchronization operations that need to understand
/// existing index definitions and determine what changes are required.
/// </remarks>
internal class ObjectIndex
{
	/// <summary>
	/// Gets or sets the index name.
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Gets or sets the index description.
	/// </summary>
	/// <value>
	/// The raw description string from system catalogs containing index properties and keywords.
	/// </value>
	public string? Description { get; set; }

	/// <summary>
	/// Gets or sets the comma-separated list of columns in the index.
	/// </summary>
	/// <value>
	/// A comma-separated string of column names that participate in the index.
	/// </value>
	public string? Keys { get; set; }

	/// <summary>
	/// Gets the parsed index type.
	/// </summary>
	/// <value>
	/// The index type category determined from the Description string.
	/// </value>
	/// <remarks>
	/// This property parses the Description string to identify whether the index is a
	/// regular index, unique index, or primary key based on the presence of specific keywords.
	/// </remarks>
	public IndexType Type
	{
		get
		{
			var tokens = Description is null ? [] : Description.Split(',');
			var result = IndexType.Index;

			/*
			 * Parse the description tokens to determine the index type.
			 */
			foreach (var token in tokens)
			{
				/*
				 * Primary key takes precedence over unique.
				 */
				if (token.Trim().Contains("primary key", StringComparison.OrdinalIgnoreCase))
					return IndexType.PrimaryKey;
				else if (string.Compare(token.Trim(), "unique", true) == 0)
					result = IndexType.Unique;
			}

			return result;
		}
	}

	/// <summary>
	/// Determines whether the index includes the specified column.
	/// </summary>
	/// <param name="column">The column name to check.</param>
	/// <returns><c>true</c> if the index references the column; otherwise, <c>false</c>.</returns>
	public bool IsReferencedBy(string column)
	{
		return Columns.Contains(column, StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Gets the list of columns participating in the index.
	/// </summary>
	/// <value>
	/// A list of column names that are part of the index.
	/// </value>
	/// <remarks>
	/// This property parses the Keys string to extract individual column names.
	/// </remarks>
	public List<string> Columns
	{
		get
		{
			var result = new List<string>();
			var tokens = Keys is null ? [] : Keys.Split(',');

			/*
			 * Extract and trim each column name from the comma-separated Keys string.
			 */
			foreach (var token in tokens)
				result.Add(token.Trim());

			return result;
		}
	}
}
